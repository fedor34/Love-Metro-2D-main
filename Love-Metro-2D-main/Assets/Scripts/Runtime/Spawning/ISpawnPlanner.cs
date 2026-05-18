using System.Collections.Generic;

namespace LoveMetro.Spawning
{
    public interface ISpawnPlanner
    {
        int CalculateSpawnCount(SpawnRequest request);
        List<bool> BuildGenderDistribution(int spawnCount);
    }
}
