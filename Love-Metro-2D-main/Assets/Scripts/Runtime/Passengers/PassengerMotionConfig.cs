using UnityEngine;

namespace LoveMetro.Passengers
{
    public readonly struct PassengerMotionConfig
    {
        public PassengerMotionConfig(
            float maxFlightSpeed,
            float minFallingSpeed,
            float magnetRadius,
            float magnetForce,
            float repelRadius,
            float repelForce,
            float rematchCooldown)
        {
            MaxFlightSpeed = maxFlightSpeed;
            MinFallingSpeed = minFallingSpeed;
            MagnetRadius = magnetRadius;
            MagnetForce = magnetForce;
            RepelRadius = repelRadius;
            RepelForce = repelForce;
            RematchCooldown = rematchCooldown;
        }

        public float MaxFlightSpeed { get; }
        public float MinFallingSpeed { get; }
        public float MagnetRadius { get; }
        public float MagnetForce { get; }
        public float RepelRadius { get; }
        public float RepelForce { get; }
        public float RematchCooldown { get; }

        public static PassengerMotionConfig FromSettings(global::PassengerSettings settings)
        {
            settings = global::PassengerSettings.Resolve(settings);
            return new PassengerMotionConfig(
                settings.maxFlightSpeed,
                settings.minFallingSpeed,
                settings.magnetRadius,
                settings.magnetForce,
                settings.repelRadius,
                settings.repelForce,
                settings.rematchCooldown);
        }

        public Vector2 ClampVelocity(Vector2 velocity)
        {
            return velocity.magnitude > MaxFlightSpeed
                ? velocity.normalized * MaxFlightSpeed
                : velocity;
        }
    }
}
