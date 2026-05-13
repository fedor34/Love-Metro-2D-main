using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.Passengers
{
    internal interface IPassengerStateHost
    {
        string Name { get; }
        Vector3 Position { get; set; }
        PassengerSettings Settings { get; }
        PassangerAnimator Animator { get; }
        bool IsInCouple { get; }
        bool IsMatchable { get; set; }
        Vector2 CurrentMovingDirection { get; set; }
        float TimeWithoutHolding { get; set; }

        float AdditionalCollisionCheckTimePeriod { get; }
        float GrabbingHandrailChance { get; }
        float HandrailCooldown { get; }
        Vector2 HandrailStandingTimeInterval { get; }
        float LaunchSensitivity { get; }
        float MinImpulseToLaunch { get; }
        float AimAssistRadius { get; }
        float AimAssistMaxStrength { get; }
        float TurbulenceStrength { get; }
        float ImpulseToVelocityScale { get; }
        float MaxFlightSpeed { get; }
        float FlightSpeedMultiplier { get; }
        float GlobalImpulseScale { get; }
        float UniformLaunchScale { get; }
        float UniformLaunchGamma { get; }
        float FlightHorizontalScale { get; }
        float FlightVerticalScale { get; }
        float FlightVerticalGamma { get; }
        float MinWindStrengthForFlying { get; }
        float MaxFlyingTime { get; }
        float MagnetRadius { get; }
        float MagnetForce { get; }
        float RepelRadius { get; }
        float RepelForce { get; }
        float FlightDeceleration { get; }
        float WallBounceBoost { get; }
        int MaxBounces { get; }
        float EaseOutMinK { get; }
        float EaseOutMaxK { get; }

        void ChangeState(PassengerStateId id);
        void EnterFallingState(Vector2 initialVelocity);
        void SetBodyType(RigidbodyType2D bodyType);
        void SetDefaultLayer();
        void SetColliderEnabled(bool enabled);
        void SetVelocity(Vector2 velocity);
        void AddForce(Vector2 force, ForceMode2D mode);
        Vector2 GetVelocity();
        Vector2 GetVelocity(global::Passenger passenger);
        void SetDamping(float linearDamping, float angularDamping);
        Vector2 ClampFlightVelocity(Vector2 velocity);
        Vector2 ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier);
        Vector2 ScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale);
        void ApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier);
        void ApplyReflectedVelocity(global::Passenger passenger, Vector2 velocity, Vector2 normal, float boostMultiplier);
        float GetWallBounceBoost(global::Passenger passenger);
        void ForwardTrainSpeedChangeToCurrentState(Vector2 force);
        Vector2 GetImpulseTargetWorld(Vector2 position);
        float GetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical);
        Vector2 GetCollisionNormal(Collision2D collision, Vector2 fallback);
        bool TryResolvePassengerImpact(global::Passenger other);
        global::Passenger FindClosestOpposite(float radius);
        void CollectSameGenderPassengers(List<global::Passenger> results);
        int GetContacts(ContactPoint2D[] contactPoints);
        void AttachHandrail(global::HandRailPosition handrail);
        void ReleaseHandrail();
        void RemovePassengerAndDestroy();
        void LogEvent(string category, string message);
    }
}
