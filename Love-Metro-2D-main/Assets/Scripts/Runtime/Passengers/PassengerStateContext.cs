using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.Passengers
{
    public readonly struct PassengerStateContext
    {
        public PassengerStateContext(global::Passenger passenger)
            : this((IPassengerStateHost)passenger)
        {
        }

        internal PassengerStateContext(IPassengerStateHost host)
        {
            _host = host;
        }

        private readonly IPassengerStateHost _host;

        private IPassengerStateHost Host
        {
            get
            {
                if (_host == null)
                    throw new System.InvalidOperationException("Passenger state context is not bound to a host.");

                return _host;
            }
        }

        public string Name => _host != null ? _host.Name : string.Empty;
        public Vector3 Position
        {
            get => Host.Position;
            set => Host.Position = value;
        }

        public PassengerSettings Settings => Host.Settings;
        public PassangerAnimator Animator => Host.Animator;
        public bool IsInCouple => Host.IsInCouple;
        public bool IsMatchable
        {
            get => Host.IsMatchable;
            set => Host.IsMatchable = value;
        }

        public Vector2 CurrentMovingDirection
        {
            get => Host.CurrentMovingDirection;
            set => Host.CurrentMovingDirection = value;
        }

        public float TimeWithoutHolding
        {
            get => Host.TimeWithoutHolding;
            set => Host.TimeWithoutHolding = value;
        }

        public PassengerStateTuning Tuning => Host.Tuning;

        public void ChangeState(PassengerStateId id)
        {
            Host.ChangeState(id);
        }

        public void EnterFallingState(Vector2 initialVelocity)
        {
            Host.EnterFallingState(initialVelocity);
        }

        public void SetBodyType(RigidbodyType2D bodyType)
        {
            Host.SetBodyType(bodyType);
        }

        public void SetDefaultLayer()
        {
            Host.SetDefaultLayer();
        }

        public void SetColliderEnabled(bool enabled)
        {
            Host.SetColliderEnabled(enabled);
        }

        public void SetVelocity(Vector2 velocity)
        {
            Host.SetVelocity(velocity);
        }

        public void AddForce(Vector2 force, ForceMode2D mode)
        {
            Host.AddForce(force, mode);
        }

        public Vector2 GetVelocity()
        {
            return Host.GetVelocity();
        }

        public void SetDamping(float linearDamping, float angularDamping)
        {
            Host.SetDamping(linearDamping, angularDamping);
        }

        public Vector2 ClampFlightVelocity(Vector2 velocity)
        {
            return Host.ClampFlightVelocity(velocity);
        }

        public Vector2 ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            return Host.ReflectVelocity(velocity, normal, boostMultiplier);
        }

        public Vector2 ScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale)
        {
            return Host.ScaleLaunchVelocity(velocity, speedMultiplier, impulseScale);
        }

        public void ApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            Host.ApplyReflectedVelocity(velocity, normal, boostMultiplier);
        }

        public void ApplyReflectedVelocity(global::Passenger passenger, Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            Host.ApplyReflectedVelocity(passenger, velocity, normal, boostMultiplier);
        }

        public Vector2 GetVelocity(global::Passenger passenger)
        {
            return Host.GetVelocity(passenger);
        }

        public float GetWallBounceBoost(global::Passenger passenger)
        {
            return Host.GetWallBounceBoost(passenger);
        }

        public void ForwardTrainSpeedChangeToCurrentState(Vector2 force)
        {
            Host.ForwardTrainSpeedChangeToCurrentState(force);
        }

        public Vector2 GetImpulseTargetWorld(Vector2 position)
        {
            return Host.GetImpulseTargetWorld(position);
        }

        public float GetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical)
        {
            return Host.GetNormalizedTargetDelta(position, targetWorld, vertical);
        }

        public Vector2 GetCollisionNormal(Collision2D collision, Vector2 fallback)
        {
            return Host.GetCollisionNormal(collision, fallback);
        }

        public bool TryResolvePassengerImpact(global::Passenger other)
        {
            return Host.TryResolvePassengerImpact(other);
        }

        public global::Passenger FindClosestOpposite(float radius)
        {
            return Host.FindClosestOpposite(radius);
        }

        public void CollectSameGenderPassengers(List<global::Passenger> results)
        {
            Host.CollectSameGenderPassengers(results);
        }

        public int GetContacts(ContactPoint2D[] contactPoints)
        {
            return Host.GetContacts(contactPoints);
        }

        public void AttachHandrail(global::HandRailPosition handrail)
        {
            Host.AttachHandrail(handrail);
        }

        public void ReleaseHandrail()
        {
            Host.ReleaseHandrail();
        }

        public void RemovePassengerAndDestroy()
        {
            Host.RemovePassengerAndDestroy();
        }

        public void LogEvent(string category, string message)
        {
            Host.LogEvent(category, message);
        }
    }
}
