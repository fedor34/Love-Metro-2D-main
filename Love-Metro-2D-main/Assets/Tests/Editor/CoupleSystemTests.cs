using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Integration tests for couple creation and breakup behavior.
/// </summary>
public class CoupleSystemTests
{
    private GameObject _coupleManagerObject;
    private CouplesManager _couplesManager;
    private GameObject _registryObject;
    private PassengerRegistry _registry;

    [SetUp]
    public void Setup()
    {
        SetStaticProperty(typeof(CouplesManager), "Instance", null);
        SetStaticProperty(typeof(PassengerRegistry), "Instance", null);

        _coupleManagerObject = new GameObject("TestCouplesManager");
        _couplesManager = _coupleManagerObject.AddComponent<CouplesManager>();
        InvokePrivateMethod(_couplesManager, "Awake");

        _registryObject = new GameObject("TestPassengerRegistry");
        _registry = _registryObject.AddComponent<PassengerRegistry>();
        InvokePrivateMethod(_registry, "Awake");
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var couple in Object.FindObjectsOfType<Couple>())
            Object.DestroyImmediate(couple.gameObject);

        foreach (var passenger in Object.FindObjectsOfType<Passenger>())
            Object.DestroyImmediate(passenger.gameObject);

        if (_coupleManagerObject != null)
            Object.DestroyImmediate(_coupleManagerObject);

        if (_registryObject != null)
            Object.DestroyImmediate(_registryObject);

        SetStaticProperty(typeof(CouplesManager), "Instance", null);
        SetStaticProperty(typeof(PassengerRegistry), "Instance", null);
    }

    [Test]
    public void Init_SetsPassengersInCouple_AndRemovesThemFromSingles()
    {
        Passenger male = CreateMockPassenger(false, Vector3.zero);
        Passenger female = CreateMockPassenger(true, new Vector3(2f, 0f, 0f));
        Couple couple = CreateMockCouple(1.25f);

        _registry.Register(male);
        _registry.Register(female);

        couple.Init(male, female);

        Assert.IsTrue(male.IsInCouple);
        Assert.IsTrue(female.IsInCouple);
        Assert.AreSame(couple.transform, male.transform.parent);
        Assert.AreSame(couple.transform, female.transform.parent);
        Assert.AreEqual(0, _registry.Singles.Count);
        Assert.AreEqual(1, _couplesManager.ActiveCouplesCount);
        Assert.AreEqual(new Vector3(1.25f, 0f, 0f), female.transform.position);
    }

    [Test]
    public void BreakByHit_ReturnsPassengersToSinglesRegistry_WhenImpactIsStrongEnough()
    {
        Passenger male = CreateMockPassenger(false, Vector3.zero);
        Passenger female = CreateMockPassenger(true, new Vector3(1f, 0f, 0f));
        Passenger hitter = CreateMockPassenger(false, new Vector3(5f, 0f, 0f));
        Couple couple = CreateMockCouple(1f);

        _registry.Register(male);
        _registry.Register(female);
        couple.Init(male, female);

        hitter.GetComponent<Rigidbody2D>().velocity = new Vector2(8f, 0f);
        couple.BreakByHit(hitter);

        Assert.IsFalse(male.IsInCouple);
        Assert.IsFalse(female.IsInCouple);
        Assert.IsNull(male.transform.parent);
        Assert.IsNull(female.transform.parent);
        Assert.AreEqual(2, _registry.Singles.Count);
        Assert.AreEqual(1, _registry.MaleSinglesCount);
        Assert.AreEqual(1, _registry.FemaleSinglesCount);
        Assert.AreEqual(0, _couplesManager.ActiveCouplesCount);
    }

    [Test]
    public void BreakByHit_DoesNothing_WhenImpactIsBelowThreshold()
    {
        Passenger male = CreateMockPassenger(false, Vector3.zero);
        Passenger female = CreateMockPassenger(true, new Vector3(1f, 0f, 0f));
        Passenger hitter = CreateMockPassenger(false, new Vector3(5f, 0f, 0f));
        Couple couple = CreateMockCouple(1f);

        _registry.Register(male);
        _registry.Register(female);
        couple.Init(male, female);

        hitter.GetComponent<Rigidbody2D>().velocity = new Vector2(1f, 0f);
        couple.BreakByHit(hitter);

        Assert.IsTrue(male.IsInCouple);
        Assert.IsTrue(female.IsInCouple);
        Assert.AreSame(couple.transform, male.transform.parent);
        Assert.AreSame(couple.transform, female.transform.parent);
        Assert.AreEqual(0, _registry.Singles.Count);
        Assert.AreEqual(1, _couplesManager.ActiveCouplesCount);
    }

    private static Couple CreateMockCouple(float socialDistance)
    {
        var go = new GameObject("MockCouple");
        var couple = go.AddComponent<Couple>();
        SetPrivateField(couple, "_socialDistance", socialDistance);
        return couple;
    }

    private static Passenger CreateMockPassenger(bool isFemale, Vector3 position)
    {
        var go = new GameObject("MockPassenger_" + (isFemale ? "F" : "M"));
        go.transform.position = position;
        go.AddComponent<BoxCollider2D>();
        var passenger = go.AddComponent<Passenger>();

        passenger.IsFemale = isFemale;
        return passenger;
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
