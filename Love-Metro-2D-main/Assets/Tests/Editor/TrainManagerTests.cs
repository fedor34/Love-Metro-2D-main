using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class TrainManagerTests
{
    private GameObject _trainObject;
    private TrainManager _trainManager;

    [SetUp]
    public void Setup()
    {
        _trainObject = new GameObject("TrainManager");
        _trainManager = _trainObject.AddComponent<TrainManager>();

        SetPrivateField(_trainManager, "_dirImpulseMin", 6f);
        SetPrivateField(_trainManager, "_dirImpulseScale", 35f);
    }

    [TearDown]
    public void TearDown()
    {
        if (_trainObject != null)
            Object.DestroyImmediate(_trainObject);
    }

    [Test]
    public void CalculateDirectionalImpulseMagnitude_BrakingGestureIsStrongerThanAccelerationGesture()
    {
        float accelerationGesture = InvokePrivateMethod<float>(_trainManager, "CalculateDirectionalImpulseMagnitude", 1f, 1f);
        float brakingGesture = InvokePrivateMethod<float>(_trainManager, "CalculateDirectionalImpulseMagnitude", -1f, 1f);

        Assert.Greater(brakingGesture, accelerationGesture);
    }

    [Test]
    public void BuildDirectionalImpulse_UsesGestureSignsForAxes()
    {
        Vector2 impulse = InvokePrivateMethod<Vector2>(_trainManager, "BuildDirectionalImpulse", 1f, -0.25f, 10f);

        Assert.AreEqual(new Vector2(-10f, 6f), impulse);
    }

    [Test]
    public void CalculateHeldAcceleration_UsesConfiguredAccelerationWeight()
    {
        SetPrivateField(_trainManager, "_acceleration", 0.16f);

        float acceleration = InvokePrivateMethod<float>(_trainManager, "CalculateHeldAcceleration", 1f, 0f);

        Assert.AreEqual(0.64f, acceleration, 0.0001f);
    }

    [Test]
    public void SceneTrainTuning_UsesResponsiveAcceleration()
    {
        AssertResponsiveSceneTrainTuning("Assets/Scenes/Scene2.unity");
        AssertResponsiveSceneTrainTuning("Assets/Scenes/Scene3.unity");
    }

    private static T InvokePrivateMethod<T>(object instance, string methodName, params object[] args)
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

    private static void AssertResponsiveSceneTrainTuning(string scenePath)
    {
        string yaml = File.ReadAllText(scenePath);
        Assert.GreaterOrEqual(ReadSerializedFloat(yaml, "_acceleration"), 0.15f, scenePath);
        Assert.GreaterOrEqual(ReadSerializedFloat(yaml, "_startBoost"), 10f, scenePath);
    }

    private static float ReadSerializedFloat(string yaml, string fieldName)
    {
        Match match = Regex.Match(
            yaml,
            @"^\s+" + Regex.Escape(fieldName) + @":\s+([-+]?\d+(?:\.\d+)?)",
            RegexOptions.Multiline);
        Assert.IsTrue(match.Success, $"Serialized field {fieldName} was not found.");
        return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }
}
