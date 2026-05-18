using System.Collections.Generic;
using LoveMetro.Spawning;
using NUnit.Framework;

public class SpawnPlannerTests
{
    [Test]
    public void CalculateSpawnCount_ClampsToAvailableLocationsAndRemainingSlots()
    {
        SequenceSpawnRandom random = new SequenceSpawnRandom(rangeResults: new[] { 2 });
        SpawnPlanner planner = new SpawnPlanner(random);
        SpawnRequest request = new SpawnRequest(
            availableLocationsCount: 10,
            currentPassengerCount: 18,
            maxPassengersInScene: 20,
            minPassengersPerWave: 5,
            maxPassengersPerWave: 7);

        int count = planner.CalculateSpawnCount(request);

        Assert.AreEqual(2, count);
        Assert.AreEqual((2, 3), random.RangeCalls[0]);
    }

    [Test]
    public void CalculateSpawnCount_ReturnsZeroWhenSceneIsFull()
    {
        SpawnPlanner planner = new SpawnPlanner(new SequenceSpawnRandom());
        SpawnRequest request = new SpawnRequest(
            availableLocationsCount: 5,
            currentPassengerCount: 20,
            maxPassengersInScene: 20,
            minPassengersPerWave: 5,
            maxPassengersPerWave: 7);

        Assert.AreEqual(0, planner.CalculateSpawnCount(request));
    }

    [Test]
    public void BuildGenderDistribution_BalancesEvenSpawnCount()
    {
        SpawnPlanner planner = new SpawnPlanner(new SequenceSpawnRandom());

        List<bool> distribution = planner.BuildGenderDistribution(6);

        Assert.AreEqual(6, distribution.Count);
        Assert.AreEqual(3, Count(distribution, true));
        Assert.AreEqual(3, Count(distribution, false));
    }

    [Test]
    public void BuildGenderDistribution_UsesRandomTieBreakerForOddSpawnCount()
    {
        SpawnPlanner femaleFavoredPlanner = new SpawnPlanner(new SequenceSpawnRandom(value: 0.25f));
        SpawnPlanner maleFavoredPlanner = new SpawnPlanner(new SequenceSpawnRandom(value: 0.75f));

        List<bool> femaleFavored = femaleFavoredPlanner.BuildGenderDistribution(5);
        List<bool> maleFavored = maleFavoredPlanner.BuildGenderDistribution(5);

        Assert.AreEqual(3, Count(femaleFavored, true));
        Assert.AreEqual(2, Count(femaleFavored, false));
        Assert.AreEqual(2, Count(maleFavored, true));
        Assert.AreEqual(3, Count(maleFavored, false));
    }

    private static int Count(List<bool> values, bool expected)
    {
        int count = 0;
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == expected)
                count++;
        }

        return count;
    }

    private sealed class SequenceSpawnRandom : ISpawnRandom
    {
        private readonly Queue<int> _rangeResults;

        public SequenceSpawnRandom(float value = 0f, IEnumerable<int> rangeResults = null)
        {
            Value = value;
            _rangeResults = rangeResults != null
                ? new Queue<int>(rangeResults)
                : new Queue<int>();
            RangeCalls = new List<(int MinInclusive, int MaxExclusive)>();
        }

        public float Value { get; }
        public List<(int MinInclusive, int MaxExclusive)> RangeCalls { get; }

        public int Range(int minInclusive, int maxExclusive)
        {
            RangeCalls.Add((minInclusive, maxExclusive));
            if (_rangeResults.Count > 0)
                return _rangeResults.Dequeue();

            return minInclusive;
        }
    }
}
