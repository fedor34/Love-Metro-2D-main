using System;
using UnityEngine;

namespace EndlessMode
{
    /// <summary>
    /// Описывает эффект, который может быть случайно сгенерирован в бесконечном режиме.
    /// </summary>
    [Serializable]
    public class EffectPoolEntry
    {
        public FieldEffectType effectType = FieldEffectType.Gravity;
        [Tooltip("Базовый вес — чем больше, тем выше шанс появления.")]
        public float baseWeight = 1f;
        [Tooltip("Количество раундов, которые должны пройти, прежде чем эффект может появиться снова.")]
        public int cooldownRounds = 0;

        [HideInInspector] public int lastSpawnRound = -999;
    }
} 