using NUnit.Framework;
using UnityEngine;

public class CouplesManagerTests
{
    private GameObject _managerObject;
    private CouplesManager _manager;
    private GameObject _registryObject;
    private PassengerRegistry _registry;

    [SetUp]
    public void Setup()
    {
        SetStaticProperty(typeof(CouplesManager), "Instance", null);
        SetStaticProperty(typeof(PassengerRegistry), "Instance", null);

        _managerObject = new GameObject("TestCouplesManager");
        _manager = _managerObject.AddComponent<CouplesManager>();
        InvokePrivateMethod(_manager, "Awake");

        _registryObject = new GameObject("TestPassengerRegistry");
        _registry = _registryObject.AddComponent<PassengerRegistry>();
        InvokePrivateMethod(_registry, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var couple in Object.FindObjectsOfType<Couple>())
            Object.DestroyImmediate(couple.gameObject);

        foreach (var passenger in Object.FindObjectsOfType<Passenger>())
            Object.DestroyImmediate(passenger.gameObject);

        if (_managerObject != null)
            Object.DestroyImmediate(_managerObject);

        if (_registryObject != null)
            Object.DestroyImmediate(_registryObject);

        SetStaticProperty(typeof(CouplesManager), "Instance", null);
        SetStaticProperty(typeof(PassengerRegistry), "Instance", null);
    }

    [Test]
    public void ActiveCouplesCount_CleansDestroyedCouples()
    {
        Couple couple = CreateMockCouple();
        _manager.RegisterCouple(couple);

        Object.DestroyImmediate(couple.gameObject);

        Assert.AreEqual(0, _manager.ActiveCouplesCount);
    }

    [Test]
    public void RegisterCouple_ReachingThreshold_DespawnsTrackedCouples()
    {
        Couple couple = CreateMockCouple();
        SetPrivateField(_manager, "_stationThreshold", 1);

        _manager.RegisterCouple(couple);

        Assert.AreEqual(0, _manager.ActiveCouplesCount);
    }

    [Test]
    public void Update_NoPairsPossible_DespawnsActiveCouples()
    {
        Couple couple = CreateMockCouple();
        _manager.RegisterCouple(couple);
        SetPrivateField(_manager, "_stationThreshold", 99);
        SetPrivateField(_manager, "_stopWhenNoPairs", true);
        SetPrivateField(_manager, "_minPairsBeforeStop", 1);
        SetPrivateField(_manager, "_checkInterval", 0f);
        SetPrivateField(_manager, "_cooldownAfterStop", 0f);
        SetPrivateField(_manager, "_nextCheckTime", -1f);

        InvokePrivateMethod(_manager, "Update");

        Assert.AreEqual(0, _manager.ActiveCouplesCount);
    }

    private static Couple CreateMockCouple()
    {
        var go = new GameObject("MockCouple");
        return go.AddComponent<Couple>();
    }

    private static void InvokePrivateMethod(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(instance, null);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(instance, value);
    }

    private static void SetStaticProperty(System.Type type, string propertyName, object value)
    {
        var property = type.GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        property?.SetValue(null, value);
    }
}
