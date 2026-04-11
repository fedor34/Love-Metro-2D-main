using NUnit.Framework;
using UnityEngine;

public class ParallaxEffectTests
{
    private GameObject _effectObject;
    private ParallaxEffect _effect;

    [SetUp]
    public void Setup()
    {
        _effectObject = new GameObject("ParallaxEffectTest");
        _effect = _effectObject.AddComponent<ParallaxEffect>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_effectObject != null)
            Object.DestroyImmediate(_effectObject);
    }

    [Test]
    public void ShouldReadSpeedFromTrain_ReturnsFalse_WhenTrainManagerIsMissing()
    {
        bool result = InvokePrivateInstance<bool>(_effect, "ShouldReadSpeedFromTrain");
        Assert.IsFalse(result);
    }

    [Test]
    public void ShouldReadSpeedFromTrain_RespectsFreshExternalSpeed()
    {
        GameObject trainObject = new GameObject("Train");
        TrainManager trainManager = trainObject.AddComponent<TrainManager>();

        SetPrivateField(_effect, "_trainManager", trainManager);
        SetPrivateField(_effect, "_lastExternalSetTime", Time.time);

        bool result = InvokePrivateInstance<bool>(_effect, "ShouldReadSpeedFromTrain");

        Object.DestroyImmediate(trainObject);
        Assert.IsFalse(result);
    }

    private static T InvokePrivateInstance<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)method.Invoke(instance, args);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(instance, value);
    }
}
