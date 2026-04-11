using System.Collections.Generic;
using UnityEngine;

public partial class Passenger
{
    private readonly List<Passenger> _sameGenderRepelBuffer = new List<Passenger>(8);

    private void EnsureStateMachineInitialized()
    {
        if (wanderingState != null)
            return;

        wanderingState = new Wandering(this);
        stayingOnHandrailState = new StayingOnHandrail(this);
        fallingState = new Falling(this);
        flyingState = new Flying(this);
        matchingState = new Matching(this);
        beingAbsorbedState = new BeingAbsorbed(this);
    }

    private void ChangeState(PassangerState newState)
    {
        if (_currentState != null)
        {
            _currentState.Exit();
            UnsubscribeCurrentStateFromTrainInertia();
        }

        _currentState = newState;

        if (_currentState != null)
        {
            _currentState.Enter();
            SubscribeCurrentStateToTrainInertia();
        }
    }

    private void SubscribeCurrentStateToTrainInertia()
    {
        if (_train == null || _currentState == null)
            return;

        _train.startInertia -= _currentState.OnTrainSpeedChange;
        _train.startInertia += _currentState.OnTrainSpeedChange;
    }

    private void UnsubscribeCurrentStateFromTrainInertia()
    {
        if (_train != null && _currentState != null)
            _train.startInertia -= _currentState.OnTrainSpeedChange;
    }

    private Vector2 GetImpulseTargetWorld(Vector2 position)
    {
        return ClickDirectionManager.HasReleasePoint
            ? ClickDirectionManager.LastReleaseWorld
            : position + ClickDirectionManager.GetCurrentDirection() * 5f;
    }

