using UnityEngine;

namespace LoveMetro.Train
{
    public sealed class TrainMotionController
    {
        public TrainMotionController(float minSpeed, float maxSpeed)
        {
            MinSpeed = Mathf.Max(0f, minSpeed);
            MaxSpeed = Mathf.Max(MinSpeed, maxSpeed);
        }

        public float MinSpeed { get; }
        public float MaxSpeed { get; }

        public float ClampSpeed(float speed)
        {
            return Mathf.Clamp(speed, 0f, MaxSpeed);
        }

        public float ApplyAcceleration(float currentSpeed, float acceleration, float deltaTime)
        {
            return ClampSpeed(currentSpeed + acceleration * Mathf.Max(0f, deltaTime));
        }
    }
}
