using NUnit.Framework;
using TMPro;
using UnityEngine;

/// <summary>
/// Unit tests for manual pairing click handling and compatibility checks.
/// </summary>
public class ManualPairingManagerTests
{
    private GameObject _managerObject;
    private ManualPairingManager _manager;
    private GameObject _cameraObject;
    private Camera _testCamera;

    [SetUp]
    public void Setup()
    {
        SetStaticProperty(typeof(ManualPairingManager), "Instance", null);

        _cameraObject = new GameObject("TestCamera");
        _cameraObject.tag = "MainCamera";
        _testCamera = _cameraObject.AddComponent<Camera>();
        _testCamera.transform.position = new Vector3(0, 0, -10);
        _testCamera.orthographic = true;
        _testCamera.orthographicSize = 5;

        _managerObject = new GameObject("TestManualPairingManager");
        _manager = _managerObject.AddComponent<ManualPairingManager>();

        SetPrivateField(_manager, "_maxPairingDistance", 3.0f);
        SetPrivateField(_manager, "_clickRadius", 0.4f);
        SetPrivateField(_manager, "_verticalSearchFactor", 2.0f);
    }

    [TearDown]
    public void Teardown()
    {
        if (_managerObject != null)
            Object.DestroyImmediate(_managerObject);

        if (_cameraObject != null)
            Object.DestroyImmediate(_cameraObject);

        foreach (var couple in Object.FindObjectsOfType<Couple>())
            Object.DestroyImmediate(couple.gameObject);
        foreach (var passenger in Object.FindObjectsOfType<Passenger>())
            Object.DestroyImmediate(passenger.gameObject);
        foreach (var scoreCounter in Object.FindObjectsOfType<ScoreCounter>())
            Object.DestroyImmediate(scoreCounter.gameObject);
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            Object.DestroyImmediate(canvas.gameObject);

        SetStaticProperty(typeof(ManualPairingManager), "Instance", null);
    }

    [Test]
    public void Singleton_InstanceIsSet_AfterAwake()
    {
        Assert.AreSame(_manager, ManualPairingManager.Instance);
    }

    [Test]
    public void HandleClick_ReturnsFalse_WhenNoCameraExists()
    {
        Object.DestroyImmediate(_cameraObject);
        _cameraObject = null;

        Assert.IsFalse(_manager.HandleClick(Vector2.zero));
    }

    [Test]
    public void HandleClick_ReturnsFalse_WhenClickingEmptySpace()
    {
        Vector2 screenPos = _testCamera.WorldToScreenPoint(new Vector3(50f, 50f, 0f));

        Assert.IsFalse(_manager.HandleClick(screenPos));
    }

