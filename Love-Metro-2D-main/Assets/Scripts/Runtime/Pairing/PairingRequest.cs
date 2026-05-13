using UnityEngine;

namespace LoveMetro.Pairing
{
    public readonly struct PairingRequest
    {
        public PairingRequest(global::Passenger first, global::Passenger second, float maxDistance = -1f, string source = null)
        {
            First = first;
            Second = second;
            MaxDistance = maxDistance;
            Source = source;
        }

        public global::Passenger First { get; }
        public global::Passenger Second { get; }
        public float MaxDistance { get; }
        public string Source { get; }

        public bool HasDistanceLimit => MaxDistance >= 0f;

        public float Distance
        {
            get
            {
                if (First == null || Second == null)
                    return float.PositiveInfinity;

                return Vector2.Distance(First.transform.position, Second.transform.position);
            }
        }
    }
}
