using System;
using System.Collections.Generic;

namespace LoveMetro.FieldEffects
{
    /// <summary>
    /// Stores active field effects indexed by category and type. Pure POCO; no Unity dependencies
    /// so it can be unit-tested directly.
    /// </summary>
    public sealed class FieldEffectRegistry
    {
        private readonly Dictionary<global::FieldEffectCategory, List<global::IFieldEffect>> _byCategory
            = new Dictionary<global::FieldEffectCategory, List<global::IFieldEffect>>();
        private readonly Dictionary<global::FieldEffectType, List<global::IFieldEffect>> _byType
            = new Dictionary<global::FieldEffectType, List<global::IFieldEffect>>();

        public FieldEffectRegistry()
        {
            foreach (global::FieldEffectCategory category in Enum.GetValues(typeof(global::FieldEffectCategory)))
                _byCategory[category] = new List<global::IFieldEffect>();

            foreach (global::FieldEffectType type in Enum.GetValues(typeof(global::FieldEffectType)))
                _byType[type] = new List<global::IFieldEffect>();
        }

        public IReadOnlyDictionary<global::FieldEffectCategory, List<global::IFieldEffect>> EffectsByCategory => _byCategory;

        public bool TryRegister(global::IFieldEffect effect, out global::FieldEffectData data, out global::FieldEffectCategory category)
        {
            if (!TryGetMetadata(effect, out data, out category))
                return false;

            List<global::IFieldEffect> categoryEffects = _byCategory[category];
            if (categoryEffects.Contains(effect))
                return false;

            categoryEffects.Add(effect);
            _byType[data.effectType].Add(effect);
            return true;
        }

        public bool TryUnregister(global::IFieldEffect effect, out global::FieldEffectData data, out global::FieldEffectCategory category)
        {
            if (!TryGetMetadata(effect, out data, out category))
                return false;

            bool removed = _byCategory[category].Remove(effect);
            removed |= _byType[data.effectType].Remove(effect);
            return removed;
        }

        public List<global::IFieldEffect> GetEffectsByCategory(global::FieldEffectCategory category)
        {
            return _byCategory.TryGetValue(category, out List<global::IFieldEffect> effects)
                ? new List<global::IFieldEffect>(effects)
                : new List<global::IFieldEffect>();
        }

        public List<global::IFieldEffect> GetEffectsByType(global::FieldEffectType type)
        {
            return _byType.TryGetValue(type, out List<global::IFieldEffect> effects)
                ? new List<global::IFieldEffect>(effects)
                : new List<global::IFieldEffect>();
        }

        public int GetTotalEffectsCount()
        {
            int count = 0;
            foreach (List<global::IFieldEffect> effects in _byCategory.Values)
                count += effects.Count;
            return count;
        }

        public List<global::IFieldEffect> GetAllEffects()
        {
            var all = new List<global::IFieldEffect>();
            foreach (List<global::IFieldEffect> effects in _byCategory.Values)
                all.AddRange(effects);
            return all;
        }

        public static bool TryGetMetadata(global::IFieldEffect effect, out global::FieldEffectData data, out global::FieldEffectCategory category)
        {
            data = effect?.GetEffectData();
            if (effect == null || data == null)
            {
                category = global::FieldEffectCategory.Other;
                return false;
            }

            category = ClassifyCategory(data.effectType);
            return true;
        }

        public static global::FieldEffectCategory ClassifyCategory(global::FieldEffectType type)
        {
            switch (type)
            {
                case global::FieldEffectType.Gravity:
                case global::FieldEffectType.Repulsion:
                case global::FieldEffectType.Wind:
                case global::FieldEffectType.Magnetic:
                case global::FieldEffectType.Vortex:
                    return global::FieldEffectCategory.Movement;

                case global::FieldEffectType.Slowdown:
                case global::FieldEffectType.Speedup:
                case global::FieldEffectType.Friction:
                case global::FieldEffectType.Bounce:
                    return global::FieldEffectCategory.Modifier;

                case global::FieldEffectType.Teleport:
                case global::FieldEffectType.Checkpoint:
                case global::FieldEffectType.Activator:
                    return global::FieldEffectCategory.Trigger;

                case global::FieldEffectType.Visual:
                    return global::FieldEffectCategory.Visual;

                case global::FieldEffectType.Audio:
                    return global::FieldEffectCategory.Audio;

                default:
                    return global::FieldEffectCategory.Other;
            }
        }
    }
}
