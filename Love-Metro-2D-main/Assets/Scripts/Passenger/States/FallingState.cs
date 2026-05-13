using UnityEngine;

public partial class Passenger
{
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
            Passanger._rigidbody.drag = Passanger.Settings.defaultLinearDamping;
            Passanger._rigidbody.angularDrag = Passanger.Settings.defaultAngularDamping;
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
            Passanger._rigidbody.drag = Passanger.Settings.airborneLinearDamping;
            Passanger._rigidbody.angularDrag = Passanger.Settings.airborneAngularDamping;
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
}
