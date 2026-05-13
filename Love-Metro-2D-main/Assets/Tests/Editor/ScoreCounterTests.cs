using NUnit.Framework;
using TMPro;
using UnityEngine;

/// <summary>
/// Unit tests for score bookkeeping and braking-session counters.
/// </summary>
public class ScoreCounterTests
{
    private GameObject _canvasObject;
    private GameObject _scoreCounterObject;
    private ScoreCounter _scoreCounter;

    [SetUp]
    public void Setup()
    {
        _canvasObject = new GameObject("ScoreHudCanvas", typeof(RectTransform), typeof(Canvas));

        _scoreCounterObject = new GameObject("TestScoreCounter");
        _scoreCounterObject.transform.SetParent(_canvasObject.transform);
        _scoreCounterObject.AddComponent<RectTransform>();
        _scoreCounterObject.AddComponent<Animator>();
        _scoreCounterObject.AddComponent<TextMeshProUGUI>();
        _scoreCounter = _scoreCounterObject.AddComponent<ScoreCounter>();

        SetPrivateField(_scoreCounter, "_initialScorePointsPerCouple", 100);
    }

    [TearDown]
    public void Teardown()
    {
        if (_scoreCounterObject != null)
            Object.DestroyImmediate(_scoreCounterObject);

        if (_canvasObject != null)
            Object.DestroyImmediate(_canvasObject);
    }

    [Test]
    public void GetBasePointsPerCouple_ReturnsConfiguredValue()
    {
        Assert.AreEqual(100, _scoreCounter.GetBasePointsPerCouple());
    }

    [Test]
    public void AwardMatchPoints_IncreasesCurrentScore()
    {
        _scoreCounter.AwardMatchPoints(Vector3.zero, 125);

        Assert.AreEqual(125, _scoreCounter.CurrentScore);
    }

    [Test]
    public void AwardMatchPoints_ClampsNegativePointsToZero()
    {
        _scoreCounter.AwardMatchPoints(Vector3.zero, -50);

        Assert.AreEqual(0, _scoreCounter.CurrentScore);
    }

    [Test]
    public void UpdateScorePointFromMatching_UsesConfiguredBasePoints()
    {
        _scoreCounter.UpdateScorePointFromMatching(Vector3.zero);

        Assert.AreEqual(100, _scoreCounter.CurrentScore);
    }

    [Test]
    public void BrakingSession_TracksOnlyMatchesMadeWhileBraking()
    {
        _scoreCounter.StartBrakingSession();
        _scoreCounter.AwardMatchPoints(Vector3.zero, 100);
        _scoreCounter.EndBrakingSession();
        _scoreCounter.AwardMatchPoints(Vector3.zero, 100);

        Assert.AreEqual(1, _scoreCounter.MatchesInCurrentBrake);
        Assert.IsFalse(_scoreCounter.IsBrakingInProgress);
        Assert.AreEqual(200, _scoreCounter.CurrentScore);
    }

    [Test]
    public void ApplyPenalty_SubtractsFromCurrentScoreWithoutFloatingTextPrefab()
    {
        _scoreCounter.AwardMatchPoints(Vector3.zero, 200);

        _scoreCounter.ApplyPenalty(75, Vector3.zero);

        Assert.AreEqual(125, _scoreCounter.CurrentScore);
    }

    [Test]
    public void Awake_CreatesHudTextDisplayWhenSceneObjectHasNoTmpText()
    {
        var scoreObject = new GameObject("ScoreCounterWithoutText", typeof(RectTransform), typeof(Animator));
        scoreObject.transform.SetParent(_canvasObject.transform);

        try
        {
            var scoreCounter = scoreObject.AddComponent<ScoreCounter>();

            InvokePrivateMethod(scoreCounter, "Awake");

            Transform hudBadge = _canvasObject.transform.Find("ScoreCounterBadge");
            Transform hudText = hudBadge != null ? hudBadge.Find("ScoreCounterText") : null;
            Assert.IsNotNull(hudBadge);
            Assert.IsNotNull(hudText);
            Assert.IsNotNull(hudText.GetComponent<TextMeshProUGUI>());
        }
        finally
        {
            Object.DestroyImmediate(scoreObject);
        }
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private static void InvokePrivateMethod(object obj, string methodName)
    {
        var method = obj.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(obj, null);
    }
}
