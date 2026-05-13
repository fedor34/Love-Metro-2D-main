using UnityEngine;

namespace LoveMetro.Passengers.States
{
    public sealed class FlyingPassengerState : IPassengerFlyingState
    {
        private readonly PassengerStateContext _context;
        private Vector2 _flyingVelocity;
        private float _windStrength;
        private float _flyingTime;

        public FlyingPassengerState(PassengerStateContext context)
        {
            _context = context;
        }

        public PassengerStateId Id => PassengerStateId.Flying;

        public void OnCollision(Collision2D collision)
        {
            Vector2 normal = _context.GetCollisionNormal(collision, Vector2.up);
            Diagnostics.Log($"[Passenger][flying][hit] self={_context.Name} with={(collision.transform != null ? collision.transform.name : "<null>")} normal={normal} v={_flyingVelocity.magnitude:F2}");

            if (collision.transform.TryGetComponent(out global::Passenger other))
            {
                if (_context.TryResolvePassengerImpact(other))
                    return;

                Vector2 reflectedVelocity = _context.ReflectVelocity(_flyingVelocity, normal, Mathf.Max(1f, _context.WallBounceBoost));
                _context.ApplyReflectedVelocity(other, _context.GetVelocity(other), -normal, Mathf.Max(1f, _context.GetWallBounceBoost(other)));
                _context.EnterFallingState(reflectedVelocity);
                return;
            }

            Vector2 reflected = _context.ReflectVelocity(_flyingVelocity, normal, _context.WallBounceBoost);
            _context.EnterFallingState(reflected);
            Diagnostics.Log($"[Passenger][flying->falling] {_context.Name} after wall bounce v={reflected.magnitude:F2}");
        }

        public void Exit()
        {
            _context.IsMatchable = true;
            _context.Animator.ExitAirborneMode();
            _context.SetDefaultLayer();
            _flyingTime = 0f;
        }

        public void UpdateState()
        {
            _flyingTime += Time.deltaTime;

            if (_flyingVelocity.sqrMagnitude > 0.0001f && _context.FlightDeceleration > 0f)
            {
                float speed = Mathf.Max(0f, _flyingVelocity.magnitude - _context.FlightDeceleration * Time.deltaTime);
                _flyingVelocity = _flyingVelocity.normalized * speed;
            }

            _flyingVelocity = _context.ClampFlightVelocity(_flyingVelocity);
            _context.SetVelocity(_flyingVelocity);

            if (_windStrength < _context.MinWindStrengthForFlying || _flyingTime > _context.MaxFlyingTime)
            {
                _context.EnterFallingState(_flyingVelocity);
                return;
            }

            _context.Animator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(_flyingVelocity, Vector3.right).normalized, Vector3.right) == 1);
        }

        public void Enter()
        {
            _context.IsMatchable = false;
            _context.Animator.EnterAirborneMode();
            _context.SetDefaultLayer();
            _flyingTime = 0f;
        }

        public void OnTrainSpeedChange(Vector2 force)
        {
            _flyingVelocity += force * 0.3f;
        }

        public void OnTriggerEnter(Collider2D collision) { }

        public void SetFlyingParameters(Vector2 windVelocity, float windStrength)
        {
            _flyingVelocity = windVelocity * (_context.FlightSpeedMultiplier * _context.GlobalImpulseScale);
            _windStrength = windStrength;
        }

        public void UpdateWindEffect(Vector2 windVelocity, float windStrength)
        {
            _flyingVelocity = windVelocity * (_context.FlightSpeedMultiplier * _context.GlobalImpulseScale);
            _windStrength = windStrength;
        }
    }
}
