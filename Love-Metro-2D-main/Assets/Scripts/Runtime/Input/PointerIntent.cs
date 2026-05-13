using UnityEngine;

namespace LoveMetro.Input
{
    public readonly struct PointerIntent
    {
        public PointerIntent(
            Vector2 direction,
            bool hasDirection,
            bool isHeld,
            float horizontalAxis,
            float horizontalVelocity,
            float verticalAxis,
            float verticalVelocity,
            Vector2 pointerWorld,
            Vector2 lastReleaseWorld,
            bool hasReleasePoint,
            float lastReleaseTime)
        {
            Direction = direction;
            HasDirection = hasDirection;
            IsHeld = isHeld;
            HorizontalAxis = horizontalAxis;
            HorizontalVelocity = horizontalVelocity;
            VerticalAxis = verticalAxis;
            VerticalVelocity = verticalVelocity;
            PointerWorld = pointerWorld;
            LastReleaseWorld = lastReleaseWorld;
            HasReleasePoint = hasReleasePoint;
            LastReleaseTime = lastReleaseTime;
        }

        public Vector2 Direction { get; }
        public bool HasDirection { get; }
        public bool IsHeld { get; }
        public float HorizontalAxis { get; }
        public float HorizontalVelocity { get; }
        public float VerticalAxis { get; }
        public float VerticalVelocity { get; }
        public Vector2 PointerWorld { get; }
        public Vector2 LastReleaseWorld { get; }
        public bool HasReleasePoint { get; }
        public float LastReleaseTime { get; }

        public Vector2 ResolvedDirection => HasDirection ? Direction : Vector2.right;

        public static PointerIntent Empty => new PointerIntent(
            Vector2.zero,
            false,
            false,
            0f,
            0f,
            0f,
            0f,
            Vector2.zero,
            Vector2.zero,
            false,
            -999f);
    }
}
