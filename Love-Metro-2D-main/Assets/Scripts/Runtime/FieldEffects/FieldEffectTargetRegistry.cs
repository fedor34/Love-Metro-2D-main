using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.FieldEffects
{
    /// <summary>
    /// Tracks field-effect targets and the active effects currently applied to each.
    /// POCO; tied to Unity only via UnityEngine.Vector3 returned by the target position probe.
    /// </summary>
    public sealed class FieldEffectTargetRegistry
    {
        private readonly List<global::IFieldEffectTarget> _allTargets = new List<global::IFieldEffectTarget>();
        private readonly Dictionary<global::IFieldEffectTarget, List<global::ActiveEffectData>> _activeEffectsPerTarget
            = new Dictionary<global::IFieldEffectTarget, List<global::ActiveEffectData>>();

        public IReadOnlyList<global::IFieldEffectTarget> AllTargets => _allTargets;
        public int Count => _allTargets.Count;

        public bool Register(global::IFieldEffectTarget target)
        {
            if (target == null || _allTargets.Contains(target))
                return false;

            _allTargets.Add(target);
            GetOrCreateActiveEffects(target);
            return true;
        }

        public List<global::ActiveEffectData> GetOrCreateActiveEffects(global::IFieldEffectTarget target)
        {
            if (target == null)
                return null;

            if (!_activeEffectsPerTarget.TryGetValue(target, out List<global::ActiveEffectData> activeEffects))
            {
                activeEffects = new List<global::ActiveEffectData>();
                _activeEffectsPerTarget[target] = activeEffects;
            }

            return activeEffects;
        }

        public bool TryGetActiveEffects(global::IFieldEffectTarget target, out List<global::ActiveEffectData> activeEffects)
        {
            return _activeEffectsPerTarget.TryGetValue(target, out activeEffects);
        }

        public List<global::IFieldEffectTarget> SnapshotTargets()
        {
            return new List<global::IFieldEffectTarget>(_activeEffectsPerTarget.Keys);
        }

        public void RemoveTarget(global::IFieldEffectTarget target)
        {
            if (target == null)
                return;

            _allTargets.Remove(target);
            _activeEffectsPerTarget.Remove(target);
        }

        public void RemoveTargetAt(int index, global::IFieldEffectTarget target)
        {
            if (index >= 0 && index < _allTargets.Count)
                _allTargets.RemoveAt(index);

            if (target != null)
                _activeEffectsPerTarget.Remove(target);
        }

        public static bool TryGetTargetPosition(global::IFieldEffectTarget target, out Vector3 position)
        {
            try
            {
                position = target.GetPosition();
                return true;
            }
            catch (System.Exception)
            {
                position = Vector3.zero;
                return false;
            }
        }

        public static bool HasActiveEffect(List<global::ActiveEffectData> activeEffects, global::IFieldEffect effect)
        {
            if (activeEffects == null || effect == null)
                return false;

            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i] != null && activeEffects[i].Effect == effect)
                    return true;
            }

            return false;
        }
    }
}
