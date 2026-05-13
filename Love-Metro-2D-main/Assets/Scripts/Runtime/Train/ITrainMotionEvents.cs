using System;
using UnityEngine;

namespace LoveMetro.Train
{
    public interface ITrainMotionEvents
    {
        TrainMotionState CurrentMotionState { get; }
        event Action<Vector2> InertiaImpulseDispatched;
        event Action BrakeStarted;
        event Action BrakeEnded;
    }
}
