using NUnit.Framework;
using UnityEngine;

public class ClickDirectionManagerTests
{
    private GameObject _managerObject;
    private ClickDirectionManager _manager;
    private GameObject _cameraObject;

    [SetUp]
    public void Setup()
    {
        InvokePrivateStatic(typeof(ClickDirectionManager), "ResetStaticState");
        _managerObject = new GameObject("ClickDirectionManagerTest");
        _manager = _managerObject.AddComponent<ClickDirectionManager>();

        _cameraObject = new GameObject("MainCamera");
        _cameraObject.tag = "MainCamera";
        _cameraObject.AddComponent<Camera>();
    }

    [TearDown]
    public void TearDown()
    {
        InvokePrivateStatic(typeof(ClickDirectionManager), "ResetStaticState");

        if (_cameraObject != null)
            Object.DestroyImmediate(_cameraObject);

        if (_managerObject != null)
            Object.DestroyImmediate(_managerObject);
    }

    [Test]
    public void ClampScreenDirection_LimitsMagnitudeToMaxPixels()
    {
        Vector2 clamped = InvokePrivateStatic<Vector2>(
            typeof(ClickDirectionManager),
            "ClampScreenDirection",
            new Vector2(1200f, 0f),
            300f);

        Assert.That(clamped.magnitude, Is.EqualTo(300f).Within(0.001f));
    }

    [Test]
    public void BuildDirectionFromScreen_NormalizedModeReturnsScaledUnitVector()
    {
        Vector2 direction = InvokePrivateStatic<Vector2>(
            typeof(ClickDirectionManager),
            "BuildDirectionFromScreen",
            new Vector2(3f, 4f),
            true,
            2f);

        Assert.That(direction.x, Is.EqualTo(1.2f).Within(0.0001f));
        Assert.That(direction.y, Is.EqualTo(1.6f).Within(0.0001f));
    }

    [Test]
    public void ComputeNormalizedAxis_DeadZoneSuppressesSmallInput()
    {
        float axis = InvokePrivateStatic<float>(
            typeof(ClickDirectionManager),
            "ComputeNormalizedAxis",
            10f,
            500f,
            0.05f);

        Assert.AreEqual(0f, axis);
    }

    [Test]
    public void HandlePointerUp_SuppressedRelease_DoesNotPublishReleasePoint()
    {
        SetPrivateField(_manager, "_mainCamera", _cameraObject.GetComponent<Camera>());
        SetPrivateField(_manager, "_suppressReleaseUntilMouseUp", true);

        InvokePrivateInstance(_manager, "HandlePointerUp", Vector2.zero);

        Assert.IsFalse(ClickDirectionManager.HasReleasePoint);
        Assert.AreEqual(-999f, ClickDirectionManager.LastReleaseTime);
    }

    private static T InvokePrivateStatic<T>(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (T)method.Invoke(null, args);
    }

    private static void InvokePrivateStatic(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Invoke(null, args);
    }

    private static void InvokePrivateInstance(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(instance, args);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(instance, value);
    }
}
