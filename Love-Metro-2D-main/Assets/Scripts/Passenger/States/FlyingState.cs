using UnityEngine;

public partial class Passenger
{
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
            Passanger._rigidbody.linearVelocity = _flyingVelocity;

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
}
