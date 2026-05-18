using System.Collections.Generic;
using LoveMetro.Spawning;
using NUnit.Framework;

public class PrefabPoolTests
{
    [Test]
    public void Create_ShufflesItemsWithProvidedRandom()
    {
        List<int> source = new List<int> { 1, 2, 3 };
        SequenceSpawnRandom random = new SequenceSpawnRandom(new[] { 2, 0, 0 });

        List<int> pool = PrefabPool.Create(source, random);

        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, pool);
    }

    [Test]
    public void TakeNext_RemovesFirstItemAndRefillsWhenPoolIsExhausted()
    {
        List<string> source = new List<string> { "female-a", "female-b" };
        List<string> pool = new List<string> { "female-a" };

        string prefab = PrefabPool.TakeNext(pool, source);

        Assert.AreEqual("female-a", prefab);
        CollectionAssert.AreEqual(source, pool);
    }

    [Test]
    public void TakeNext_ReturnsNullForEmptyPool()
    {
        string prefab = PrefabPool.TakeNext(new List<string>(), new List<string> { "fallback" });

        Assert.IsNull(prefab);
    }

    [Test]
    public void ShuffleList_IgnoresNullAndSingleItemLists()
    {
        List<int> single = new List<int> { 42 };

        PrefabPool.ShuffleList<int>(null, new SequenceSpawnRandom());
        PrefabPool.ShuffleList(single, new SequenceSpawnRandom(new[] { 0 }));

        CollectionAssert.AreEqual(new[] { 42 }, single);
    }

    private sealed class SequenceSpawnRandom : ISpawnRandom
    {
        private readonly Queue<int> _rangeResults;

        public SequenceSpawnRandom(IEnumerable<int> rangeResults = null)
        {
            _rangeResults = rangeResults != null
                ? new Queue<int>(rangeResults)
                : new Queue<int>();
        }

        public float Value => 0f;

        public int Range(int minInclusive, int maxExclusive)
        {
            if (_rangeResults.Count > 0)
                return _rangeResults.Dequeue();

            return minInclusive;
        }
    }
}
