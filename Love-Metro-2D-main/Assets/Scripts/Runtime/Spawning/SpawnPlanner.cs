using System;
using System.Collections.Generic;

namespace LoveMetro.Spawning
{
    public sealed class SpawnPlanner : ISpawnPlanner
    {
        private readonly ISpawnRandom _random;

        public SpawnPlanner(ISpawnRandom random = null)
        {
            _random = random ?? new UnitySpawnRandom();
        }

        public int CalculateSpawnCount(SpawnRequest request)
        {
            int maxPossibleSpawn = Math.Min(
                request.MaxPassengersPerWave,
                Math.Min(request.AvailableLocationsCount, request.RemainingSlots));

            if (maxPossibleSpawn <= 0)
                return 0;

            int minDesired = Math.Min(request.MinPassengersPerWave, maxPossibleSpawn);
            return _random.Range(minDesired, maxPossibleSpawn + 1);
        }

        public List<bool> BuildGenderDistribution(int spawnCount)
        {
            List<bool> genderDistribution = new List<bool>(Math.Max(0, spawnCount));
            if (spawnCount <= 0)
                return genderDistribution;

            int femaleCount = spawnCount / 2;
            int maleCount = spawnCount / 2;

            if (spawnCount % 2 == 1)
            {
                if (_random.Value < 0.5f)
                    femaleCount++;
                else
                    maleCount++;
            }

            for (int i = 0; i < femaleCount; i++)
                genderDistribution.Add(true);
            for (int i = 0; i < maleCount; i++)
                genderDistribution.Add(false);

            PrefabPool.ShuffleList(genderDistribution, _random);
            return genderDistribution;
        }
    }
}
