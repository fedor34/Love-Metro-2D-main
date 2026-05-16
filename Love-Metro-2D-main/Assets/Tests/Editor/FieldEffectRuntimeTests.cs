using System.Collections.Generic;
using LoveMetro.FieldEffects;
using NUnit.Framework;
using UnityEngine;

public class FieldEffectRegistryTests
{
    [Test]
    public void Register_IndexesEffect_ByCategoryAndType()
    {
        var registry = new FieldEffectRegistry();
        var effect = new FakeEffect(FieldEffectType.Wind, radius: 1f);

        bool registered = registry.TryRegister(effect, out var data, out var category);

        Assert.IsTrue(registered);
        Assert.AreEqual(FieldEffectCategory.Movement, category);
        Assert.AreEqual(FieldEffectType.Wind, data.effectType);
        CollectionAssert.Contains(registry.GetEffectsByCategory(FieldEffectCategory.Movement), effect);
        CollectionAssert.Contains(registry.GetEffectsByType(FieldEffectType.Wind), effect);
        Assert.AreEqual(1, registry.GetTotalEffectsCount());
    }

    [Test]
    public void Register_IgnoresDuplicate()
    {
        var registry = new FieldEffectRegistry();
        var effect = new FakeEffect(FieldEffectType.Gravity, radius: 1f);

        Assert.IsTrue(registry.TryRegister(effect, out _, out _));
        Assert.IsFalse(registry.TryRegister(effect, out _, out _));
        Assert.AreEqual(1, registry.GetTotalEffectsCount());
    }

    [Test]
    public void Unregister_RemovesEffect_FromBothIndexes()
    {
        var registry = new FieldEffectRegistry();
        var effect = new FakeEffect(FieldEffectType.Magnetic, radius: 1f);
        registry.TryRegister(effect, out _, out _);

        bool unregistered = registry.TryUnregister(effect, out _, out var category);

        Assert.IsTrue(unregistered);
        Assert.AreEqual(FieldEffectCategory.Movement, category);
        Assert.AreEqual(0, registry.GetEffectsByCategory(FieldEffectCategory.Movement).Count);
        Assert.AreEqual(0, registry.GetEffectsByType(FieldEffectType.Magnetic).Count);
    }

    [Test]
    public void ClassifyCategory_MapsKnownTypes()
    {
        Assert.AreEqual(FieldEffectCategory.Movement, FieldEffectRegistry.ClassifyCategory(FieldEffectType.Wind));
        Assert.AreEqual(FieldEffectCategory.Movement, FieldEffectRegistry.ClassifyCategory(FieldEffectType.Vortex));
        Assert.AreEqual(FieldEffectCategory.Modifier, FieldEffectRegistry.ClassifyCategory(FieldEffectType.Slowdown));
        Assert.AreEqual(FieldEffectCategory.Trigger, FieldEffectRegistry.ClassifyCategory(FieldEffectType.Teleport));
        Assert.AreEqual(FieldEffectCategory.Visual, FieldEffectRegistry.ClassifyCategory(FieldEffectType.Visual));
        Assert.AreEqual(FieldEffectCategory.Audio, FieldEffectRegistry.ClassifyCategory(FieldEffectType.Audio));
    }

    [Test]
    public void TryGetMetadata_ReturnsFalse_ForNullEffect()
    {
        Assert.IsFalse(FieldEffectRegistry.TryGetMetadata(null, out var data, out var category));
        Assert.IsNull(data);
        Assert.AreEqual(FieldEffectCategory.Other, category);
    }

    [Test]
    public void GetAllEffects_ReturnsEveryRegistered()
    {
        var registry = new FieldEffectRegistry();
        var a = new FakeEffect(FieldEffectType.Wind, 1f);
        var b = new FakeEffect(FieldEffectType.Friction, 1f);
        registry.TryRegister(a, out _, out _);
        registry.TryRegister(b, out _, out _);

        var all = registry.GetAllEffects();

        Assert.AreEqual(2, all.Count);
        CollectionAssert.Contains(all, a);
        CollectionAssert.Contains(all, b);
    }
}

