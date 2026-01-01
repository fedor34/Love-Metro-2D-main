using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Play Mode tests for passenger abilities system
/// Tests ability activation, stacking, and time-based effects
/// </summary>
public class AbilitiesPlayModeTests
{
    private GameObject testContainer;
    private ScoreCounter scoreCounter;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        testContainer = new GameObject("TestContainer");

        // Setup ScoreCounter
        var scoreObj = new GameObject("ScoreCounter");
        scoreObj.AddComponent<Canvas>();
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

        if (scoreCounter != null)
            Object.Destroy(scoreCounter.gameObject);

        yield return null;
    }

    [UnityTest]
    public IEnumerator VIPAbility_DoublesPoints_WhenMatched()
    {
        // Create passenger with abilities component
        var passenger = CreateTestPassenger(true, Vector3.zero, "VIPPassenger");
        var abilities = passenger.gameObject.AddComponent<PassengerAbilities>();

        // Create and add VIP ability
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility);

        yield return null;

        // Verify ability is added
        Assert.IsTrue(abilities.HasAbility<VipAbility>(), "Should have VIP ability");

        // Invoke match with 100 base points
        abilities.InvokeMatched(null, scoreCounter, 100);

        yield return new WaitForSeconds(1f);

        // Check score - should be 200 (doubled by VIP)
        int score = GetPrivateField<int>(scoreCounter, "_score");
        Assert.GreaterOrEqual(score, 200, "VIP ability should double points to 200");

        Object.Destroy(vipAbility);
    }

    [UnityTest]
    public IEnumerator MultipleAbilities_StackCorrectly()
    {
        var passenger = CreateTestPassenger(false, Vector3.zero, "MultiAbilityPassenger");
        var abilities = passenger.gameObject.AddComponent<PassengerAbilities>();

        // Add multiple VIP abilities (if they stack)
        var vip1 = ScriptableObject.CreateInstance<VipAbility>();
        var vip2 = ScriptableObject.CreateInstance<VipAbility>();

        abilities.AddAbility(vip1);
        abilities.AddAbility(vip2);

        yield return null;

        // Invoke match
        abilities.InvokeMatched(null, scoreCounter, 100);

        yield return new WaitForSeconds(1f);

        // With 2 VIP abilities: 100 * 2 * 2 = 400
        int score = GetPrivateField<int>(scoreCounter, "_score");
        Assert.GreaterOrEqual(score, 400, "Multiple VIP abilities should stack (100 * 2 * 2 = 400)");

        Object.Destroy(vip1);
        Object.Destroy(vip2);
    }

    [UnityTest]
    public IEnumerator Ability_CanBeAdded_AndRemoved_Dynamically()
    {
        var passenger = CreateTestPassenger(true, Vector3.zero, "DynamicAbilityPassenger");
        var abilities = passenger.gameObject.AddComponent<PassengerAbilities>();

        // Add ability
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility);

        yield return null;
        Assert.IsTrue(abilities.HasAbility<VipAbility>(), "Should have VIP ability after adding");

        // Remove ability
        abilities.RemoveAbility(vipAbility);

        yield return null;
        Assert.IsFalse(abilities.HasAbility<VipAbility>(), "Should not have VIP ability after removing");

        Object.Destroy(vipAbility);
    }

    [UnityTest]
    public IEnumerator PassengerWithoutAbilities_UsesBasePoints()
    {
        var passenger = CreateTestPassenger(false, Vector3.zero, "NormalPassenger");
        var abilities = passenger.gameObject.AddComponent<PassengerAbilities>();

        // No abilities added
        yield return null;

        // Invoke match with 100 base points
        abilities.InvokeMatched(null, scoreCounter, 100);

        yield return new WaitForSeconds(1f);

        // Score should be exactly 100 (no multipliers)
        int score = GetPrivateField<int>(scoreCounter, "_score");
        Assert.AreEqual(100, score, "Without abilities, should award exactly base points");
    }

    [UnityTest]
    public IEnumerator Abilities_PersistAcrossFrames()
    {
        var passenger = CreateTestPassenger(true, Vector3.zero, "PersistentAbilityPassenger");
        var abilities = passenger.gameObject.AddComponent<PassengerAbilities>();

        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility);

        // Wait several frames
        for (int i = 0; i < 10; i++)
        {
            yield return null;
        }

        // Ability should still be there
        Assert.IsTrue(abilities.HasAbility<VipAbility>(), "Ability should persist across frames");

        Object.Destroy(vipAbility);
    }

    [UnityTest]
    public IEnumerator Ability_WorksAfterPassengerMovement()
    {
        var passenger = CreateTestPassenger(false, new Vector3(0, 0, 0), "MovingAbilityPassenger");
        var abilities = passenger.gameObject.AddComponent<PassengerAbilities>();

        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();
        abilities.AddAbility(vipAbility);

        // Move passenger
        var rb = passenger.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.right * 5f;

        // Wait for movement
        yield return new WaitForSeconds(1f);

        // Ability should still work after movement
        Assert.IsTrue(abilities.HasAbility<VipAbility>(), "Ability should work after passenger moves");

        abilities.InvokeMatched(null, scoreCounter, 100);
        yield return new WaitForSeconds(1f);

        int score = GetPrivateField<int>(scoreCounter, "_score");
        Assert.GreaterOrEqual(score, 200, "Ability should still double points after movement");

        Object.Destroy(vipAbility);
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

        go.AddComponent<BoxCollider2D>();
        go.AddComponent<PassangerAnimator>();

        var passenger = go.AddComponent<Passenger>();
        passenger.IsFemale = isFemale;
        passenger.IsMatchable = true;

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
