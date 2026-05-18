using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.Spawning
{
    public interface ISpawnLocationProvider
    {
        List<Transform> CollectAvailableLocations(IList<Transform> spawnLocations);
        SpawnLocationMetrics CalculateMetrics(IList<Transform> availableLocations);
        Transform TakeNextLocation(IList<Transform> availableLocations);
        Vector3 BuildSpawnPosition(Transform spawnPoint, SpawnLocationMetrics metrics);
    }
}
