using System;
using System.Collections.Generic;

namespace LoveMetro.FieldEffects
{
    /// <summary>
    /// Applies and removes individual field effects on a target while respecting the
    /// max-effects-per-target cap and notifying listeners. POCO; unit-testable.
    /// </summary>
    public sealed class FieldEffectApplicator
    {
        private readonly Func<float> _timeProvider;
        private readonly Func<int> _maxEffectsPerTargetProvider;

        public event Action<global::IFieldEffectTarget, global::IFieldEffect> EffectApplied;
        public event Action<global::IFieldEffectTarget, global::IFieldEffect> EffectRemoved;

        public FieldEffectApplicator(Func<int> maxEffectsPerTargetProvider, Func<float> timeProvider)
        {
            _maxEffectsPerTargetProvider = maxEffectsPerTargetProvider ?? (() => 5);
            _timeProvider = timeProvider ?? (() => 0f);
        }

        public FieldEffectApplicator(int maxEffectsPerTarget, Func<float> timeProvider)
            : this(() => maxEffectsPerTarget, timeProvider)
        {
        }

        public bool CanApply(global::IFieldEffectTarget target, List<global::ActiveEffectData> activeEffects, global::IFieldEffect effect)
        {
            if (target == null || effect == null)
                return false;

            global::FieldEffectData data = effect.GetEffectData();
            if (data == null || !target.CanBeAffectedBy(data.effectType))
                return false;

            return FieldEffectTargetRegistry.HasActiveEffect(activeEffects, effect)
                || activeEffects.Count < _maxEffectsPerTargetProvider();
        }

        public void Apply(global::IFieldEffectTarget target, global::IFieldEffect effect, List<global::ActiveEffectData> activeEffects, float deltaTime)
        {
            if (target == null || effect == null || activeEffects == null)
                return;

            global::ActiveEffectData existing = activeEffects.Find(active => active != null && active.Effect == effect);
            if (existing == null)
            {
                activeEffects.Add(new global::ActiveEffectData(effect, _timeProvider()));
                target.OnEnterFieldEffect(effect);
                EffectApplied?.Invoke(target, effect);
            }

            effect.ApplyEffect(target, deltaTime);
        }

        public bool Remove(global::IFieldEffectTarget target, global::IFieldEffect effect, List<global::ActiveEffectData> activeEffects)
        {
            if (target == null || effect == null || activeEffects == null)
                return false;

            bool removed = false;
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i] != null && activeEffects[i].Effect == effect)
                {
                    activeEffects.RemoveAt(i);
                    removed = true;
                }
            }

            if (!removed)
                return false;

            effect.RemoveEffect(target);
            target.OnExitFieldEffect(effect);
            EffectRemoved?.Invoke(target, effect);
            return true;
        }

        public void RemoveAllFromTarget(global::IFieldEffectTarget target, List<global::ActiveEffectData> activeEffects)
        {
            if (target == null || activeEffects == null)
                return;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                global::ActiveEffectData snapshot = activeEffects[i];
                if (snapshot != null)
                    Remove(target, snapshot.Effect, activeEffects);
            }
        }
    }
}
