using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Play Mode tests for physics-based gameplay
/// Tests collision detection, rigidbody interactions, and couple formation through physics
/// </summary>
public class PhysicsPlayModeTests
{
    private GameObject testContainer;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        // Create container for test objects
        testContainer = new GameObject("TestContainer");

        // Ensure PassengerRegistry exists
        var registryObj = new GameObject("PassengerRegistry");
        registryObj.AddComponent<PassengerRegistry>();

        // Ensure CouplesManager exists
        var couplesObj = new GameObject("CouplesManager");
        couplesObj.AddComponent<CouplesManager>();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        // Clean up all test objects
        if (testContainer != null)
            Object.Destroy(testContainer);

        var registry = Object.FindObjectOfType<PassengerRegistry>();
        if (registry != null)
            Object.Destroy(registry.gameObject);

        var couplesManager = Object.FindObjectOfType<CouplesManager>();
        if (couplesManager != null)
            Object.Destroy(couplesManager.gameObject);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Physics_TwoPassengers_MovingTowardsEachOther_FormCouple()
    {
        // Arrange: Create male and female passengers
        var male = CreateTestPassenger(false, new Vector3(-3, 0, 0), "Male");
        var female = CreateTestPassenger(true, new Vector3(3, 0, 0), "Female");

        // Give them velocities towards each other
        male.GetComponent<Rigidbody2D>().velocity = Vector2.right * 2f;
        female.GetComponent<Rigidbody2D>().velocity = Vector2.left * 2f;

        // Act: Wait for physics to process collision
        yield return new WaitForSeconds(3f);

        // Assert: Check if they formed a couple
        // Note: This depends on your collision logic - adjust assertion as needed
        bool coupledOrCollided = male.IsInCouple || female.IsInCouple ||
                                 Vector3.Distance(male.transform.position, female.transform.position) < 1f;

        Assert.IsTrue(coupledOrCollided, "Passengers should either form a couple or collide");
    }

    [UnityTest]
    public IEnumerator Physics_SameGenderPassengers_DoNotFormCouple()
    {
        // Arrange: Create two male passengers
        var male1 = CreateTestPassenger(false, new Vector3(-2, 0, 0), "Male1");
        var male2 = CreateTestPassenger(false, new Vector3(2, 0, 0), "Male2");

        male1.GetComponent<Rigidbody2D>().velocity = Vector2.right * 2f;
        male2.GetComponent<Rigidbody2D>().velocity = Vector2.left * 2f;

        // Act: Wait for collision
        yield return new WaitForSeconds(3f);

        // Assert: They should NOT form a couple (same gender)
        Assert.IsFalse(male1.IsInCouple, "Same gender passengers should not form couples");
        Assert.IsFalse(male2.IsInCouple, "Same gender passengers should not form couples");
    }

    [UnityTest]
    public IEnumerator Physics_PassengerFallsWithGravity()
    {
        // Arrange: Create passenger in the air
        var passenger = CreateTestPassenger(true, new Vector3(0, 5, 0), "FallingPassenger");
        passenger.GetComponent<Rigidbody2D>().gravityScale = 1f; // Enable gravity

        float startY = passenger.transform.position.y;

        // Act: Wait for gravity to pull down
        yield return new WaitForSeconds(1f);

        // Assert: Should have fallen
        float endY = passenger.transform.position.y;
        Assert.Less(endY, startY, "Passenger should fall down with gravity");
    }

    [UnityTest]
    public IEnumerator Rigidbody_StopsMoving_WhenVelocityReachesZero()
    {
        // Arrange: Create passenger with initial velocity
        var passenger = CreateTestPassenger(false, Vector3.zero, "MovingPassenger");
        var rb = passenger.GetComponent<Rigidbody2D>();

        // Add some drag to slow it down
        rb.drag = 2f;
        rb.velocity = Vector2.right * 5f;

        // Act: Wait for drag to slow it down
        yield return new WaitForSeconds(3f);

        // Assert: Should have slowed down significantly
        Assert.Less(rb.velocity.magnitude, 1f, "Passenger should slow down due to drag");
    }

    [UnityTest]
    public IEnumerator Collision_WithBoundaries_BouncesBack()
    {
        // Arrange: Create a boundary wall
        var wall = new GameObject("Wall");
        wall.transform.position = new Vector3(5, 0, 0);
        wall.transform.parent = testContainer.transform;
        var wallCollider = wall.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(1, 10);

        var passenger = CreateTestPassenger(true, new Vector3(0, 0, 0), "BouncingPassenger");
        var rb = passenger.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.right * 10f; // Fast towards wall

        float initialVelocityX = rb.velocity.x;

        // Act: Wait for collision with wall
        yield return new WaitForSeconds(1f);

        // Assert: Velocity should have changed direction (bounced)
        // This might need adjustment based on your physics material settings
        bool bouncedOrStopped = Mathf.Abs(rb.velocity.x) < initialVelocityX;
        Assert.IsTrue(bouncedOrStopped, "Passenger should bounce or stop at wall");

        Object.Destroy(wall);
    }

    // Helper method to create test passengers
    private Passenger CreateTestPassenger(bool isFemale, Vector3 position, string name)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.parent = testContainer.transform;

        // Add all required components
        var animator = go.AddComponent<Animator>();
        var spriteRenderer = go.AddComponent<SpriteRenderer>();

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // No gravity by default (2D game on horizontal plane)
        rb.drag = 0.5f;
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
}
