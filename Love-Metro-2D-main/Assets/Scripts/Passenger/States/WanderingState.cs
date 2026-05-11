using UnityEngine;

public partial class Passenger
{
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
            Passanger._rigidbody.linearVelocity = Vector2.zero;
            Passanger.PassangerAnimator.ExitWanderingMode();
        }

        public override void UpdateState()
        {
            if (!LevelGameplaySettings.SlipperyFloorEnabled)
                Passanger._rigidbody.linearVelocity = Vector2.zero;

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
            Passanger._rigidbody.linearVelocity = Vector2.zero;
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
}
