using UnityEngine;

namespace LoveMetro.Passengers.States
{
    public sealed class WanderingPassengerState : IPassengerState
    {
        private readonly PassengerStateContext _context;
        private readonly ContactPoint2D[] _contactPoints = new ContactPoint2D[20];
        private float _expiredCollisionCheckTime;

        public WanderingPassengerState(PassengerStateContext context)
        {
            _context = context;
        }

        public PassengerStateId Id => PassengerStateId.Wandering;

        public void OnCollision(Collision2D collision)
        {
            ReflectMovementDirection(_context.GetCollisionNormal(collision, Vector2.up));
            _expiredCollisionCheckTime = 0f;
        }

        public void Exit()
        {
            _context.SetVelocity(Vector2.zero);
            _context.Animator.ExitWanderingMode();
        }

        public void UpdateState()
        {
            if (!LevelGameplaySettings.SlipperyFloorEnabled)
                _context.SetVelocity(Vector2.zero);

            _context.Animator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(_context.CurrentMovingDirection, Vector3.right).normalized, Vector3.right) == 1);

            _context.TimeWithoutHolding += Time.deltaTime;
            _expiredCollisionCheckTime += Time.deltaTime;
            if (_expiredCollisionCheckTime <= _context.AdditionalCollisionCheckTimePeriod)
                return;

            _expiredCollisionCheckTime = 0f;
            Vector3? normal = CheckCollisions();
            if (normal != null)
                ReflectMovementDirection((Vector3)normal);
        }

        public void Enter()
        {
            _context.SetBodyType(RigidbodyType2D.Dynamic);
            _context.SetVelocity(Vector2.zero);
            _context.Animator.EnterWanderingMode();
        }

        public void OnTrainSpeedChange(Vector2 force)
        {
            float sensitivity = _context.LaunchSensitivity;
            Vector2 position = _context.Position;
            Vector2 targetWorld = _context.GetImpulseTargetWorld(position);

            float baseMagnitude = Mathf.Max(force.magnitude, 6f) * sensitivity;
            float deltaXNorm = _context.GetNormalizedTargetDelta(position, targetWorld, vertical: false);
            float deltaYNorm = _context.GetNormalizedTargetDelta(position, targetWorld, vertical: true);

            float xWeight = Mathf.Pow(Mathf.Abs(deltaXNorm), _context.UniformLaunchGamma);
            float yWeight = Mathf.Pow(Mathf.Abs(deltaYNorm), _context.UniformLaunchGamma);

            float xFromClick = Mathf.Sign(deltaXNorm) * baseMagnitude * (_context.UniformLaunchScale * 1.2f) * xWeight;
            float yFromClick = Mathf.Sign(deltaYNorm) * baseMagnitude * (_context.UniformLaunchScale * 0.8f) * yWeight;

            xFromClick += (Random.value - 0.5f) * 0.1f * _context.TurbulenceStrength;
            yFromClick += (Random.value - 0.5f) * 0.1f * _context.TurbulenceStrength;

            global::Passenger target = _context.FindClosestOpposite(_context.AimAssistRadius);
            if (target != null)
            {
                Vector2 toTarget = (Vector2)(target.transform.position - _context.Position);
                float distanceNormalized = toTarget.magnitude / _context.AimAssistRadius;
                if (distanceNormalized < 1f)
                {
                    Vector2 direction = toTarget.normalized;
                    float assistStrength = _context.AimAssistMaxStrength * (1f - distanceNormalized);
                    xFromClick += direction.x * assistStrength;
                    yFromClick += direction.y * assistStrength;
                }
            }

            Vector2 delta = new Vector2(xFromClick, yFromClick) * (_context.FlightSpeedMultiplier * _context.GlobalImpulseScale);
            if (delta.magnitude < _context.MinImpulseToLaunch)
                return;

            Vector2 startVelocity = _context.ScaleLaunchVelocity(
                delta,
                _context.ImpulseToVelocityScale,
                _context.FlightSpeedMultiplier * _context.GlobalImpulseScale);

            _context.EnterFallingState(startVelocity);
            _context.ForwardTrainSpeedChangeToCurrentState(delta);
            _context.LogEvent("launch", $"startVelocity={startVelocity} x={xFromClick:F2} y={yFromClick:F2}");
        }

        public void OnTriggerEnter(Collider2D collision)
        {
            if (!collision.TryGetComponent(out HandRailPosition handrail))
                return;

            if (_context.TimeWithoutHolding <= _context.HandrailCooldown
                || handrail.IsOccupied
                || Random.Range(0f, 1f) > _context.GrabbingHandrailChance)
            {
                return;
            }

            _context.AttachHandrail(handrail);
            _context.ChangeState(PassengerStateId.StayingOnHandrail);
        }

        private Vector3? CheckCollisions()
        {
            int contacts = _context.GetContacts(_contactPoints);
            return contacts > 0 ? _contactPoints[0].normal : (Vector3?)null;
        }

        private void ReflectMovementDirection(Vector3 normal)
        {
            _context.CurrentMovingDirection = Vector2.Reflect(_context.CurrentMovingDirection, normal).normalized;
        }
    }
}
