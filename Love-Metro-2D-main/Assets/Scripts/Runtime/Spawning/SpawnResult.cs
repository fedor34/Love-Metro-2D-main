namespace LoveMetro.Spawning
{
    public readonly struct SpawnResult
    {
        public SpawnResult(int spawnedCount, int femalesCreated, int malesCreated, int containerCount)
        {
            SpawnedCount = spawnedCount;
            FemalesCreated = femalesCreated;
            MalesCreated = malesCreated;
            ContainerCount = containerCount;
        }

        public int SpawnedCount { get; }
        public int FemalesCreated { get; }
        public int MalesCreated { get; }
        public int ContainerCount { get; }
        public bool SpawnedAny => SpawnedCount > 0;
    }
}
