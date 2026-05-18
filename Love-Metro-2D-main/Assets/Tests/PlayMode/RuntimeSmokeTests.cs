using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RuntimeSmokeTests
{
    [UnityTest]
    public IEnumerator MainMenu_LoadsWithSingleRuntimeServices()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return null;

        GameBootstrap.EnsureRuntimeServices();

        Assert.LessOrEqual(Object.FindObjectsOfType<PassengerRegistry>().Length, 1);
        Assert.LessOrEqual(Object.FindObjectsOfType<CouplesManager>().Length, 1);
        Assert.LessOrEqual(Object.FindObjectsOfType<FieldEffectSystem>().Length, 1);
        Assert.LessOrEqual(Object.FindObjectsOfType<ClickDirectionManager>().Length, 1);
        Assert.LessOrEqual(Object.FindObjectsOfType<ManualPairingManager>().Length, 1);
    }

    [UnityTest]
    public IEnumerator Scene2_LoadsAndSpawnsPassengers()
    {
        SceneManager.LoadScene("Scene2");
        yield return null;
        yield return null;
        yield return null;

        PassangerSpawner spawner = Object.FindObjectOfType<PassangerSpawner>();
        PassangersContainer container = Object.FindObjectOfType<PassangersContainer>();

        Assert.IsNotNull(spawner);
        Assert.IsNotNull(container);

        container.CleanupNullReferences();
        int count = container.Passangers?.Count ?? 0;
        Assert.GreaterOrEqual(count, 1);
        Assert.LessOrEqual(count, 20);
    }

    [UnityTest]
    public IEnumerator MainMenu_Play_LoadsScene2WithBackgroundScroller()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return null;

        MenuManager menuManager = Object.FindObjectOfType<MenuManager>();
        Assert.IsNotNull(menuManager);

        menuManager.OnPlayButtonClicked();

        float deadline = Time.realtimeSinceStartup + 5f;
        while (SceneManager.GetActiveScene().name != "Scene2" && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.AreEqual("Scene2", SceneManager.GetActiveScene().name);

        yield return null;
        yield return null;

        Assert.IsNotNull(Object.FindObjectOfType<TrainManager>());
        Assert.IsNotNull(Object.FindObjectOfType<SimpleBackgroundScroller>());
    }

    [UnityTest]
    public IEnumerator WindForce_MovesPassengerIntoFlyingState()
    {
        GameBootstrap.EnsureRuntimeServices();

        GameObject trainObject = new GameObject("train");
        TrainManager train = trainObject.AddComponent<TrainManager>();
        Passenger passenger = CreatePassenger("wind-passenger");

        passenger.Initiate(Vector2.right, train, null);
        passenger.ApplyFieldForce(Vector2.right * 10f, FieldEffectType.Wind);

        Assert.AreEqual("Flying", passenger.GetCurrentStateName());

        Object.Destroy(passenger.gameObject);
        Object.Destroy(trainObject);
        yield return null;
    }

    private static Passenger CreatePassenger(string name)
    {
        GameObject passengerObject = new GameObject(name);
        passengerObject.AddComponent<Rigidbody2D>();
        passengerObject.AddComponent<BoxCollider2D>();
        passengerObject.AddComponent<Animator>();
        passengerObject.AddComponent<SpriteRenderer>();
        passengerObject.AddComponent<PassangerAnimator>();
        return passengerObject.AddComponent<Passenger>();
    }
}
