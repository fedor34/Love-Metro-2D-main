using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.Passengers
{
    public readonly struct PassengerStateContext
    {
        private readonly Func<PassengerStateId, IPassengerState> _legacyStateFactory;

        public PassengerStateContext(
            global::Passenger passenger,
            Func<PassengerStateId, IPassengerState> legacyStateFactory = null)
        {
            Passenger = passenger;
            _legacyStateFactory = legacyStateFactory;
        }

        public global::Passenger Passenger { get; }
        public string Name => Passenger != null ? Passenger.name : string.Empty;
        public Transform Transform => Passenger.transform;
        public Vector3 Position
        {
            get => Passenger.transform.position;
            set => Passenger.transform.position = value;
        }

        public PassengerSettings Settings => Passenger.Settings;
        public PassangerAnimator Animator => Passenger.StateAnimator;
        public bool IsInCouple => Passenger.IsInCouple;
        public bool IsMatchable
        {
            get => Passenger.IsMatchable;
            set => Passenger.IsMatchable = value;
        }

        public Vector2 CurrentMovingDirection
        {
            get => Passenger.StateCurrentMovingDirection;
            set => Passenger.StateCurrentMovingDirection = value;
        }

        public float TimeWithoutHolding
        {
            get => Passenger.StateTimeWithoutHolding;
            set => Passenger.StateTimeWithoutHolding = value;
        }

        public float AdditionalCollisionCheckTimePeriod => Passenger.StateAdditionalCollisionCheckTimePeriod;
        public float GrabbingHandrailChance => Passenger.StateGrabbingHandrailChance;
        public float HandrailCooldown => Passenger.StateHandrailCooldown;
        public Vector2 HandrailStandingTimeInterval => Passenger.StateHandrailStandingTimeInterval;
        public float LaunchSensitivity => Passenger.StateLaunchSensitivity;
        public float MinImpulseToLaunch => Passenger.StateMinImpulseToLaunch;
        public float AimAssistRadius => Passenger.StateAimAssistRadius;
        public float AimAssistMaxStrength => Passenger.StateAimAssistMaxStrength;
        public float TurbulenceStrength => Passenger.StateTurbulenceStrength;
        public float ImpulseToVelocityScale => Passenger.StateImpulseToVelocityScale;
        public float MaxFlightSpeed => Passenger.StateMaxFlightSpeed;
        public float FlightSpeedMultiplier => Passenger.StateFlightSpeedMultiplier;
        public float GlobalImpulseScale => Passenger.StateGlobalImpulseScale;
        public float UniformLaunchScale => Passenger.StateUniformLaunchScale;
        public float UniformLaunchGamma => Passenger.StateUniformLaunchGamma;
        public float FlightHorizontalScale => Passenger.StateFlightHorizontalScale;
        public float FlightVerticalScale => Passenger.StateFlightVerticalScale;
        public float FlightVerticalGamma => Passenger.StateFlightVerticalGamma;
        public float MinWindStrengthForFlying => Passenger.StateMinWindStrengthForFlying;
        public float MaxFlyingTime => Passenger.StateMaxFlyingTime;
        public float MagnetRadius => Passenger.StateMagnetRadius;
        public float MagnetForce => Passenger.StateMagnetForce;
        public float RepelRadius => Passenger.StateRepelRadius;
        public float RepelForce => Passenger.StateRepelForce;
        public float FlightDeceleration => Passenger.StateFlightDeceleration;
        public float WallBounceBoost => Passenger.StateWallBounceBoost;
        public int MaxBounces => Passenger.StateMaxBounces;
        public float EaseOutMinK => Passenger.StateEaseOutMinK;
        public float EaseOutMaxK => Passenger.StateEaseOutMaxK;

        public IPassengerState CreateLegacyState(PassengerStateId id)
        {
            return _legacyStateFactory?.Invoke(id);
        }

        public void ChangeState(PassengerStateId id)
        {
            Passenger.StateChangeState(id);
        }

        public void EnterFallingState(Vector2 initialVelocity)
        {
            Passenger.StateEnterFallingState(initialVelocity);
        }

        public void SetBodyType(RigidbodyType2D bodyType)
        {
            Passenger.StateSetBodyType(bodyType);
        }

        public void SetDefaultLayer()
        {
            Passenger.StateSetDefaultLayer();
        }

        public void SetVelocity(Vector2 velocity)
        {
            Passenger.StateSetVelocity(velocity);
        }

        public void AddForce(Vector2 force, ForceMode2D mode)
        {
            Passenger.StateAddForce(force, mode);
        }

        public Vector2 GetVelocity()
        {
            return Passenger.StateCurrentVelocity;
        }

        public void SetDamping(float linearDamping, float angularDamping)
        {
            Passenger.StateSetDamping(linearDamping, angularDamping);
        }

        public Vector2 ClampFlightVelocity(Vector2 velocity)
        {
            return Passenger.StateClampFlightVelocity(velocity);
        }

        public Vector2 ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            return Passenger.StateReflectVelocity(velocity, normal, boostMultiplier);
        }

        public Vector2 ScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale)
        {
            return Passenger.StateScaleLaunchVelocity(velocity, speedMultiplier, impulseScale);
        }

        public void ApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            Passenger.StateApplyReflectedVelocity(velocity, normal, boostMultiplier);
        }

        public void ApplyReflectedVelocity(global::Passenger passenger, Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            passenger.StateApplyReflectedVelocity(velocity, normal, boostMultiplier);
        }

        public Vector2 GetVelocity(global::Passenger passenger)
        {
            return passenger.StateCurrentVelocity;
        }

        public float GetWallBounceBoost(global::Passenger passenger)
        {
            return passenger.StateWallBounceBoost;
        }

        public void ForwardTrainSpeedChangeToCurrentState(Vector2 force)
        {
            Passenger.StateForwardTrainSpeedChangeToCurrentState(force);
        }

        public Vector2 GetImpulseTargetWorld(Vector2 position)
        {
            return Passenger.StateGetImpulseTargetWorld(position);
        }

        public float GetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical)
        {
            return Passenger.StateGetNormalizedTargetDelta(position, targetWorld, vertical);
        }

        public Vector2 GetCollisionNormal(Collision2D collision, Vector2 fallback)
        {
            return Passenger.StateGetCollisionNormal(collision, fallback);
        }

        public bool TryResolvePassengerImpact(global::Passenger other)
        {
            return Passenger.StateTryResolvePassengerImpact(other);
        }

        public global::Passenger FindClosestOpposite(float radius)
        {
            return Passenger.StateFindClosestOpposite(radius);
        }

        public void CollectSameGenderPassengers(List<global::Passenger> results)
        {
            Passenger.StateCollectSameGenderPassengers(results);
        }

        public int GetContacts(ContactPoint2D[] contactPoints)
        {
            return Passenger.StateGetContacts(contactPoints);
        }

        public void AttachHandrail(global::HandRailPosition handrail)
        {
            Passenger.StateAttachHandrail(handrail);
        }

        public void LogEvent(string category, string message)
        {
            Passenger.StateLogEvent(category, message);
        }
    }
}