public class FieldEffectTargetRegistryTests
{
    [Test]
    public void Register_AddsTargetOnce()
    {
        var registry = new FieldEffectTargetRegistry();
        var target = new FakeTarget(Vector3.zero);

        Assert.IsTrue(registry.Register(target));
        Assert.IsFalse(registry.Register(target));
        Assert.AreEqual(1, registry.Count);
        Assert.IsTrue(registry.TryGetActiveEffects(target, out var effects));
        Assert.IsNotNull(effects);
        Assert.AreEqual(0, effects.Count);
    }

    [Test]
    public void RemoveTarget_DropsFromBothCollections()
    {
        var registry = new FieldEffectTargetRegistry();
        var target = new FakeTarget(Vector3.zero);
        registry.Register(target);

        registry.RemoveTarget(target);

        Assert.AreEqual(0, registry.Count);
        Assert.IsFalse(registry.TryGetActiveEffects(target, out _));
    }

    [Test]
    public void HasActiveEffect_DetectsEffectByReference()
    {
        var effect = new FakeEffect(FieldEffectType.Wind, 1f);
        var active = new List<ActiveEffectData> { new ActiveEffectData(effect, startTime: 0f) };

        Assert.IsTrue(FieldEffectTargetRegistry.HasActiveEffect(active, effect));
        Assert.IsFalse(FieldEffectTargetRegistry.HasActiveEffect(active, new FakeEffect(FieldEffectType.Wind, 1f)));
        Assert.IsFalse(FieldEffectTargetRegistry.HasActiveEffect(null, effect));
    }

    [Test]
    public void TryGetTargetPosition_CatchesExceptions()
    {
        var throwingTarget = new ThrowingTarget();

        bool ok = FieldEffectTargetRegistry.TryGetTargetPosition(throwingTarget, out var position);

        Assert.IsFalse(ok);
        Assert.AreEqual(Vector3.zero, position);
    }
}

public class FieldEffectApplicatorTests
{
    [Test]
    public void Apply_AddsActiveEffect_AndCallsEffect()
    {
        var applicator = new FieldEffectApplicator(maxEffectsPerTarget: 5, timeProvider: () => 1.5f);
        var target = new FakeTarget(Vector3.zero);
        var effect = new FakeEffect(FieldEffectType.Wind, 1f);
        var active = new List<ActiveEffectData>();

        int enters = 0;
        target.OnEnter = _ => enters++;
        int appliedEvents = 0;
        applicator.EffectApplied += (_, _) => appliedEvents++;

        applicator.Apply(target, effect, active, deltaTime: 0.1f);

        Assert.AreEqual(1, active.Count);
        Assert.AreEqual(1.5f, active[0].StartTime);
        Assert.AreEqual(1, effect.ApplyCount);
        Assert.AreEqual(1, enters);
        Assert.AreEqual(1, appliedEvents);
    }

    [Test]
    public void Apply_DoesNotReEnter_WhenEffectAlreadyActive()
    {
        var applicator = new FieldEffectApplicator(5, () => 0f);
        var target = new FakeTarget(Vector3.zero);
        var effect = new FakeEffect(FieldEffectType.Wind, 1f);
        var active = new List<ActiveEffectData>();
        int enters = 0;
        target.OnEnter = _ => enters++;

        applicator.Apply(target, effect, active, 0.1f);
        applicator.Apply(target, effect, active, 0.1f);

        Assert.AreEqual(1, active.Count);
        Assert.AreEqual(1, enters);
        Assert.AreEqual(2, effect.ApplyCount);
    }

    [Test]
    public void CanApply_RespectsMaxEffectsPerTarget()
    {
        var applicator = new FieldEffectApplicator(maxEffectsPerTarget: 1, timeProvider: () => 0f);
        var target = new FakeTarget(Vector3.zero);
        var primary = new FakeEffect(FieldEffectType.Wind, 1f);
        var secondary = new FakeEffect(FieldEffectType.Wind, 1f);
        var active = new List<ActiveEffectData>();

        applicator.Apply(target, primary, active, 0.1f);

        Assert.IsTrue(applicator.CanApply(target, active, primary), "Already-active effect must remain applicable.");
        Assert.IsFalse(applicator.CanApply(target, active, secondary), "Second effect must be blocked at max.");
    }

    [Test]
    public void CanApply_DropsEffectsTargetCannotBeAffectedBy()
    {
        var applicator = new FieldEffectApplicator(5, () => 0f);
        var target = new FakeTarget(Vector3.zero) { Accept = false };
        var effect = new FakeEffect(FieldEffectType.Wind, 1f);
        var active = new List<ActiveEffectData>();

        Assert.IsFalse(applicator.CanApply(target, active, effect));
    }

