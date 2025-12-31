using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Integration tests for core gameplay mechanics
/// Tests interactions between multiple systems
/// </summary>
public class GameplayIntegrationTests
{
    private GameObject registryObject;
    private PassengerRegistry registry;
    private GameObject couplesManagerObject;
    private CouplesManager couplesManager;

    [SetUp]
    public void Setup()
    {
        // Setup PassengerRegistry
        registryObject = new GameObject("TestRegistry");
        registry = registryObject.AddComponent<PassengerRegistry>();
        // Awake is called automatically by Unity when component is added

        // Setup CouplesManager
        couplesManagerObject = new GameObject("TestCouplesManager");
        couplesManager = couplesManagerObject.AddComponent<CouplesManager>();
        // Awake is called automatically by Unity when component is added
    }

    [TearDown]
    public void Teardown()
    {
        if (registryObject != null)
            Object.DestroyImmediate(registryObject);
        if (couplesManagerObject != null)
            Object.DestroyImmediate(couplesManagerObject);
    }

    [Test]
    public void PairingScenario_OneMan_OneFemale_CanPair()
    {
        var male = CreatePassenger(false, Vector3.zero);
        var female = CreatePassenger(true, new Vector3(2, 0, 0));

        registry.Register(male);
        registry.Register(female);

        int possiblePairs = registry.GetPossiblePairsCount();

        Assert.AreEqual(1, possiblePairs);

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void PairingScenario_TwoMen_OneFemale_OnlyOnePair()
    {
        var male1 = CreatePassenger(false, Vector3.zero);
        var male2 = CreatePassenger(false, new Vector3(1, 0, 0));
        var female = CreatePassenger(true, new Vector3(2, 0, 0));

        registry.Register(male1);
        registry.Register(male2);
        registry.Register(female);

        int possiblePairs = registry.GetPossiblePairsCount();

        Assert.AreEqual(1, possiblePairs); // Min(2 males, 1 female) = 1

        CleanupPassenger(male1);
        CleanupPassenger(male2);
        CleanupPassenger(female);
    }

    [Test]
    public void PairingScenario_OnlyMales_NoPairs()
    {
        var male1 = CreatePassenger(false, Vector3.zero);
        var male2 = CreatePassenger(false, new Vector3(1, 0, 0));

        registry.Register(male1);
        registry.Register(male2);

        int possiblePairs = registry.GetPossiblePairsCount();

        Assert.AreEqual(0, possiblePairs);

        CleanupPassenger(male1);
        CleanupPassenger(male2);
    }

    [Test]
    public void PairingScenario_CoupledPassengers_RemovedFromSingles()
    {
        var male = CreatePassenger(false, Vector3.zero, isInCouple: false);
        var female = CreatePassenger(true, new Vector3(1, 0, 0), isInCouple: false);

        registry.Register(male);
        registry.Register(female);

        Assert.AreEqual(2, registry.Singles.Count);

        // Simulate pairing
        male.IsInCouple = true;
        female.IsInCouple = true;
        registry.UpdateCoupleStatus(male);
        registry.UpdateCoupleStatus(female);

        Assert.AreEqual(0, registry.Singles.Count);
        Assert.AreEqual(0, registry.GetPossiblePairsCount());

        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void MagnetSystem_FindsClosestOpposite_WhenMultipleOptions()
    {
        var male = CreatePassenger(false, Vector3.zero);
        var farFemale = CreatePassenger(true, new Vector3(10, 0, 0));
        var nearFemale = CreatePassenger(true, new Vector3(2, 0, 0));

        registry.Register(male);
        registry.Register(farFemale);
        registry.Register(nearFemale);

        var closest = registry.FindClosestOpposite(male, 15f);

        Assert.AreEqual(nearFemale, closest);

        CleanupPassenger(male);
        CleanupPassenger(farFemale);
        CleanupPassenger(nearFemale);
    }

    [Test]
    public void RepelSystem_FindsSameGender_InRadius()
    {
        var male1 = CreatePassenger(false, Vector3.zero);
        var male2 = CreatePassenger(false, new Vector3(1.5f, 0, 0));
        var farMale = CreatePassenger(false, new Vector3(10, 0, 0));

        registry.Register(male1);
        registry.Register(male2);
        registry.Register(farMale);

        var results = new System.Collections.Generic.List<Passenger>();
        registry.GetSameGenderInRadius(male1, 2.0f, results);

        Assert.AreEqual(1, results.Count);
        Assert.Contains(male2, results);
        Assert.IsFalse(results.Contains(farMale));

        CleanupPassenger(male1);
        CleanupPassenger(male2);
        CleanupPassenger(farMale);
    }

    [Test]
    public void RegistryCleanup_RemovesNullReferences()
    {
        var male = CreatePassenger(false, Vector3.zero);
        var female = CreatePassenger(true, new Vector3(1, 0, 0));

        registry.Register(male);
        registry.Register(female);

        Assert.AreEqual(2, registry.AllPassengers.Count);

        // Destroy one passenger (simulating game destruction)
        Object.DestroyImmediate(male.gameObject);

        // Cleanup should remove null reference
        registry.CleanupNullReferences();

        Assert.AreEqual(1, registry.AllPassengers.Count);

        CleanupPassenger(female);
    }

    [Test]
    public void VIPAbility_Integration_DoublesScore()
    {
        var scoreCounterObject = new GameObject("ScoreCounter");
        var scoreCounter = scoreCounterObject.AddComponent<ScoreCounter>();
        SetPrivateField(scoreCounter, "_initialScorePointsPerCouple", 100);

        var male = CreatePassenger(false, Vector3.zero);
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        var abilities = male.gameObject.AddComponent<PassengerAbilities>();
        abilities.AddAbility(vipAbility);
        abilities.AttachAll();

        var female = CreatePassenger(true, new Vector3(1, 0, 0));

        int points = scoreCounter.GetBasePointsPerCouple();
        abilities.InvokeMatched(female, ref points);

        Assert.AreEqual(200, points); // VIP doubles to 200

        Object.DestroyImmediate(scoreCounterObject);
        Object.DestroyImmediate(vipAbility);
        CleanupPassenger(male);
        CleanupPassenger(female);
    }

    [Test]
    public void MultiplePassengers_RegisteredCorrectly()
    {
        var passengers = new System.Collections.Generic.List<Passenger>();

        // Create 5 males and 5 females
        for (int i = 0; i < 5; i++)
        {
            passengers.Add(CreatePassenger(false, new Vector3(i, 0, 0)));
            passengers.Add(CreatePassenger(true, new Vector3(i, 1, 0)));
        }

        foreach (var p in passengers)
        {
            registry.Register(p);
        }

        Assert.AreEqual(10, registry.AllPassengers.Count);
        Assert.AreEqual(5, registry.Males.Count);
        Assert.AreEqual(5, registry.Females.Count);
        Assert.AreEqual(10, registry.Singles.Count);
        Assert.AreEqual(5, registry.MaleSinglesCount);
        Assert.AreEqual(5, registry.FemaleSinglesCount);

        foreach (var p in passengers)
        {
            CleanupPassenger(p);
        }
    }

    // Helper methods
    private Passenger CreatePassenger(bool isFemale, Vector3 position, bool isInCouple = false)
    {
        var go = new GameObject($"Passenger_{(isFemale ? "F" : "M")}");
        go.transform.position = position;

        // Add required components before Passenger (Passenger has [RequireComponent])
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<BoxCollider2D>();
        var passangerAnimator = go.AddComponent<PassangerAnimator>();

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

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
}
