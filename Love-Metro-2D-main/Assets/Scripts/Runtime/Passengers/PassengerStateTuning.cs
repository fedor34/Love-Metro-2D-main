using UnityEngine;

namespace LoveMetro.Passengers
{
    public readonly struct PassengerStateTuning
    {
        public PassengerStateTuning(
            float additionalCollisionCheckTimePeriod,
            float grabbingHandrailChance,
            float handrailCooldown,
            Vector2 handrailStandingTimeInterval,
            float launchSensitivity,
            float minImpulseToLaunch,
            float aimAssistRadius,
            float aimAssistMaxStrength,
            float turbulenceStrength,
            float impulseToVelocityScale,
            float maxFlightSpeed,
            float flightSpeedMultiplier,
            float globalImpulseScale,
            float uniformLaunchScale,
            float uniformLaunchGamma,
            float flightHorizontalScale,
            float flightVerticalScale,
            float flightVerticalGamma,
            float minWindStrengthForFlying,
            float maxFlyingTime,
            float magnetRadius,
            float magnetForce,
            float repelRadius,
            float repelForce,
            float flightDeceleration,
            float wallBounceBoost,
            int maxBounces,
            float easeOutMinK,
            float easeOutMaxK)
        {
            AdditionalCollisionCheckTimePeriod = additionalCollisionCheckTimePeriod;
            GrabbingHandrailChance = grabbingHandrailChance;
            HandrailCooldown = handrailCooldown;
            HandrailStandingTimeInterval = handrailStandingTimeInterval;
            LaunchSensitivity = launchSensitivity;
            MinImpulseToLaunch = minImpulseToLaunch;
            AimAssistRadius = aimAssistRadius;
            AimAssistMaxStrength = aimAssistMaxStrength;
            TurbulenceStrength = turbulenceStrength;
            ImpulseToVelocityScale = impulseToVelocityScale;
            MaxFlightSpeed = maxFlightSpeed;
            FlightSpeedMultiplier = flightSpeedMultiplier;
            GlobalImpulseScale = globalImpulseScale;
            UniformLaunchScale = uniformLaunchScale;
            UniformLaunchGamma = uniformLaunchGamma;
            FlightHorizontalScale = flightHorizontalScale;
            FlightVerticalScale = flightVerticalScale;
            FlightVerticalGamma = flightVerticalGamma;
            MinWindStrengthForFlying = minWindStrengthForFlying;
            MaxFlyingTime = maxFlyingTime;
            MagnetRadius = magnetRadius;
            MagnetForce = magnetForce;
            RepelRadius = repelRadius;
            RepelForce = repelForce;
            FlightDeceleration = flightDeceleration;
            WallBounceBoost = wallBounceBoost;
            MaxBounces = maxBounces;
            EaseOutMinK = easeOutMinK;
            EaseOutMaxK = easeOutMaxK;
        }

        public float AdditionalCollisionCheckTimePeriod { get; }
        public float GrabbingHandrailChance { get; }
        public float HandrailCooldown { get; }
        public Vector2 HandrailStandingTimeInterval { get; }
        public float LaunchSensitivity { get; }
        public float MinImpulseToLaunch { get; }
        public float AimAssistRadius { get; }
        public float AimAssistMaxStrength { get; }
        public float TurbulenceStrength { get; }
        public float ImpulseToVelocityScale { get; }
        public float MaxFlightSpeed { get; }
        public float FlightSpeedMultiplier { get; }
        public float GlobalImpulseScale { get; }
        public float UniformLaunchScale { get; }
        public float UniformLaunchGamma { get; }
        public float FlightHorizontalScale { get; }
        public float FlightVerticalScale { get; }
        public float FlightVerticalGamma { get; }
        public float MinWindStrengthForFlying { get; }
        public float MaxFlyingTime { get; }
        public float MagnetRadius { get; }
        public float MagnetForce { get; }
        public float RepelRadius { get; }
        public float RepelForce { get; }
        public float FlightDeceleration { get; }
        public float WallBounceBoost { get; }
        public int MaxBounces { get; }
        public float EaseOutMinK { get; }
        public float EaseOutMaxK { get; }
    }
}
