using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.Passengers.States
{
    public sealed class FallingPassengerState : IPassengerFallingState
    {
        private readonly PassengerStateContext _context;
        private readonly List<global::Passenger> _sameGenderRepelBuffer = new List<global::Passenger>(8);
        private Vector2 _currentFallingSpeed;
        private int _bounceCount;

        public FallingPassengerState(PassengerStateContext context)
        {
            _context = context;
            ResetFallingSpeeds();
        }

        public PassengerStateId Id => PassengerStateId.Falling;

        public void SetInitialFallingSpeed(Vector2 initialSpeed)
        {
            _currentFallingSpeed = initialSpeed * (_context.Tuning.FlightSpeedMultiplier * _context.Tuning.GlobalImpulseScale);
        }

        public void OnCollision(Collision2D collision)
        {
            Vector2 normal = _context.GetCollisionNormal(collision, Vector2.up);
            Diagnostics.Log($"[Passenger][falling][hit] self={_context.Name} with={(collision.transform != null ? collision.transform.name : "<null>")} normal={normal} v={_currentFallingSpeed.magnitude:F2}");

            if (collision.transform.TryGetComponent(out global::Passenger passenger))
            {
                if (_context.TryResolvePassengerImpact(passenger))
                    return;

                Vector2 selfVelocity = _currentFallingSpeed;
                Vector2 otherVelocity = _context.GetVelocity(passenger);

                _currentFallingSpeed = _context.ReflectVelocity(selfVelocity, normal, Mathf.Max(1f, _context.Tuning.WallBounceBoost));
                _context.SetVelocity(_currentFallingSpeed);
                _context.ApplyReflectedVelocity(passenger, otherVelocity, -normal, Mathf.Max(1f, _context.GetWallBounceBoost(passenger)));
            }
            else
            {
                _currentFallingSpeed = _context.ReflectVelocity(_currentFallingSpeed, normal, _context.Tuning.WallBounceBoost);
                _context.SetVelocity(_currentFallingSpeed);
                Diagnostics.Log($"[Passenger][falling][wall] {_context.Name} bounce v={_currentFallingSpeed.magnitude:F2}");
            }

            _bounceCount++;
            if (_bounceCount >= _context.Tuning.MaxBounces)
            {
                _context.SetVelocity(Vector2.zero);
                _context.ChangeState(PassengerStateId.Wandering);
            }
        }

        public void Exit()
        {
            ResetFallingSpeeds();
            _context.SetVelocity(Vector2.zero);
            _context.Animator.ExitAirborneMode();
            _context.SetDefaultLayer();
            _context.SetDamping(_context.Settings.defaultLinearDamping, _context.Settings.defaultAngularDamping);
        }

        public void UpdateState()
        {
            float speed = _currentFallingSpeed.magnitude;
            float t01 = Mathf.InverseLerp(0f, _context.Tuning.MaxFlightSpeed, speed);
            float k = Mathf.Lerp(_context.Tuning.EaseOutMinK, _context.Tuning.EaseOutMaxK, t01);
            _currentFallingSpeed *= Mathf.Pow(k, 60f * Time.deltaTime);

            global::Passenger target = _context.FindClosestOpposite(_context.Tuning.MagnetRadius);
            if (target != null)
            {
                Vector2 toTarget = (Vector2)(target.transform.position - _context.Position);
                float weight = Mathf.InverseLerp(_context.Tuning.MagnetRadius, 0f, toTarget.magnitude);
                Vector2 acceleration = toTarget.normalized * (_context.Tuning.MagnetForce * weight) * Time.deltaTime;
                Vector2 forward = _currentFallingSpeed.sqrMagnitude > 0.0001f ? _currentFallingSpeed.normalized : Vector2.right;
                float along = Vector2.Dot(acceleration, forward);
                _currentFallingSpeed += forward * along;
            }

            _context.CollectSameGenderPassengers(_sameGenderRepelBuffer);
            for (int i = 0; i < _sameGenderRepelBuffer.Count; i++)
            {
                global::Passenger other = _sameGenderRepelBuffer[i];
                if (other == null)
                    continue;

                Vector2 toOther = (Vector2)(other.transform.position - _context.Position);
                float distance = toOther.magnitude;
                if (distance < 0.001f || distance > _context.Tuning.RepelRadius)
                    continue;

                float weight = Mathf.InverseLerp(_context.Tuning.RepelRadius, 0f, distance);
                _currentFallingSpeed -= toOther.normalized * (_context.Tuning.RepelForce * weight) * Time.deltaTime;
            }

            _currentFallingSpeed = _context.ClampFlightVelocity(_currentFallingSpeed);
            _context.SetVelocity(_currentFallingSpeed);

            if (_currentFallingSpeed.magnitude <= _context.Settings.minFallingSpeed)
                _context.ChangeState(PassengerStateId.Wandering);
        }

        public void Enter()
        {
            _context.SetBodyType(RigidbodyType2D.Dynamic);
            _context.Animator.EnterAirborneMode();
            _context.SetDefaultLayer();
            _bounceCount = 0;
            _context.SetDamping(_context.Settings.airborneLinearDamping, _context.Settings.airborneAngularDamping);
        }

        public void OnTrainSpeedChange(Vector2 force)
        {
            float sensitivity = Mathf.Max(0.05f, _context.Tuning.LaunchSensitivity);
            Vector2 position = _context.Position;
            Vector2 targetWorld = _context.GetImpulseTargetWorld(position);

            float baseMagnitude = Mathf.Max(force.magnitude, 6f) * sensitivity;
            float xFromTrain = force.x * sensitivity * _context.Tuning.FlightHorizontalScale * 1.1f;
            float deltaYNorm = _context.GetNormalizedTargetDelta(position, targetWorld, vertical: true);
            float yWeight = Mathf.Pow(Mathf.Abs(deltaYNorm), _context.Tuning.FlightVerticalGamma);
            float yFromClick = Mathf.Sign(deltaYNorm) * baseMagnitude * _context.Tuning.FlightVerticalScale * yWeight;

            yFromClick += (Random.value - 0.5f) * 0.25f * _context.Tuning.TurbulenceStrength;

            global::Passenger aimTarget = _context.FindClosestOpposite(_context.Tuning.AimAssistRadius);
            if (aimTarget != null)
            {
                Vector2 toAim = (Vector2)(aimTarget.transform.position - _context.Position);
                float weight = Mathf.Clamp01(Mathf.Abs(toAim.y) / _context.Tuning.AimAssistRadius);
                yFromClick += Mathf.Sign(toAim.y) * Mathf.Min(_context.Tuning.AimAssistMaxStrength, toAim.magnitude * 0.2f) * weight;
            }

            Vector2 delta = new Vector2(xFromTrain, yFromClick) * _context.Tuning.GlobalImpulseScale;
            _currentFallingSpeed += delta;
            _currentFallingSpeed = _context.ClampFlightVelocity(_currentFallingSpeed);
        }

        public void OnTriggerEnter(Collider2D collision) { }

        private void ResetFallingSpeeds()
        {
            _currentFallingSpeed = Vector2.zero;
        }
    }
}
