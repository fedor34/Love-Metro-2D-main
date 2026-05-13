using UnityEngine;

namespace LoveMetro.Train
{
    public readonly struct TrainMotionState
    {
        public TrainMotionState(float currentSpeed, float currentAcceleration, bool isBraking, bool isStopped, Vector2 lastInertiaImpulse)
        {
            CurrentSpeed = currentSpeed;
            CurrentAcceleration = currentAcceleration;
            IsBraking = isBraking;
            IsStopped = isStopped;
            LastInertiaImpulse = lastInertiaImpulse;
        }

        public float CurrentSpeed { get; }
        public float CurrentAcceleration { get; }
        public bool IsBraking { get; }
        public bool IsStopped { get; }
        public Vector2 LastInertiaImpulse { get; }
    }
}
