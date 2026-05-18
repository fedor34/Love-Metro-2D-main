using UnityEngine;

namespace LoveMetro.Spawning
{
    public sealed class UnitySpawnRandom : ISpawnRandom
    {
        public float Value => Random.value;

        public int Range(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }
    }
}