    [Test]
    public void HandleClick_ReturnsTrue_WhenPassengersAreHitEvenWithoutValidPair()
    {
        var male1 = CreateMockPassenger(false, Vector3.zero);
        var male2 = CreateMockPassenger(false, new Vector3(0.1f, 0f, 0f));
        Vector2 screenPos = _testCamera.WorldToScreenPoint(Vector3.zero);

        bool result = _manager.HandleClick(screenPos);

        Assert.IsTrue(result);

        CleanupPassenger(male1);
        CleanupPassenger(male2);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenSameGender()
    {
        var male1 = CreateMockPassenger(false, Vector3.zero);
        var male2 = CreateMockPassenger(false, new Vector3(1f, 0f, 0f));

        bool result = InvokePrivateMethod<bool>(_manager, "CanPair", male1, male2);

        Assert.IsFalse(result);

        CleanupPassenger(male1);
        CleanupPassenger(male2);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenTooFarApart()
    {
        var male = CreateMockPassenger(false, Vector3.zero);
        var female = CreateMockPassenger(true, new Vector3(10f, 0f, 0f));

        bool result = InvokePrivateMethod<bool>(_manager, "CanPair", male, female);

        Assert.IsFalse(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void CanPair_ReturnsTrue_WhenValidPair()
    {
        var male = CreateMockPassenger(false, Vector3.zero);
        var female = CreateMockPassenger(true, new Vector3(2f, 0f, 0f));

        bool result = InvokePrivateMethod<bool>(_manager, "CanPair", male, female);

        Assert.IsTrue(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenSamePassenger()
    {
        var passenger = CreateMockPassenger(false, Vector3.zero);

        bool result = InvokePrivateMethod<bool>(_manager, "CanPair", passenger, passenger);

        Assert.IsFalse(result);

        CleanupPassenger(passenger);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenOneIsAlreadyInCouple()
    {
        var male = CreateMockPassenger(false, Vector3.zero, isInCouple: true);
        var female = CreateMockPassenger(true, new Vector3(1f, 0f, 0f));

        bool result = InvokePrivateMethod<bool>(_manager, "CanPair", male, female);

        Assert.IsFalse(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenOneIsNotMatchable()
    {
        var male = CreateMockPassenger(false, Vector3.zero, isMatchable: false);
        var female = CreateMockPassenger(true, new Vector3(1f, 0f, 0f));

        bool result = InvokePrivateMethod<bool>(_manager, "CanPair", male, female);

        Assert.IsFalse(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void AttemptOverlapPairing_ReturnsFalse_WhenLessThanTwoPassengers()
    {
        var passengers = new System.Collections.Generic.List<Passenger>
        {
            CreateMockPassenger(false, Vector3.zero)
        };

        bool result = InvokePrivateMethod<bool>(_manager, "AttemptOverlapPairing", passengers);

        Assert.IsFalse(result);

        foreach (var passenger in passengers)
            CleanupPassenger(passenger);
    }

    [Test]
    public void AttemptOverlapPairing_ReturnsFalse_WhenNoValidPairs()
    {
        var passengers = new System.Collections.Generic.List<Passenger>
        {
            CreateMockPassenger(false, Vector3.zero),
            CreateMockPassenger(false, new Vector3(1f, 0f, 0f))
        };

        bool result = InvokePrivateMethod<bool>(_manager, "AttemptOverlapPairing", passengers);

        Assert.IsFalse(result);

        foreach (var passenger in passengers)
            CleanupPassenger(passenger);
    }

    [Test]
    public void PairPassengers_AwardsScoreOnlyOnce()
    {
        var male = CreateMockPassenger(false, Vector3.zero);
        var female = CreateMockPassenger(true, new Vector3(1f, 0f, 0f));
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        var scoreObject = new GameObject("ScoreCounter", typeof(RectTransform), typeof(Animator), typeof(TextMeshProUGUI));
        scoreObject.transform.SetParent(canvasObject.transform);
        var scoreCounter = scoreObject.AddComponent<ScoreCounter>();
        var couplePrefab = new GameObject("CouplePrefab");
        couplePrefab.AddComponent<Couple>();

        SetPrivateField(scoreCounter, "_initialScorePointsPerCouple", 100);
        SetPrivateField(male, "CouplePref", couplePrefab);
        SetPrivateField(female, "CouplePref", couplePrefab);
        SetPrivateField(male, "_scoreCounter", scoreCounter);
        SetPrivateField(female, "_scoreCounter", scoreCounter);

        InvokePrivateMethod<object>(_manager, "PairPassengers", male, female);

        Assert.AreEqual(100, scoreCounter.CurrentScore);

        Object.DestroyImmediate(scoreObject);
        Object.DestroyImmediate(canvasObject);
        foreach (var couple in Object.FindObjectsOfType<Couple>())
            Object.DestroyImmediate(couple.gameObject);
        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    private static Passenger CreateMockPassenger(bool isFemale, Vector3 position, bool isMatchable = true, bool isInCouple = false)
    {
        var go = new GameObject("MockPassenger_" + (isFemale ? "F" : "M"));
        go.transform.position = position;

        var passenger = go.AddComponent<Passenger>();
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.5f, 1f);

        passenger.IsFemale = isFemale;
        passenger.IsMatchable = isMatchable;
        passenger.IsInCouple = isInCouple;

        return passenger;
    }

    private static void CleanupPassenger(Passenger passenger)
    {
        if (passenger != null && passenger.gameObject != null)
            Object.DestroyImmediate(passenger.gameObject);
    }

    private static T InvokePrivateMethod<T>(object obj, string methodName, params object[] args)
    {
        var method = obj.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)method.Invoke(obj, args);
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private static void SetStaticProperty(System.Type type, string propertyName, object value)
    {
        var prop = type.GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        prop?.SetValue(null, value);
    }
}