    [Test]
    public void Remove_NotifiesEffectAndTarget()
    {
        var applicator = new FieldEffectApplicator(5, () => 0f);
        var target = new FakeTarget(Vector3.zero);
        var effect = new FakeEffect(FieldEffectType.Wind, 1f);
        var active = new List<ActiveEffectData>();
        applicator.Apply(target, effect, active, 0.1f);

        int exits = 0;
        target.OnExit = _ => exits++;
        int removedEvents = 0;
        applicator.EffectRemoved += (_, _) => removedEvents++;

        bool removed = applicator.Remove(target, effect, active);

        Assert.IsTrue(removed);
        Assert.AreEqual(0, active.Count);
        Assert.AreEqual(1, effect.RemoveCount);
        Assert.AreEqual(1, exits);
        Assert.AreEqual(1, removedEvents);
    }

    [Test]
    public void Remove_ReturnsFalse_WhenEffectNotActive()
    {
        var applicator = new FieldEffectApplicator(5, () => 0f);
        var target = new FakeTarget(Vector3.zero);
        var effect = new FakeEffect(FieldEffectType.Wind, 1f);
        var active = new List<ActiveEffectData>();

        Assert.IsFalse(applicator.Remove(target, effect, active));
    }

    [Test]
    public void RemoveAllFromTarget_DrainsActiveList()
    {
        var applicator = new FieldEffectApplicator(5, () => 0f);
        var target = new FakeTarget(Vector3.zero);
        var a = new FakeEffect(FieldEffectType.Wind, 1f);
        var b = new FakeEffect(FieldEffectType.Gravity, 1f);
        var active = new List<ActiveEffectData>();
        applicator.Apply(target, a, active, 0.1f);
        applicator.Apply(target, b, active, 0.1f);

        applicator.RemoveAllFromTarget(target, active);

        Assert.AreEqual(0, active.Count);
        Assert.AreEqual(1, a.RemoveCount);
        Assert.AreEqual(1, b.RemoveCount);
    }
}

internal sealed class FakeEffect : IFieldEffect
{
    private readonly FieldEffectData _data;

    public FakeEffect(FieldEffectType type, float radius)
    {
        _data = new FieldEffectData(type, 1f, radius, Vector3.zero);
    }

    public int ApplyCount { get; private set; }
    public int RemoveCount { get; private set; }

    public void ApplyEffect(IFieldEffectTarget target, float deltaTime) => ApplyCount++;
    public void RemoveEffect(IFieldEffectTarget target) => RemoveCount++;
    public bool IsInEffectZone(Vector3 targetPosition) => true;
    public FieldEffectData GetEffectData() => _data;
}

internal sealed class FakeTarget : IFieldEffectTarget
{
    public FakeTarget(Vector3 position) { Position = position; }
    public Vector3 Position { get; set; }
    public bool Accept { get; set; } = true;
    public System.Action<IFieldEffect> OnEnter;
    public System.Action<IFieldEffect> OnExit;

    public void ApplyFieldForce(Vector2 force, FieldEffectType effectType) { }
    public void ApplyFieldForce(Vector3 force, ForceMode2D forceMode) { }
    public Vector3 GetPosition() => Position;
    public Rigidbody2D GetRigidbody() => null;
    public bool CanBeAffectedBy(FieldEffectType effectType) => Accept;
    public void OnEnterFieldEffect(IFieldEffect effect) => OnEnter?.Invoke(effect);
    public void OnExitFieldEffect(IFieldEffect effect) => OnExit?.Invoke(effect);
}

internal sealed class ThrowingTarget : IFieldEffectTarget
{
    public void ApplyFieldForce(Vector2 force, FieldEffectType effectType) { }
    public void ApplyFieldForce(Vector3 force, ForceMode2D forceMode) { }
    public Vector3 GetPosition() => throw new System.InvalidOperationException("boom");
    public Rigidbody2D GetRigidbody() => null;
    public bool CanBeAffectedBy(FieldEffectType effectType) => true;
    public void OnEnterFieldEffect(IFieldEffect effect) { }
    public void OnExitFieldEffect(IFieldEffect effect) { }
}
