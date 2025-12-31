using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// Unit tests for PassengerRegistry class
/// Tests singleton pattern, registration/unregistration, and lookup methods
/// </summary>
public class PassengerRegistryTests
{
    private GameObject registryObject;
    private PassengerRegistry registry;

    [SetUp]
    public void Setup()
    {
        // Create a fresh PassengerRegistry for each test
        registryObject = new GameObject("TestRegistry");
        registry = registryObject.AddComponent<PassengerRegistry>();
        // Awake is called automatically by Unity when component is added
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up after each test
        if (registryObject != null)
        {
            Object.DestroyImmediate(registryObject);
        }
    }

    [Test]
    public void Singleton_InstanceIsSet_AfterAwake()
    {
        Assert.IsNotNull(PassengerRegistry.Instance);
        Assert.AreEqual(registry, PassengerRegistry.Instance);
    }

    [Test]
    public void Register_AddsPassengerToAllList()
    {
        var passenger = CreateMockPassenger(true); // female

        registry.Register(passenger);

        Assert.AreEqual(1, registry.AllPassengers.Count);
        Assert.Contains(passenger, (System.Collections.ICollection)registry.AllPassengers);

        CleanupPassenger(passenger);
    }

    [Test]
    public void Register_AddsFemaleToFemalesList()
    {
        var femalePassenger = CreateMockPassenger(true);

        registry.Register(femalePassenger);

        Assert.AreEqual(1, registry.Females.Count);
        Assert.AreEqual(0, registry.Males.Count);
        Assert.Contains(femalePassenger, (System.Collections.ICollection)registry.Females);

        CleanupPassenger(femalePassenger);
    }

    [Test]
    public void Register_AddsMaleToMalesList()
    {
        var malePassenger = CreateMockPassenger(false);

        registry.Register(malePassenger);

        Assert.AreEqual(1, registry.Males.Count);
        Assert.AreEqual(0, registry.Females.Count);
        Assert.Contains(malePassenger, (System.Collections.ICollection)registry.Males);

        CleanupPassenger(malePassenger);
    }

    [Test]
    public void Register_AddsSinglePassengerToSinglesList()
    {
        var passenger = CreateMockPassenger(true, isInCouple: false);

        registry.Register(passenger);

        Assert.AreEqual(1, registry.Singles.Count);
        Assert.Contains(passenger, (System.Collections.ICollection)registry.Singles);

        CleanupPassenger(passenger);
    }

    [Test]
    public void Register_DoesNotAddCoupledPassengerToSinglesList()
    {
        var passenger = CreateMockPassenger(true, isInCouple: true);

        registry.Register(passenger);

        Assert.AreEqual(0, registry.Singles.Count);
        Assert.AreEqual(1, registry.AllPassengers.Count);

        CleanupPassenger(passenger);
    }

