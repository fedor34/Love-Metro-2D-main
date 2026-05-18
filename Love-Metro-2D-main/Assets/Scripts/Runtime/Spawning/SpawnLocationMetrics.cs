namespace LoveMetro.Spawning
{
    public readonly struct SpawnLocationMetrics
    {
        public SpawnLocationMetrics(float typicalY, float minY, float maxY, bool hasTypicalY)
        {
            TypicalY = typicalY;
            MinY = minY;
            MaxY = maxY;
            HasTypicalY = hasTypicalY;
        }

        public float TypicalY { get; }
        public float MinY { get; }
        public float MaxY { get; }
        public bool HasTypicalY { get; }

        public static SpawnLocationMetrics Empty => new SpawnLocationMetrics(0f, 0f, 0f, false);
    }
}
