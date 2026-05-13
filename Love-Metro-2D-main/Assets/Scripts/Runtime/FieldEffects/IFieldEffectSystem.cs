using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.FieldEffects
{
    public interface IFieldEffectSystem
    {
        void RegisterEffect(global::IFieldEffect effect);
        void UnregisterEffect(global::IFieldEffect effect);
        void RegisterTarget(global::IFieldEffectTarget target);
        void UnregisterTarget(global::IFieldEffectTarget target);
        List<global::IFieldEffect> GetEffectsAtPosition(Vector3 position);
        List<global::IFieldEffect> GetEffectsByCategory(global::FieldEffectCategory category);
        List<global::IFieldEffect> GetEffectsByType(global::FieldEffectType type);
        int GetTotalEffectsCount();
        int GetTargetsCount();
    }
}
