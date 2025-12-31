using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unit tests for ScoreCounter class
/// Tests score calculation, award methods, and display updates
/// </summary>
public class ScoreCounterTests
{
    private GameObject scoreCounterObject;
    private ScoreCounter scoreCounter;

    [SetUp]
    public void Setup()
    {
        scoreCounterObject = new GameObject("TestScoreCounter");
        scoreCounter = scoreCounterObject.AddComponent<ScoreCounter>();

        // Set base points using reflection
        SetPrivateField(scoreCounter, "_basePointsPerCouple", 100);
        SetPrivateField(scoreCounter, "_totalScore", 0);
    }

    [TearDown]
    public void Teardown()
    {
        if (scoreCounterObject != null)
        {
            Object.DestroyImmediate(scoreCounterObject);
        }
    }

    [Test]
    public void GetBasePointsPerCouple_ReturnsCorrectValue()
    {
        int points = scoreCounter.GetBasePointsPerCouple();

        Assert.AreEqual(100, points);
    }

    [Test]
    public void AwardMatchPoints_IncreasesTotalScore()
    {
        int initialScore = GetPrivateField<int>(scoreCounter, "_totalScore");

        scoreCounter.AwardMatchPoints(Vector3.zero, 100);

        int finalScore = GetPrivateField<int>(scoreCounter, "_totalScore");
        Assert.AreEqual(initialScore + 100, finalScore);
    }

    [Test]
    public void AwardMatchPoints_HandlesMultipleAwards()
    {
        scoreCounter.AwardMatchPoints(Vector3.zero, 100);
        scoreCounter.AwardMatchPoints(Vector3.zero, 200);
        scoreCounter.AwardMatchPoints(Vector3.zero, 50);

        int totalScore = GetPrivateField<int>(scoreCounter, "_totalScore");
        Assert.AreEqual(350, totalScore);
    }

    [Test]
    public void AwardMatchPoints_HandlesZeroPoints()
    {
        scoreCounter.AwardMatchPoints(Vector3.zero, 0);

        int totalScore = GetPrivateField<int>(scoreCounter, "_totalScore");
        Assert.AreEqual(0, totalScore);
    }

    [Test]
    public void AwardMatchPoints_HandlesNegativePoints_AsZero()
    {
        // Most implementations clamp negative to 0
        scoreCounter.AwardMatchPoints(Vector3.zero, -50);

        int totalScore = GetPrivateField<int>(scoreCounter, "_totalScore");
        // Should not decrease score
        Assert.GreaterOrEqual(totalScore, 0);
    }

    [Test]
    public void AwardMatchPoints_VIPBonus_DoublesPoints()
    {
        // Simulate VIP scenario where points are doubled before calling AwardMatchPoints
        int basePoints = scoreCounter.GetBasePointsPerCouple();
        int vipPoints = basePoints * 2; // VIP doubles the points

        scoreCounter.AwardMatchPoints(Vector3.zero, vipPoints);

        int totalScore = GetPrivateField<int>(scoreCounter, "_totalScore");
        Assert.AreEqual(200, totalScore); // 100 * 2 = 200
    }

    [Test]
    public void ScoreAccumulation_WorksCorrectly()
    {
        // Test typical gameplay scenario
        scoreCounter.AwardMatchPoints(Vector3.zero, 100); // Normal couple
        scoreCounter.AwardMatchPoints(Vector3.zero, 100); // Normal couple
        scoreCounter.AwardMatchPoints(Vector3.zero, 200); // VIP couple

        int totalScore = GetPrivateField<int>(scoreCounter, "_totalScore");
        Assert.AreEqual(400, totalScore);
    }

    // Helper methods
    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        return default(T);
    }
}
