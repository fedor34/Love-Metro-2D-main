namespace LoveMetro.Spawning
{
    public interface ISpawnRandom
    {
        float Value { get; }
        int Range(int minInclusive, int maxExclusive);
    }
}
