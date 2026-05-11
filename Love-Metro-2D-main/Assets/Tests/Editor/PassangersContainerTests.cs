using NUnit.Framework;
using UnityEngine;

public class PassangersContainerTests
{
    private GameObject _containerObject;
    private PassangersContainer _container;

    [SetUp]
    public void SetUp()
    {
        _containerObject = new GameObject("container");
        _container = _containerObject.AddComponent<PassangersContainer>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_containerObject);
    }

    [Test]
    public void AddPassenger_AddsOnceAndAssignsContainer()
    {
        Passenger passenger = CreatePassenger("passenger");

        _container.AddPassenger(passenger);
        _container.AddPassenger(passenger);

        Assert.AreEqual(1, _container.Count);
        Assert.AreSame(_container, passenger.container);

        Object.DestroyImmediate(passenger.gameObject);
    }

    [Test]
    public void RemovePassenger_RemovesPassengerButKeepsCompatibilityMethod()
    {
        Passenger passenger = CreatePassenger("passenger");
        _container.AddPassenger(passenger);

        Assert.IsTrue(_container.RemovePassenger(passenger));
        _container.RemovePassanger(passenger);

        Assert.AreEqual(0, _container.Count);

        Object.DestroyImmediate(passenger.gameObject);
    }

    [Test]
    public void ClearPassengers_EmptiesListWithoutDestroyingObjects()
    {
        Passenger first = CreatePassenger("first");
        Passenger second = CreatePassenger("second");
        _container.AddPassenger(first);
        _container.AddPassenger(second);

        _container.ClearPassengers();

        Assert.AreEqual(0, _container.Count);
        Assert.IsNotNull(first);
        Assert.IsNotNull(second);

        Object.DestroyImmediate(first.gameObject);
        Object.DestroyImmediate(second.gameObject);
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
