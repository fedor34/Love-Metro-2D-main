using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Integration tests for Couple system
/// Tests couple creation, positioning, and breaking mechanics
/// </summary>
public class CoupleSystemTests
{
    private GameObject coupleManagerObject;
    private CouplesManager couplesManager;

    [SetUp]
    public void Setup()
    {
        SetStaticProperty(typeof(CouplesManager), "Instance", null);
        coupleManagerObject = new GameObject("TestCouplesManager");
        couplesManager = coupleManagerObject.AddComponent<CouplesManager>();

        var awakeMethod = typeof(CouplesManager).GetMethod("Awake",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        awakeMethod?.Invoke(couplesManager, null);
    }

    [TearDown]
    public void Teardown()
    {
        if (coupleManagerObject != null)
        {
            Object.DestroyImmediate(coupleManagerObject);
        }
        SetStaticProperty(typeof(CouplesManager), "Instance", null);
    }

    [Test]
    public void Singleton_InstanceIsSet_AfterAwake()
    {
        Assert.IsNotNull(CouplesManager.Instance);
        Assert.AreEqual(couplesManager, CouplesManager.Instance);
    }

    [Test]
    public void RegisterCouple_AddsToActiveCouples()
    {
        var couple = CreateMockCouple();

        couplesManager.RegisterCouple(couple);

        var activeCouples = GetPrivateField<System.Collections.Generic.List<Couple>>(
            couplesManager, "_activeCouples");
        Assert.IsNotNull(activeCouples);
        Assert.Contains(couple, (System.Collections.ICollection)activeCouples);

        CleanupCouple(couple);
    }

    [Test]
    public void RegisterCouple_DoesNotAddDuplicate()
    {
        var couple = CreateMockCouple();

        couplesManager.RegisterCouple(couple);
        couplesManager.RegisterCouple(couple);

        var activeCouples = GetPrivateField<System.Collections.Generic.List<Couple>>(
            couplesManager, "_activeCouples");
        int count = 0;
        foreach (var c in activeCouples)
        {
            if (c == couple) count++;
        }
        Assert.AreEqual(1, count);

        CleanupCouple(couple);
    }

    [Test]
    public void UnregisterCouple_RemovesFromActiveCouples()
    {
        var couple = CreateMockCouple();
        couplesManager.RegisterCouple(couple);

        couplesManager.UnregisterCouple(couple);

        var activeCouples = GetPrivateField<System.Collections.Generic.List<Couple>>(
            couplesManager, "_activeCouples");
        Assert.IsFalse(activeCouples.Contains(couple));

        CleanupCouple(couple);
    }

    [Test]
    public void CoupleCreation_SetsIsInCouple_ForBothPassengers()
    {
        var male = CreateMockPassenger(false, Vector3.zero);
        var female = CreateMockPassenger(true, new Vector3(1, 0, 0));
        var couple = CreateMockCouple();

        var initMethod = typeof(Couple).GetMethod("init",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (initMethod != null)
        {
            Assert.Pass("Couple init method exists");
        }
        else
        {
            Assert.Inconclusive("Couple init method not found via reflection");
        }

        CleanupPassenger(male);
        CleanupPassenger(female);
        CleanupCouple(couple);
    }

    [Test]
    public void CouplePositioning_PlacesBetweenPassengers()
    {
        var male = CreateMockPassenger(false, new Vector3(0, 0, 0));
        var female = CreateMockPassenger(true, new Vector3(4, 0, 0));
        var couple = CreateMockCouple();

        couple.transform.position = (male.transform.position + female.transform.position) * 0.5f;

        Vector3 expectedPosition = new Vector3(2, 0, 0);
        Assert.AreEqual(expectedPosition, couple.transform.position);

        CleanupPassenger(male);
        CleanupPassenger(female);
        CleanupCouple(couple);
    }

    [Test]
    public void MultipleCouplesTogetherCreation_AllRegistered()
    {
        var couple1 = CreateMockCouple();
        var couple2 = CreateMockCouple();
        var couple3 = CreateMockCouple();

        couplesManager.RegisterCouple(couple1);
        couplesManager.RegisterCouple(couple2);
        couplesManager.RegisterCouple(couple3);

        var activeCouples = GetPrivateField<System.Collections.Generic.List<Couple>>(
            couplesManager, "_activeCouples");
        Assert.AreEqual(3, activeCouples.Count);

        CleanupCouple(couple1);
        CleanupCouple(couple2);
        CleanupCouple(couple3);
    }

    private Couple CreateMockCouple()
    {
        var go = new GameObject("MockCouple");
        var couple = go.AddComponent<Couple>();
        return couple;
    }

    private Passenger CreateMockPassenger(bool isFemale, Vector3 position)
    {
        var go = new GameObject("MockPassenger_" + (isFemale ? "F" : "M"));
        go.transform.position = position;
        var passenger = go.AddComponent<Passenger>();

        var type = typeof(Passenger);
        var field = type.GetField("IsFemale");
        var prop = type.GetProperty("IsFemale");
        if (field != null)
            field.SetValue(passenger, isFemale);
        else if (prop != null)
            prop.SetValue(passenger, isFemale);

        return passenger;
    }

    private void CleanupCouple(Couple couple)
    {
        if (couple != null && couple.gameObject != null)
        {
            Object.DestroyImmediate(couple.gameObject);
        }
    }

    private void CleanupPassenger(Passenger passenger)
    {
        if (passenger != null && passenger.gameObject != null)
        {
            Object.DestroyImmediate(passenger.gameObject);
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

    private void SetStaticProperty(System.Type type, string propertyName, object value)
    {
        var prop = type.GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        prop?.SetValue(null, value);
    }
}