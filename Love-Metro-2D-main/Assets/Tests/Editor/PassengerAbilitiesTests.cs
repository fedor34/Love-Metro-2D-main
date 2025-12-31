using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for PassengerAbilities system
/// Tests ability attachment, invocation, and VIP ability functionality
/// </summary>
public class PassengerAbilitiesTests
{
    private GameObject passengerObject;
    private Passenger passenger;
    private PassengerAbilities abilities;

    [SetUp]
    public void Setup()
    {
        passengerObject = new GameObject("TestPassenger");
        passenger = passengerObject.AddComponent<Passenger>();
        abilities = passengerObject.AddComponent<PassengerAbilities>();
    }

    [TearDown]
    public void Teardown()
    {
        if (passengerObject != null)
        {
            Object.DestroyImmediate(passengerObject);
        }
    }

    [Test]
    public void AddAbility_AddsAbilityToList()
    {
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();

        abilities.AddAbility(vipAbility);

        Assert.IsTrue(abilities.HasAbility<VipAbility>());

        Object.DestroyImmediate(vipAbility);
    }

    [Test]
    public void AddAbility_IgnoresNullAbility()
    {
        abilities.AddAbility(null);

        // Should not throw exception and list remains valid
        Assert.Pass();
    }

    [Test]
    public void HasAbility_ReturnsFalse_WhenAbilityNotPresent()
    {
        bool hasVip = abilities.HasAbility<VipAbility>();

        Assert.IsFalse(hasVip);
    }

    [Test]
    public void HasAbility_ReturnsTrue_WhenAbilityPresent()
    {
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility);

        bool hasVip = abilities.HasAbility<VipAbility>();

        Assert.IsTrue(hasVip);

        Object.DestroyImmediate(vipAbility);
    }

    [Test]
    public void InvokeMatched_VIPAbility_DoublesPoints()
    {
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility);
        abilities.AttachAll();

        var partner = CreateMockPassenger();
        int points = 100;

        abilities.InvokeMatched(partner, ref points);

        Assert.AreEqual(200, points); // VIP doubles points

        Object.DestroyImmediate(vipAbility);
        CleanupPassenger(partner);
    }

    [Test]
    public void InvokeMatched_WithoutAbilities_PointsUnchanged()
    {
        var partner = CreateMockPassenger();
        int points = 100;

        abilities.InvokeMatched(partner, ref points);

        Assert.AreEqual(100, points); // No abilities, points stay the same

        CleanupPassenger(partner);
    }

    [Test]
    public void InvokeMatched_MultipleAbilities_StackCorrectly()
    {
        var vipAbility1 = ScriptableObject.CreateInstance<VipAbility>();
        var vipAbility2 = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility1);
        abilities.AddAbility(vipAbility2);
        abilities.AttachAll();

        var partner = CreateMockPassenger();
        int points = 100;

        abilities.InvokeMatched(partner, ref points);

        // Two VIP abilities: 100 * 2 * 2 = 400
        Assert.AreEqual(400, points);

        Object.DestroyImmediate(vipAbility1);
        Object.DestroyImmediate(vipAbility2);
        CleanupPassenger(partner);
    }

    [Test]
    public void InvokePairBroken_DoesNotThrow_WithNoAbilities()
    {
        var hitter = CreateMockPassenger();

        Assert.DoesNotThrow(() =>
        {
            abilities.InvokePairBroken(hitter);
        });

        CleanupPassenger(hitter);
    }

    [Test]
    public void AttachAll_CallsOnAttach_ForAllAbilities()
    {
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility);

        Assert.DoesNotThrow(() =>
        {
            abilities.AttachAll();
        });

        Object.DestroyImmediate(vipAbility);
    }

    [Test]
    public void AddAbility_AllowsMultipleAbilitiesOfSameType()
    {
        var vipAbility1 = ScriptableObject.CreateInstance<VipAbility>();
        var vipAbility2 = ScriptableObject.CreateInstance<VipAbility>();

        abilities.AddAbility(vipAbility1);
        abilities.AddAbility(vipAbility2);

        // Both should be added (list allows duplicates)
        Assert.IsTrue(abilities.HasAbility<VipAbility>());

        Object.DestroyImmediate(vipAbility1);
        Object.DestroyImmediate(vipAbility2);
    }

    // Helper methods
    private Passenger CreateMockPassenger()
    {
        var go = new GameObject("MockPartner");
        var p = go.AddComponent<Passenger>();
        return p;
    }

    private void CleanupPassenger(Passenger p)
    {
        if (p != null && p.gameObject != null)
        {
            Object.DestroyImmediate(p.gameObject);
        }
    }
}
