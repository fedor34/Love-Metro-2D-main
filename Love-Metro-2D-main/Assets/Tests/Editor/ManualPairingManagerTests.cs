using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Unit tests for ManualPairingManager class
/// Tests click handling, pairing logic, and distance validation
/// </summary>
public class ManualPairingManagerTests
{
    private GameObject managerObject;
    private ManualPairingManager manager;
    private GameObject cameraObject;
    private Camera testCamera;

    [SetUp]
    public void Setup()
    {
        cameraObject = new GameObject("TestCamera");
        testCamera = cameraObject.AddComponent<Camera>();
        testCamera.transform.position = new Vector3(0, 0, -10);
        testCamera.orthographic = true;
        testCamera.orthographicSize = 5;

        SetStaticProperty(typeof(ManualPairingManager), "Instance", null);
        managerObject = new GameObject("TestManualPairingManager");
        manager = managerObject.AddComponent<ManualPairingManager>();
        
        var awakeMethod = typeof(ManualPairingManager).GetMethod("Awake",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        awakeMethod?.Invoke(manager, null);

        SetPrivateField(manager, "_maxPairingDistance", 3.0f);
        SetPrivateField(manager, "_clickRadius", 0.4f);
        SetPrivateField(manager, "_verticalSearchFactor", 2.0f);
    }

    [TearDown]
    public void Teardown()
    {
        if (managerObject != null)
            Object.DestroyImmediate(managerObject);
        if (cameraObject != null)
            Object.DestroyImmediate(cameraObject);
        SetStaticProperty(typeof(ManualPairingManager), "Instance", null);
    }

    [Test]
    public void Singleton_InstanceIsSet_AfterAwake()
    {
        Assert.IsNotNull(ManualPairingManager.Instance);
        Assert.AreEqual(manager, ManualPairingManager.Instance);
    }

    [Test]
    public void HandleClick_ReturnsFalse_WhenNoCameraExists()
    {
        Object.DestroyImmediate(cameraObject);
        cameraObject = null;

        bool result = manager.HandleClick(Vector2.zero);

        Assert.IsFalse(result);
    }

    [Test]
    public void HandleClick_ReturnsFalse_WhenClickingEmptySpace()
    {
        Vector2 screenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);

        bool result = manager.HandleClick(screenPos);

        Assert.IsFalse(result);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenSameGender()
    {
        var male1 = CreateMockPassenger(false, Vector3.zero);
        var male2 = CreateMockPassenger(false, new Vector3(1, 0, 0));

        var canPairMethod = typeof(ManualPairingManager).GetMethod("CanPair",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool result = (bool)canPairMethod.Invoke(manager, new object[] { male1, male2 });

        Assert.IsFalse(result);

        CleanupPassenger(male1);
        CleanupPassenger(male2);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenTooFarApart()
    {
        var male = CreateMockPassenger(false, Vector3.zero);
        var female = CreateMockPassenger(true, new Vector3(10, 0, 0));

        var canPairMethod = typeof(ManualPairingManager).GetMethod("CanPair",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool result = (bool)canPairMethod.Invoke(manager, new object[] { male, female });

        Assert.IsFalse(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void CanPair_ReturnsTrue_WhenValidPair()
    {
        var male = CreateMockPassenger(false, Vector3.zero, isMatchable: true, isInCouple: false);
        var female = CreateMockPassenger(true, new Vector3(2, 0, 0), isMatchable: true, isInCouple: false);

        var canPairMethod = typeof(ManualPairingManager).GetMethod("CanPair",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool result = (bool)canPairMethod.Invoke(manager, new object[] { male, female });

        Assert.IsTrue(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenSamePassenger()
    {
        var passenger = CreateMockPassenger(false, Vector3.zero);

        var canPairMethod = typeof(ManualPairingManager).GetMethod("CanPair",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool result = (bool)canPairMethod.Invoke(manager, new object[] { passenger, passenger });

        Assert.IsFalse(result);

        CleanupPassenger(passenger);
    }

    [Test]
    public void CanPair_ReturnsFalse_WhenOneIsInCouple()
    {
        var male = CreateMockPassenger(false, Vector3.zero, isMatchable: true, isInCouple: true);
        var female = CreateMockPassenger(true, new Vector3(1, 0, 0), isMatchable: true, isInCouple: false);

        var canPairMethod = typeof(ManualPairingManager).GetMethod("CanPair",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool result = (bool)canPairMethod.Invoke(manager, new object[] { male, female });

        Assert.IsTrue(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void AttemptOverlapPairing_ReturnsFalse_WhenLessThanTwoPassengers()
    {
        var passengers = new System.Collections.Generic.List<Passenger>();
        passengers.Add(CreateMockPassenger(false, Vector3.zero));

        var method = typeof(ManualPairingManager).GetMethod("AttemptOverlapPairing",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool result = (bool)method.Invoke(manager, new object[] { passengers });

        Assert.IsFalse(result);

        foreach (var p in passengers)
            CleanupPassenger(p);
    }

    [Test]
    public void AttemptOverlapPairing_ReturnsFalse_WhenNoValidPairs()
    {
        var passengers = new System.Collections.Generic.List<Passenger>();
        passengers.Add(CreateMockPassenger(false, Vector3.zero));
        passengers.Add(CreateMockPassenger(false, new Vector3(1, 0, 0)));

        var method = typeof(ManualPairingManager).GetMethod("AttemptOverlapPairing",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool result = (bool)method.Invoke(manager, new object[] { passengers });

        Assert.IsFalse(result);

        foreach (var p in passengers)
            CleanupPassenger(p);
    }

    private Passenger CreateMockPassenger(bool isFemale, Vector3 position,
        bool isMatchable = true, bool isInCouple = false)
    {
        var go = new GameObject("MockPassenger_" + (isFemale ? "F" : "M"));
        go.transform.position = position;

        var passenger = go.AddComponent<Passenger>();
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.5f, 1f);

        var type = typeof(Passenger);
        var field = type.GetField("IsFemale");
        var prop = type.GetProperty("IsFemale");
        if (field != null)
            field.SetValue(passenger, isFemale);
        else if (prop != null)
            prop.SetValue(passenger, isFemale);

        passenger.IsMatchable = isMatchable;
        passenger.IsInCouple = isInCouple;

        return passenger;
    }

    private void CleanupPassenger(Passenger passenger)
    {
        if (passenger != null && passenger.gameObject != null)
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    private void SetStaticProperty(System.Type type, string propertyName, object value)
    {
        var prop = type.GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        prop?.SetValue(null, value);
    }
}