using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SortingLayerEditorTests
{
    private GameObject _root;
    private PassangersContainer _container;
    private SortingLayerEditor _sortingLayerEditor;

    [SetUp]
    public void Setup()
    {
        _root = new GameObject("SortingLayerEditorTestRoot");
        _container = _root.AddComponent<PassangersContainer>();
        _container.Passangers = new List<Passenger>();
        _sortingLayerEditor = _root.AddComponent<SortingLayerEditor>();
        InvokePrivateMethod(_sortingLayerEditor, "Start");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Passenger passenger in Object.FindObjectsOfType<Passenger>())
            Object.DestroyImmediate(passenger.gameObject);

        if (_root != null)
            Object.DestroyImmediate(_root);
    }

    [Test]
    public void Update_ResortsWhenPassengerYChanges()
    {
        Passenger low = CreatePassenger("Low", 0f);
        Passenger high = CreatePassenger("High", 2f);
        _container.Passangers.Add(low);
        _container.Passangers.Add(high);

        InvokePrivateMethod(_sortingLayerEditor, "Update");
        Assert.AreEqual(0, high.GetComponent<SpriteRenderer>().sortingOrder);
        Assert.AreEqual(1, low.GetComponent<SpriteRenderer>().sortingOrder);

        low.transform.position = new Vector3(0f, 3f, 0f);
        InvokePrivateMethod(_sortingLayerEditor, "Update");

        Assert.AreEqual(0, low.GetComponent<SpriteRenderer>().sortingOrder);
        Assert.AreEqual(1, high.GetComponent<SpriteRenderer>().sortingOrder);
    }

    [Test]
    public void Update_RemovesDestroyedPassengerSprites()
    {
        Passenger survivor = CreatePassenger("Survivor", 0f);
        Passenger destroyed = CreatePassenger("Destroyed", 2f);
        _container.Passangers.Add(survivor);
        _container.Passangers.Add(destroyed);

        InvokePrivateMethod(_sortingLayerEditor, "Update");
        Object.DestroyImmediate(destroyed.gameObject);

        Assert.DoesNotThrow(() => InvokePrivateMethod(_sortingLayerEditor, "Update"));
        Assert.AreEqual(0, survivor.GetComponent<SpriteRenderer>().sortingOrder);
    }

    private Passenger CreatePassenger(string name, float y)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.position = new Vector3(0f, y, 0f);
        gameObject.AddComponent<SpriteRenderer>();
        return gameObject.AddComponent<Passenger>();
    }

    private static void InvokePrivateMethod(object instance, string methodName)
    {
        instance.GetType()
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(instance, null);
    }
}
