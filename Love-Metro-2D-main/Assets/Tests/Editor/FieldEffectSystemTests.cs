using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class FieldEffectSystemTests
{
    private GameObject _systemObject;
    private FieldEffectSystem _system;

    [SetUp]
    public void Setup()
    {
        ResetFieldEffectSystemStatics();

        _systemObject = new GameObject("FieldEffectSystem");
        _system = _systemObject.AddComponent<FieldEffectSystem>();

        InvokePrivateMethod<object>(_system, "Awake");
        SetPrivateField(_system, "_enableDebugMode", false);
        SetPrivateField(_system, "_enableGizmos", false);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var effect in Object.FindObjectsOfType<FieldEffectSystemTestEffect>())
            Object.DestroyImmediate(effect.gameObject);

        foreach (var target in Object.FindObjectsOfType<FieldEffectSystemTestTarget>())
            Object.DestroyImmediate(target.gameObject);

        if (_systemObject != null)
            Object.DestroyImmediate(_systemObject);

        ResetFieldEffectSystemStatics();
    }

    [Test]
    public void UpdateEffects_ReappliesAlreadyActiveEffect_WhenTargetReachedMaxEffects()
    {
        SetPrivateField(_system, "_maxEffectsPerTarget", 1);

        var target = CreateTarget(Vector3.zero);
        var primaryEffect = CreateEffect("Primary", Vector3.zero, 10f);
        var blockedEffect = CreateEffect("Blocked", Vector3.zero, 10f);

        _system.RegisterTarget(target);
        _system.RegisterEffect(primaryEffect);
        _system.RegisterEffect(blockedEffect);

        InvokePrivateMethod<object>(_system, "UpdateEffects");
        InvokePrivateMethod<object>(_system, "UpdateEffects");

        Assert.AreEqual(2, primaryEffect.ApplyCount);
        Assert.AreEqual(0, blockedEffect.ApplyCount);
        Assert.AreEqual(1, target.EnterCount);
    }

    [Test]
    public void UnregisterEffect_DoesNotNotifyTargets_ThatNeverHadThisEffect()
    {
        var target = CreateTarget(Vector3.zero);
        var distantEffect = CreateEffect("Distant", new Vector3(100f, 0f, 0f), 1f);

        _system.RegisterTarget(target);
        _system.RegisterEffect(distantEffect);

        InvokePrivateMethod<object>(_system, "UpdateEffects");
        _system.UnregisterEffect(distantEffect);

        Assert.AreEqual(0, distantEffect.RemoveCount);
        Assert.AreEqual(0, target.ExitCount);
    }

    private static void ResetFieldEffectSystemStatics()
    {
        typeof(FieldEffectSystem)
            .GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static)
            ?.Invoke(null, null);
    }

    private FieldEffectSystemTestTarget CreateTarget(Vector3 position)
    {
        var gameObject = new GameObject("Target");
        gameObject.transform.position = position;
        return gameObject.AddComponent<FieldEffectSystemTestTarget>();
    }

    private FieldEffectSystemTestEffect CreateEffect(string name, Vector3 position, float radius)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.position = position;

        var effect = gameObject.AddComponent<FieldEffectSystemTestEffect>();
        effect.Configure(position, radius);
        return effect;
    }

    private static T InvokePrivateMethod<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)method.Invoke(instance, args);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(instance, value);
    }
}

internal sealed class FieldEffectSystemTestEffect : MonoBehaviour, IFieldEffect
{
    private FieldEffectData _data;

    public int ApplyCount { get; private set; }
    public int RemoveCount { get; private set; }

    public void Configure(Vector3 position, float radius)
    {
        _data = new FieldEffectData(FieldEffectType.Wind, 1f, radius, position);
        transform.position = position;
    }

    public void ApplyEffect(IFieldEffectTarget target, float deltaTime)
    {
        ApplyCount++;
    }

    public void RemoveEffect(IFieldEffectTarget target)
    {
        RemoveCount++;
    }

    public bool IsInEffectZone(Vector3 targetPosition)
    {
        return Vector3.Distance(transform.position, targetPosition) <= _data.radius;
    }

    public FieldEffectData GetEffectData()
    {
        return _data;
    }
}

internal sealed class FieldEffectSystemTestTarget : MonoBehaviour, IFieldEffectTarget
{
    public int EnterCount { get; private set; }
    public int ExitCount { get; private set; }

    public void ApplyFieldForce(Vector2 force, FieldEffectType effectType)
    {
    }

    public void ApplyFieldForce(Vector3 force, ForceMode2D forceMode)
    {
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public Rigidbody2D GetRigidbody()
    {
        return null;
    }

    public bool CanBeAffectedBy(FieldEffectType effectType)
    {
        return true;
    }

    public void OnEnterFieldEffect(IFieldEffect effect)
    {
        EnterCount++;
    }

    public void OnExitFieldEffect(IFieldEffect effect)
    {
        ExitCount++;
    }
}
