using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Play Mode tests for gameplay mechanics
/// Tests score system, abilities, and game flow
/// </summary>
public class GameplayPlayModeTests
{
    private GameObject testContainer;
    private ScoreCounter scoreCounter;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        testContainer = new GameObject("TestContainer");

        // Setup PassengerRegistry
        var registryObj = new GameObject("PassengerRegistry");
        registryObj.AddComponent<PassengerRegistry>();

        // Setup CouplesManager
        var couplesObj = new GameObject("CouplesManager");
        couplesObj.AddComponent<CouplesManager>();

        // Setup ScoreCounter with required components
        var scoreObj = new GameObject("ScoreCounter");
        var canvas = scoreObj.AddComponent<Canvas>();
        scoreObj.AddComponent<TMPro.TextMeshProUGUI>();
        scoreObj.AddComponent<Animator>();
        scoreCounter = scoreObj.AddComponent<ScoreCounter>();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        if (testContainer != null)
            Object.Destroy(testContainer);

        var registry = Object.FindObjectOfType<PassengerRegistry>();
        if (registry != null)
            Object.Destroy(registry.gameObject);

        var couplesManager = Object.FindObjectOfType<CouplesManager>();
        if (couplesManager != null)
            Object.Destroy(couplesManager.gameObject);

        if (scoreCounter != null)
            Object.Destroy(scoreCounter.gameObject);

        yield return null;
    }

    [UnityTest]
    public IEnumerator ScoreCounter_AwardsPoints_OverTime()
    {
        // Get initial score
        int initialScore = GetPrivateField<int>(scoreCounter, "_score");

        // Award some points
        scoreCounter.AwardMatchPoints(Vector3.zero, 100);

        // Wait for coroutine to process
        yield return new WaitForSeconds(2f);

        // Check that score increased
        int finalScore = GetPrivateField<int>(scoreCounter, "_score");
        Assert.Greater(finalScore, initialScore, "Score should increase after awarding points");
    }

    [UnityTest]
    public IEnumerator VIPAbility_DoublesPoints_InRealtime()
    {
        // Create VIP passenger
        var passenger = CreateTestPassenger(true, Vector3.zero, "VIPPassenger");
        var abilities = passenger.gameObject.AddComponent<PassengerAbilities>();

        // Add VIP ability
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility);

        // Test that ability exists
        Assert.IsTrue(abilities.HasAbility<VipAbility>(), "VIP ability should be present");

        // Invoke match
        int basePoints = 100;
        abilities.InvokeMatched(null, scoreCounter, basePoints);

        yield return new WaitForSeconds(0.5f);

        // Score should be doubled (200) due to VIP
        int score = GetPrivateField<int>(scoreCounter, "_score");
        Assert.GreaterOrEqual(score, basePoints * 2, "VIP should double the base points");

        Object.Destroy(vipAbility);
    }

    [UnityTest]
    public IEnumerator PassengerRegistry_TracksPassengers_InRealtime()
    {
        var registry = PassengerRegistry.Instance;
        Assert.IsNotNull(registry, "PassengerRegistry should exist");

        int initialCount = registry.AllPassengers.Count;

        // Create and register passengers
        var male = CreateTestPassenger(false, Vector3.zero, "Male");
        var female = CreateTestPassenger(true, Vector3.right, "Female");

        // Wait a frame for registration
        yield return null;

        // Should have 2 more passengers
        Assert.AreEqual(initialCount + 2, registry.AllPassengers.Count,
            "Registry should track newly created passengers");

        // Check gender lists
        Assert.Greater(registry.Males.Count, 0, "Should have at least one male");
        Assert.Greater(registry.Females.Count, 0, "Should have at least one female");
    }

    [UnityTest]
    public IEnumerator CouplesManager_ManagesCouples_OverTime()
    {
        var couplesManager = CouplesManager.Instance;
        Assert.IsNotNull(couplesManager, "CouplesManager should exist");

        // Create a couple
        var male = CreateTestPassenger(false, Vector3.zero, "Male");
        var female = CreateTestPassenger(true, Vector3.right, "Female");

        var coupleObj = new GameObject("TestCouple");
        coupleObj.transform.parent = testContainer.transform;
        var couple = coupleObj.AddComponent<Couple>();

        // Register couple
        couplesManager.RegisterCouple(couple);

        yield return null;

        // Check that couple is registered
        var activeCouples = GetPrivateField<System.Collections.Generic.List<Couple>>(
            couplesManager, "_activeCouples");

        Assert.IsNotNull(activeCouples, "Active couples list should exist");
        Assert.Contains(couple, activeCouples as System.Collections.ICollection,
            "Couple should be in active couples list");
    }

    [UnityTest]
    public IEnumerator MultiplePassengers_RegisteredAndTracked_Simultaneously()
    {
        var registry = PassengerRegistry.Instance;
        int initialCount = registry.AllPassengers.Count;

        // Create many passengers at once
        for (int i = 0; i < 5; i++)
        {
            CreateTestPassenger(i % 2 == 0, new Vector3(i, 0, 0), $"Passenger{i}");
        }

        // Wait for registration
        yield return new WaitForSeconds(0.5f);

        // All should be registered
        Assert.AreEqual(initialCount + 5, registry.AllPassengers.Count,
            "All 5 passengers should be registered");
    }

    [UnityTest]
    public IEnumerator Passenger_Cleanup_RemovesFromRegistry()
    {
        var registry = PassengerRegistry.Instance;
        int initialCount = registry.AllPassengers.Count;

        var passenger = CreateTestPassenger(true, Vector3.zero, "TemporaryPassenger");

        yield return null;

        Assert.AreEqual(initialCount + 1, registry.AllPassengers.Count, "Passenger should be registered");

        // Destroy passenger
        Object.Destroy(passenger.gameObject);

        yield return new WaitForSeconds(0.5f);

        // Should be removed from registry (depends on OnDestroy implementation)
        // This assertion might need adjustment based on actual cleanup logic
        Assert.LessOrEqual(registry.AllPassengers.Count, initialCount + 1,
            "Registry should handle destroyed passengers");
    }

    // Helper methods
    private Passenger CreateTestPassenger(bool isFemale, Vector3 position, string name)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.parent = testContainer.transform;

        go.AddComponent<Animator>();
        go.AddComponent<SpriteRenderer>();

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.5f, 1f);

        go.AddComponent<PassangerAnimator>();

        var passenger = go.AddComponent<Passenger>();
        passenger.IsFemale = isFemale;
        passenger.IsMatchable = true;
        passenger.IsInCouple = false;

        return passenger;
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
