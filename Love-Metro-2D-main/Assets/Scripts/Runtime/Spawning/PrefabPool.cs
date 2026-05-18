using System.Collections.Generic;

namespace LoveMetro.Spawning
{
    public static class PrefabPool
    {
        public static List<T> Create<T>(IList<T> prefabs, ISpawnRandom random)
        {
            List<T> pool = prefabs != null
                ? new List<T>(prefabs)
                : new List<T>();

            ShuffleList(pool, random ?? new UnitySpawnRandom());
            return pool;
        }

        public static T TakeNext<T>(List<T> pool, IList<T> source) where T : class
        {
            if (pool == null || pool.Count == 0)
                return null;

            T prefab = pool[0];
            pool.RemoveAt(0);

            if (pool.Count == 0 && source != null && source.Count > 0)
                pool.AddRange(source);

            return prefab;
        }

        public static void ShuffleList<T>(IList<T> list, ISpawnRandom random)
        {
            if (list == null || list.Count <= 1)
                return;

            ISpawnRandom randomSource = random ?? new UnitySpawnRandom();
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                int swapIndex = i + randomSource.Range(0, count - i);
                T temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }
    }
}
