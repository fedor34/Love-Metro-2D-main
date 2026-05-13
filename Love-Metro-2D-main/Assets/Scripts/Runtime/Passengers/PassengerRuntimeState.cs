using UnityEngine;

namespace LoveMetro.Passengers
{
    public readonly struct PassengerRuntimeState
    {
        public PassengerRuntimeState(
            bool isFemale,
            bool isInCouple,
            bool isMatchable,
            string stateName,
            Vector3 position,
            Vector2 velocity)
        {
            IsFemale = isFemale;
            IsInCouple = isInCouple;
            IsMatchable = isMatchable;
            StateName = stateName;
            Position = position;
            Velocity = velocity;
        }

        public bool IsFemale { get; }
        public bool IsInCouple { get; }
        public bool IsMatchable { get; }
        public string StateName { get; }
        public Vector3 Position { get; }
        public Vector2 Velocity { get; }
    }
}
