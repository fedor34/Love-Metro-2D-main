using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.FieldEffects
{
    /// <summary>
    /// Caches "effects near position" lookups on a per-grid-cell basis to avoid scanning
    /// every registered effect every tick. Cache is flushed at a configurable interval.
    /// </summary>
    public sealed class FieldEffectSpatialQuery
    {
        private readonly Dictionary<Vector3, List<global::IFieldEffect>> _cache = new Dictionary<Vector3, List<global::IFieldEffect>>();
        private readonly float _cacheUpdateInterval;
        private float _cacheUpdateTime;

        public FieldEffectSpatialQuery(float cacheUpdateInterval)
        {
            _cacheUpdateInterval = Mathf.Max(0.01f, cacheUpdateInterval);
        }

        public void Update(float time)
        {
            if (time - _cacheUpdateTime <= _cacheUpdateInterval)
                return;

            _cache.Clear();
            _cacheUpdateTime = time;
        }

        public List<global::IFieldEffect> GetEffectsAtPosition(
            Vector3 position,
            IReadOnlyDictionary<global::FieldEffectCategory, List<global::IFieldEffect>> effectsByCategory)
        {
            Vector3 gridPosition = new Vector3(
                Mathf.Round(position.x),
                Mathf.Round(position.y),
                Mathf.Round(position.z));

            if (_cache.TryGetValue(gridPosition, out List<global::IFieldEffect> cachedEffects))
                return cachedEffects;

            var effects = new List<global::IFieldEffect>();
            foreach (List<global::IFieldEffect> categoryEffects in effectsByCategory.Values)
            {
                if (categoryEffects == null)
                    continue;

                foreach (global::IFieldEffect effect in categoryEffects)
                {
                    if (effect != null && effect.IsInEffectZone(position))
                        effects.Add(effect);
                }
            }

            _cache[gridPosition] = effects;
            return effects;
        }
    }
}