    private static float GetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            float worldDelta = vertical ? targetWorld.y - position.y : targetWorld.x - position.x;
            return Mathf.Clamp(worldDelta, -1f, 1f);
        }

        Vector3 positionScreen = mainCamera.WorldToScreenPoint(position);
        Vector3 targetScreen = mainCamera.WorldToScreenPoint(targetWorld);
        float delta = vertical ? targetScreen.y - positionScreen.y : targetScreen.x - positionScreen.x;
        float divisor = vertical ? Mathf.Max(1f, (float)Screen.height) : Mathf.Max(1f, (float)Screen.width);
        return Mathf.Clamp(delta / divisor, -1f, 1f);
    }

    private static Vector2 GetCollisionNormal(Collision2D collision, Vector2 fallback)
    {
        return collision != null && collision.contacts.Length > 0
            ? collision.contacts[0].normal
            : fallback;
    }

    private bool TryResolvePassengerImpact(Passenger other)
    {
        if (other == null)
            return false;

        BreakCoupleOnImpact(other);
        other.BreakCoupleOnImpact(this);
        return TryMatchWith(other);
    }

    private void CollectSameGenderPassengers(List<Passenger> results)
    {
        if (results == null)
            return;

        if (PassengerRegistry.Instance != null)
        {
            PassengerRegistry.Instance.GetSameGenderInRadius(this, _repelRadius, results);
            return;
        }

        results.Clear();
        foreach (Passenger other in Object.FindObjectsOfType<Passenger>())
        {
            if (other == this || other == null || other.IsFemale != IsFemale)
                continue;

            float distance = Vector2.Distance(transform.position, other.transform.position);
            if (distance <= _repelRadius)
                results.Add(other);
        }
    }

    private static Passenger FindClosestOpposite(Passenger self, float radius)
    {
        if (PassengerRegistry.Instance != null)
            return PassengerRegistry.Instance.FindClosestOpposite(self, radius);

        Passenger best = null;
        float bestDistance = radius;
        foreach (Passenger passenger in Object.FindObjectsOfType<Passenger>())
        {
            if (passenger == self || passenger == null || passenger.IsFemale == self.IsFemale)
                continue;

            float distance = Vector2.Distance(self.transform.position, passenger.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = passenger;
            }
        }

        return best;
    }

    private abstract class PassangerState
    {
        protected Passenger Passanger;

        protected PassangerState(Passenger pasanger)
        {
            Passanger = pasanger;
        }

        public abstract void UpdateState();
        public abstract void Exit();
        public abstract void Enter();
        public abstract void OnCollision(Collision2D collision);
        public abstract void OnTriggerEnter(Collider2D collision);
        public abstract void OnTrainSpeedChange(Vector2 force);
    }

    private class Wandering : PassangerState
    {
        private float _expiredCollisionCheckTime;

        public Wandering(Passenger pasanger) : base(pasanger) { }

        public override void OnCollision(Collision2D collision)
        {
            ReflectMovementDirection(GetCollisionNormal(collision, Vector2.up));
            _expiredCollisionCheckTime = 0f;
        }

        public override void Exit()
        {
            Passanger._rigidbody.velocity = Vector2.zero;
            Passanger.PassangerAnimator.ExitWanderingMode();
        }

        public override void UpdateState()
        {
            if (!LevelGameplaySettings.SlipperyFloorEnabled)
                Passanger._rigidbody.velocity = Vector2.zero;

            Passanger.PassangerAnimator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(Passanger.CurrentMovingDirection, Vector3.right).normalized, Vector3.right) == 1);

            Passanger._timeWithoutHolding += Time.deltaTime;
            _expiredCollisionCheckTime += Time.deltaTime;
            if (_expiredCollisionCheckTime <= Passanger._aditionalCollisionCheckTimePeriod)
                return;

            _expiredCollisionCheckTime = 0f;
            Vector3? normal = CheckCollisions();
            if (normal != null)
                ReflectMovementDirection((Vector3)normal);
        }

        public override void Enter()
        {
            Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Passanger._rigidbody.velocity = Vector2.zero;
            Passanger.PassangerAnimator.EnterWanderingMode();
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            float sensitivity = Passanger._launchSensitivity;
            Vector2 position = Passanger.transform.position;
            Vector2 targetWorld = Passanger.GetImpulseTargetWorld(position);

            float baseMagnitude = Mathf.Max(force.magnitude, 6f) * sensitivity;
            float deltaXNorm = GetNormalizedTargetDelta(position, targetWorld, vertical: false);
            float deltaYNorm = GetNormalizedTargetDelta(position, targetWorld, vertical: true);

            float xWeight = Mathf.Pow(Mathf.Abs(deltaXNorm), Passanger._uniformLaunchGamma);
            float yWeight = Mathf.Pow(Mathf.Abs(deltaYNorm), Passanger._uniformLaunchGamma);

            float xFromClick = Mathf.Sign(deltaXNorm) * baseMagnitude * (Passanger._uniformLaunchScale * 1.2f) * xWeight;
            float yFromClick = Mathf.Sign(deltaYNorm) * baseMagnitude * (Passanger._uniformLaunchScale * 0.8f) * yWeight;

            xFromClick += (Random.value - 0.5f) * 0.1f * Passanger._turbulenceStrength;
            yFromClick += (Random.value - 0.5f) * 0.1f * Passanger._turbulenceStrength;

            Passenger target = FindClosestOpposite(Passanger, Passanger._aimAssistRadius);
            if (target != null)
            {
                Vector2 toTarget = (Vector2)(target.transform.position - Passanger.transform.position);
                float distanceNormalized = toTarget.magnitude / Passanger._aimAssistRadius;
                if (distanceNormalized < 1f)
                {
                    Vector2 direction = toTarget.normalized;
                    float assistStrength = Passanger._aimAssistMaxStrength * (1f - distanceNormalized);
                    xFromClick += direction.x * assistStrength;
                    yFromClick += direction.y * assistStrength;
                }
            }

            Vector2 delta = new Vector2(xFromClick, yFromClick) * (Passanger._flightSpeedMultiplier * Passanger._globalImpulseScale);
            if (delta.magnitude < Passanger._minImpulseToLaunch)
                return;

            Vector2 startVelocity = delta * Passanger._impulseToVelocityScale * (Passanger._flightSpeedMultiplier * Passanger._globalImpulseScale);
            if (startVelocity.magnitude > Passanger._maxFlightSpeed)
                startVelocity = startVelocity.normalized * Passanger._maxFlightSpeed;

            Passanger.EnterFallingState(startVelocity);
            Passanger._currentState.OnTrainSpeedChange(delta);
            Passanger.LogPassengerEvent("launch", $"startVelocity={startVelocity} x={xFromClick:F2} y={yFromClick:F2}");
        }

        public override void OnTriggerEnter(Collider2D collision)
        {
            if (!collision.TryGetComponent(out HandRailPosition handrail))
                return;

            if (Passanger._timeWithoutHolding <= Passanger._handrailCooldown
                || handrail.IsOccupied
                || Random.Range(0f, 1f) > Passanger._grabingHandrailChance)
            {
                return;
            }

            handrail.IsOccupied = true;
            Passanger.transform.position = handrail.transform.position;
            Passanger.releaseHandrail += handrail.ReleaseHandrail;
            Passanger.ChangeState(Passanger.stayingOnHandrailState);
        }

        private Vector3? CheckCollisions()
        {
            ContactPoint2D[] contactPoints = new ContactPoint2D[20];
            int contacts = Passanger._rigidbody.GetContacts(contactPoints);
            return contacts > 0 ? contactPoints[0].normal : (Vector3?)null;
        }

        private void ReflectMovementDirection(Vector3 normal)
        {
            Passanger.CurrentMovingDirection = Vector2.Reflect(Passanger.CurrentMovingDirection, normal).normalized;
        }
    }

    private class StayingOnHandrail : PassangerState
    {
        private float _expiredTime;
        private float _stayingTime;

        public StayingOnHandrail(Passenger pasanger) : base(pasanger) { }

        public override void OnCollision(Collision2D collision) { }

        public override void Exit()
        {
            Passanger.releaseHandrail?.Invoke();
            Passanger.releaseHandrail = null;
            Passanger.PassangerAnimator.ExitHoldingMode();
        }

        public override void UpdateState()
        {
            _expiredTime += Time.deltaTime;
            if (_expiredTime > _stayingTime)
                Passanger.ChangeState(Passanger.wanderingState);
        }

        public override void Enter()
        {
            ResetTimer();
            Passanger._rigidbody.bodyType = RigidbodyType2D.Static;
            Passanger.PassangerAnimator.EnterHoldingMode();
            Passanger._timeWithoutHolding = 0f;
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            ResetTimer();
        }

        public override void OnTriggerEnter(Collider2D collision) { }

        private void ResetTimer()
        {
            _expiredTime = 0f;
            _stayingTime = Random.Range(Passanger.HandrailStandingTimeInterval.x, Passanger.HandrailStandingTimeInterval.y);
        }
    }

    private class Falling : PassangerState
    {
        private Vector2 currentFallingSpeed;
        private int _bounceCount;

        public Falling(Passenger pasanger) : base(pasanger)
        {
            ResetFallingSpeeds();
        }

        public void SetInitialFallingSpeed(Vector2 initialSpeed)
        {
            currentFallingSpeed = initialSpeed * (Passanger._flightSpeedMultiplier * Passanger._globalImpulseScale);
        }

        public override void OnCollision(Collision2D collision)
        {
            Vector2 normal = GetCollisionNormal(collision, Vector2.up);
            Diagnostics.Log($"[Passenger][falling][hit] self={Passanger.name} with={(collision.transform != null ? collision.transform.name : "<null>")} normal={normal} v={currentFallingSpeed.magnitude:F2}");

            if (collision.transform.TryGetComponent(out Passenger passenger))
            {
                if (Passanger.TryResolvePassengerImpact(passenger))
                    return;

                Vector2 selfVelocity = currentFallingSpeed;
                Vector2 otherVelocity = passenger.GetCurrentVelocity();

                currentFallingSpeed = Passanger.ReflectVelocity(selfVelocity, normal, Mathf.Max(1f, Passanger._wallBounceBoost));
                Passanger._rigidbody.velocity = currentFallingSpeed;
                passenger.ApplyReflectedVelocity(otherVelocity, -normal, Mathf.Max(1f, passenger._wallBounceBoost));
            }
            else
            {
                currentFallingSpeed = Passanger.ReflectVelocity(currentFallingSpeed, normal, Passanger._wallBounceBoost);
                Passanger._rigidbody.velocity = currentFallingSpeed;
                Diagnostics.Log($"[Passenger][falling][wall] {Passanger.name} bounce v={currentFallingSpeed.magnitude:F2}");
            }

            _bounceCount++;
            if (_bounceCount >= Passanger._maxBounces)
            {
                Passanger._rigidbody.velocity = Vector2.zero;
                Passanger.ChangeState(Passanger.wanderingState);
            }
        }

        public override void Exit()
        {
            ResetFallingSpeeds();
            Passanger._rigidbody.velocity = Vector2.zero;
            Passanger.PassangerAnimator.ExitAirborneMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            Passanger._rigidbody.drag = 0.2f;
            Passanger._rigidbody.angularDrag = 0.2f;
        }

        public override void UpdateState()
        {
            float speed = currentFallingSpeed.magnitude;
            float t01 = Mathf.InverseLerp(0f, Passanger._maxFlightSpeed, speed);
            float k = Mathf.Lerp(Passanger._easeOutMinK, Passanger._easeOutMaxK, t01);
            currentFallingSpeed *= Mathf.Pow(k, 60f * Time.deltaTime);

            Passenger target = FindClosestOpposite(Passanger, Passanger._magnetRadius);
            if (target != null)
            {
                Vector2 toTarget = (Vector2)(target.transform.position - Passanger.transform.position);
                float weight = Mathf.InverseLerp(Passanger._magnetRadius, 0f, toTarget.magnitude);
                Vector2 acceleration = toTarget.normalized * (Passanger._magnetForce * weight) * Time.deltaTime;
                Vector2 forward = currentFallingSpeed.sqrMagnitude > 0.0001f ? currentFallingSpeed.normalized : Vector2.right;
                float along = Vector2.Dot(acceleration, forward);
                currentFallingSpeed += forward * along;
            }

            Passanger.CollectSameGenderPassengers(Passanger._sameGenderRepelBuffer);
            for (int i = 0; i < Passanger._sameGenderRepelBuffer.Count; i++)
            {
                Passenger other = Passanger._sameGenderRepelBuffer[i];
                if (other == null)
                    continue;

                Vector2 toOther = (Vector2)(other.transform.position - Passanger.transform.position);
                float distance = toOther.magnitude;
                if (distance < 0.001f || distance > Passanger._repelRadius)
                    continue;

                float weight = Mathf.InverseLerp(Passanger._repelRadius, 0f, distance);
                currentFallingSpeed -= toOther.normalized * (Passanger._repelForce * weight) * Time.deltaTime;
            }

            currentFallingSpeed = Passanger.ClampFlightVelocity(currentFallingSpeed);
            Passanger._rigidbody.velocity = currentFallingSpeed;

            if (currentFallingSpeed.magnitude <= Passanger._minFallingSpeed)
            {
                Passanger.ChangeState(Passanger.wanderingState);
                return;
            }

        }

        public override void Enter()
        {
            Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Passanger.PassangerAnimator.EnterAirborneMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            _bounceCount = 0;
            Passanger._rigidbody.drag = 0.02f;
            Passanger._rigidbody.angularDrag = 0.02f;
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            float sensitivity = Mathf.Max(0.05f, Passanger._launchSensitivity);
            Vector2 position = Passanger.transform.position;
            Vector2 targetWorld = Passanger.GetImpulseTargetWorld(position);

            float baseMagnitude = Mathf.Max(force.magnitude, 6f) * sensitivity;
            float xFromTrain = force.x * sensitivity * Passanger._flightHorizontalScale * 1.1f;
            float deltaYNorm = GetNormalizedTargetDelta(position, targetWorld, vertical: true);
            float yWeight = Mathf.Pow(Mathf.Abs(deltaYNorm), Passanger._flightVerticalGamma);
            float yFromClick = Mathf.Sign(deltaYNorm) * baseMagnitude * Passanger._flightVerticalScale * yWeight;

            yFromClick += (Random.value - 0.5f) * 0.25f * Passanger._turbulenceStrength;

            Passenger aimTarget = FindClosestOpposite(Passanger, Passanger._aimAssistRadius);
            if (aimTarget != null)
            {
                Vector2 toAim = (Vector2)(aimTarget.transform.position - Passanger.transform.position);
                float weight = Mathf.Clamp01(Mathf.Abs(toAim.y) / Passanger._aimAssistRadius);
                yFromClick += Mathf.Sign(toAim.y) * Mathf.Min(Passanger._aimAssistMaxStrength, toAim.magnitude * 0.2f) * weight;
            }

            Vector2 delta = new Vector2(xFromTrain, yFromClick) * Passanger._globalImpulseScale;
            currentFallingSpeed += delta;
            currentFallingSpeed = Passanger.ClampFlightVelocity(currentFallingSpeed);
        }

        public override void OnTriggerEnter(Collider2D collision) { }

        private void ResetFallingSpeeds()
        {
            currentFallingSpeed = Vector2.zero;
        }
    }

    private class Flying : PassangerState
    {
        private Vector2 _flyingVelocity;
        private float _windStrength;
        private float _flyingTime;

        public Flying(Passenger pasanger) : base(pasanger) { }

        public override void OnCollision(Collision2D collision)
        {
            Vector2 normal = GetCollisionNormal(collision, Vector2.up);
            Diagnostics.Log($"[Passenger][flying][hit] self={Passanger.name} with={(collision.transform != null ? collision.transform.name : "<null>")} normal={normal} v={_flyingVelocity.magnitude:F2}");

            if (collision.transform.TryGetComponent(out Passenger other))
            {
                if (Passanger.TryResolvePassengerImpact(other))
                    return;

                Vector2 reflectedVelocity = Passanger.ReflectVelocity(_flyingVelocity, normal, Mathf.Max(1f, Passanger._wallBounceBoost));
                other.ApplyReflectedVelocity(other.GetCurrentVelocity(), -normal, Mathf.Max(1f, other._wallBounceBoost));
                Passanger.EnterFallingState(reflectedVelocity);
                return;
            }

            Vector2 reflected = Passanger.ReflectVelocity(_flyingVelocity, normal, Passanger._wallBounceBoost);
            Passanger.EnterFallingState(reflected);
            Diagnostics.Log($"[Passenger][flying->falling] {Passanger.name} after wall bounce v={reflected.magnitude:F2}");
        }

        public override void Exit()
        {
            Passanger.IsMatchable = true;
            Passanger.PassangerAnimator.ExitAirborneMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            _flyingTime = 0f;
        }

        public override void UpdateState()
        {
            _flyingTime += Time.deltaTime;

            if (_flyingVelocity.sqrMagnitude > 0.0001f && Passanger._flightDeceleration > 0f)
            {
                float speed = Mathf.Max(0f, _flyingVelocity.magnitude - Passanger._flightDeceleration * Time.deltaTime);
                _flyingVelocity = _flyingVelocity.normalized * speed;
            }

            _flyingVelocity = Passanger.ClampFlightVelocity(_flyingVelocity);
            Passanger._rigidbody.velocity = _flyingVelocity;

            if (_windStrength < Passanger._minWindStrengthForFlying || _flyingTime > Passanger._maxFlyingTime)
            {
                Passanger.EnterFallingState(_flyingVelocity);
                return;
            }

            Passanger.PassangerAnimator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(_flyingVelocity, Vector3.right).normalized, Vector3.right) == 1);
        }

        public override void Enter()
        {
            Passanger.IsMatchable = false;
            Passanger.PassangerAnimator.EnterAirborneMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            _flyingTime = 0f;
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            _flyingVelocity += force * 0.3f;
        }

        public override void OnTriggerEnter(Collider2D collision) { }

        public void SetFlyingParameters(Vector2 windVelocity, float windStrength)
        {
            _flyingVelocity = windVelocity * (Passanger._flightSpeedMultiplier * Passanger._globalImpulseScale);
            _windStrength = windStrength;
        }

        public void UpdateWindEffect(Vector2 windVelocity, float windStrength)
        {
            _flyingVelocity = windVelocity * (Passanger._flightSpeedMultiplier * Passanger._globalImpulseScale);
            _windStrength = windStrength;
        }
    }

    private class Matching : PassangerState
    {
        public Matching(Passenger pasanger) : base(pasanger) { }

        public override void OnCollision(Collision2D collision) { }

        public override void Exit()
        {
            Passanger.IsMatchable = true;
            if (Passanger._rigidbody != null)
                Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            if (Passanger._collider != null)
                Passanger._collider.enabled = true;
            Passanger.PassangerAnimator.ExitMatchingMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
        }

        public override void UpdateState() { }

        public override void Enter()
        {
            Passanger.IsMatchable = false;
            Passanger._rigidbody.bodyType = RigidbodyType2D.Static;
            Passanger.PassangerAnimator.EnterMatchingMode();
            Passanger._collider.enabled = false;
        }

        public override void OnTrainSpeedChange(Vector2 force) { }

        public override void OnTriggerEnter(Collider2D collision) { }
    }

    private class BeingAbsorbed : PassangerState
    {
        private Vector3 _absorptionCenter;
        private float _absorptionForce;
        private float _timeInAbsorption;
        private readonly float _maxAbsorptionTime = 3f;

        public BeingAbsorbed(Passenger passenger) : base(passenger) { }

        public void SetAbsorptionParameters(Vector3 center, float force)
        {
            _absorptionCenter = center;
            _absorptionForce = force;
        }

        public override void OnCollision(Collision2D collision) { }

        public override void Exit()
        {
            Passanger.PassangerAnimator.ExitAirborneMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            _timeInAbsorption = 0f;
        }

        public override void UpdateState()
        {
            _timeInAbsorption += Time.deltaTime;

            Vector3 direction = (_absorptionCenter - Passanger.transform.position).normalized;
            float distance = Vector3.Distance(Passanger.transform.position, _absorptionCenter);
            if (distance < 0.5f || _timeInAbsorption > _maxAbsorptionTime)
            {
                Passanger.RemoveFromContainerAndDestroy();
                return;
            }

            Vector2 absorptionForce = direction * _absorptionForce;
            absorptionForce *= 1f / Mathf.Max(distance, 0.1f);
            Passanger._rigidbody.AddForce(absorptionForce, ForceMode2D.Force);
        }

        public override void Enter()
        {
            Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Passanger.PassangerAnimator.EnterAirborneMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            Passanger.IsMatchable = false;
            _timeInAbsorption = 0f;
        }

        public override void OnTrainSpeedChange(Vector2 force) { }

        public override void OnTriggerEnter(Collider2D collision) { }
    }
}