    [Test]
    public void Register_UpdatesSinglesCounts()
    {
        var male = CreateMockPassenger(false, isInCouple: false);
        var female = CreateMockPassenger(true, isInCouple: false);

        registry.Register(male);
        registry.Register(female);

        Assert.AreEqual(1, registry.MaleSinglesCount);
        Assert.AreEqual(1, registry.FemaleSinglesCount);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void Register_IgnoresNullPassenger()
    {
        registry.Register(null);

        Assert.AreEqual(0, registry.AllPassengers.Count);
    }

    [Test]
    public void Register_IgnoresDuplicateRegistration()
    {
        var passenger = CreateMockPassenger(true);

        registry.Register(passenger);
        registry.Register(passenger); // Try to register again

        Assert.AreEqual(1, registry.AllPassengers.Count);

        CleanupPassenger(passenger);
    }

    [Test]
    public void Unregister_RemovesPassengerFromAllLists()
    {
        var passenger = CreateMockPassenger(true);
        registry.Register(passenger);

        registry.Unregister(passenger);

        Assert.AreEqual(0, registry.AllPassengers.Count);
        Assert.AreEqual(0, registry.Females.Count);
        Assert.AreEqual(0, registry.Singles.Count);

        CleanupPassenger(passenger);
    }

    [Test]
    public void Unregister_UpdatesSinglesCounts()
    {
        var male = CreateMockPassenger(false);
        var female = CreateMockPassenger(true);
        registry.Register(male);
        registry.Register(female);

        registry.Unregister(male);

        Assert.AreEqual(0, registry.MaleSinglesCount);
        Assert.AreEqual(1, registry.FemaleSinglesCount);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void UpdateCoupleStatus_RemovesFromSinglesWhenCoupled()
    {
        var passenger = CreateMockPassenger(true, isInCouple: false);
        registry.Register(passenger);

        passenger.IsInCouple = true;
        registry.UpdateCoupleStatus(passenger);

        Assert.AreEqual(0, registry.Singles.Count);
        Assert.AreEqual(0, registry.FemaleSinglesCount);

        CleanupPassenger(passenger);
    }

    [Test]
    public void UpdateCoupleStatus_AddsToSinglesWhenUncoupled()
    {
        var passenger = CreateMockPassenger(true, isInCouple: true);
        registry.Register(passenger);

        passenger.IsInCouple = false;
        registry.UpdateCoupleStatus(passenger);

        Assert.AreEqual(1, registry.Singles.Count);
        Assert.AreEqual(1, registry.FemaleSinglesCount);

        CleanupPassenger(passenger);
    }

    [Test]
    public void FindClosestOpposite_FindsFemaleForMale()
    {
        var male = CreateMockPassenger(false);
        var female = CreateMockPassenger(true);
        male.transform.position = Vector3.zero;
        female.transform.position = new Vector3(2, 0, 0);

        registry.Register(male);
        registry.Register(female);

        var result = registry.FindClosestOpposite(male, 5f);

        Assert.AreEqual(female, result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void FindClosestOpposite_ReturnsNullWhenNoOppositeInRange()
    {
        var male = CreateMockPassenger(false);
        var female = CreateMockPassenger(true);
        male.transform.position = Vector3.zero;
        female.transform.position = new Vector3(100, 0, 0); // Far away

        registry.Register(male);
        registry.Register(female);

        var result = registry.FindClosestOpposite(male, 5f);

        Assert.IsNull(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void FindClosestOpposite_IgnoresCoupledPassengers()
    {
        var male = CreateMockPassenger(false);
        var female = CreateMockPassenger(true, isInCouple: true);
        male.transform.position = Vector3.zero;
        female.transform.position = new Vector3(2, 0, 0);

        registry.Register(male);
        registry.Register(female);

        var result = registry.FindClosestOpposite(male, 5f);

        Assert.IsNull(result);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void FindClosestOpposite_FindsClosestWhenMultipleOptions()
    {
        var male = CreateMockPassenger(false);
        var female1 = CreateMockPassenger(true);
        var female2 = CreateMockPassenger(true);

        male.transform.position = Vector3.zero;
        female1.transform.position = new Vector3(5, 0, 0);
        female2.transform.position = new Vector3(2, 0, 0); // Closer

        registry.Register(male);
        registry.Register(female1);
        registry.Register(female2);

        var result = registry.FindClosestOpposite(male, 10f);

        Assert.AreEqual(female2, result);

        CleanupPassenger(male);
        CleanupPassenger(female1);
        CleanupPassenger(female2);
    }

    [Test]
    public void GetSameGenderInRadius_FindsMalesForMale()
    {
        var male1 = CreateMockPassenger(false);
        var male2 = CreateMockPassenger(false);
        var female = CreateMockPassenger(true);

        male1.transform.position = Vector3.zero;
        male2.transform.position = new Vector3(2, 0, 0);
        female.transform.position = new Vector3(1, 0, 0);

        registry.Register(male1);
        registry.Register(male2);
        registry.Register(female);

        var results = new System.Collections.Generic.List<Passenger>();
        registry.GetSameGenderInRadius(male1, 5f, results);

        Assert.AreEqual(1, results.Count);
        Assert.Contains(male2, results);
        Assert.IsFalse(results.Contains(female));

        CleanupPassenger(male1);
        CleanupPassenger(male2);
        CleanupPassenger(female);
    }

    [Test]
    public void GetSameGenderInRadius_DoesNotIncludeSelf()
    {
        var male1 = CreateMockPassenger(false);
        var male2 = CreateMockPassenger(false);

        male1.transform.position = Vector3.zero;
        male2.transform.position = new Vector3(2, 0, 0);

        registry.Register(male1);
        registry.Register(male2);

        var results = new System.Collections.Generic.List<Passenger>();
        registry.GetSameGenderInRadius(male1, 5f, results);

        Assert.IsFalse(results.Contains(male1));

        CleanupPassenger(male1);
        CleanupPassenger(male2);
    }

    [Test]
    public void GetPossiblePairsCount_ReturnsMinimumOfMalesAndFemales()
    {
        var male1 = CreateMockPassenger(false);
        var male2 = CreateMockPassenger(false);
        var female = CreateMockPassenger(true);

        registry.Register(male1);
        registry.Register(male2);
        registry.Register(female);

        int count = registry.GetPossiblePairsCount();

        Assert.AreEqual(1, count); // Min(2 males, 1 female) = 1

        CleanupPassenger(male1);
        CleanupPassenger(male2);
        CleanupPassenger(female);
    }

    [Test]
    public void GetPossiblePairsCount_ReturnsZeroWhenNoOppositeGender()
    {
        var male1 = CreateMockPassenger(false);
        var male2 = CreateMockPassenger(false);

        registry.Register(male1);
        registry.Register(male2);

        int count = registry.GetPossiblePairsCount();

        Assert.AreEqual(0, count);

        CleanupPassenger(male1);
        CleanupPassenger(male2);
    }

    [Test]
    public void ClearAll_RemovesAllPassengers()
    {
        var male = CreateMockPassenger(false);
        var female = CreateMockPassenger(true);
        registry.Register(male);
        registry.Register(female);

        registry.ClearAll();

        Assert.AreEqual(0, registry.AllPassengers.Count);
        Assert.AreEqual(0, registry.Males.Count);
        Assert.AreEqual(0, registry.Females.Count);
        Assert.AreEqual(0, registry.Singles.Count);
        Assert.AreEqual(0, registry.MaleSinglesCount);
        Assert.AreEqual(0, registry.FemaleSinglesCount);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    // Helper methods
    private Passenger CreateMockPassenger(bool isFemale, bool isInCouple = false)
    {
        var go = new GameObject($"MockPassenger_{(isFemale ? "F" : "M")}");

        // Add required components before Passenger
        // PassangerAnimator requires Animator and SpriteRenderer
        go.AddComponent<Animator>();
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<PassangerAnimator>();

        var passenger = go.AddComponent<Passenger>();

        // IsFemale is a public field, so assign directly
        passenger.IsFemale = isFemale;
        passenger.IsInCouple = isInCouple;
        passenger.IsMatchable = true;

        return passenger;
    }

    private void CleanupPassenger(Passenger passenger)
    {
        if (passenger != null && passenger.gameObject != null)
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }
}
