using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.Spawning
{
    public sealed class SpawnLocationProvider : ISpawnLocationProvider
    {
        private const float LowSpawnPointYOffset = 0.5f;

        public List<Transform> CollectAvailableLocations(IList<Transform> spawnLocations)
        {
            int capacity = spawnLocations != null ? spawnLocations.Count : 0;
            List<Transform> availableLocations = new List<Transform>(capacity);
            if (spawnLocations == null)
                return availableLocations;

            for (int i = 0; i < spawnLocations.Count; i++)
            {
                Transform location = spawnLocations[i];
                if (location != null)
                    availableLocations.Add(location);
            }

            return availableLocations;
        }

        public SpawnLocationMetrics CalculateMetrics(IList<Transform> availableLocations)
        {
            if (availableLocations == null || availableLocations.Count == 0)
                return SpawnLocationMetrics.Empty;

            float totalY = 0f;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            int count = 0;

            for (int i = 0; i < availableLocations.Count; i++)
            {
                Transform location = availableLocations[i];
                if (location == null)
                    continue;

                float y = location.position.y;
                totalY += y;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
                count++;
            }

            if (count == 0)
                return SpawnLocationMetrics.Empty;

            return new SpawnLocationMetrics(totalY / count, minY, maxY, true);
        }

        public Transform TakeNextLocation(IList<Transform> availableLocations)
        {
            if (availableLocations == null || availableLocations.Count == 0)
                return null;

            Transform spawnPoint = availableLocations[0];
            availableLocations.RemoveAt(0);
            return spawnPoint;
        }

        public Vector3 BuildSpawnPosition(Transform spawnPoint, SpawnLocationMetrics metrics)
        {
            if (spawnPoint == null)
                return Vector3.zero;

            Vector3 position = spawnPoint.position;
            if (metrics.HasTypicalY && position.y < metrics.TypicalY - LowSpawnPointYOffset)
                position.y = metrics.TypicalY;

            return position;
        }
    }
}
