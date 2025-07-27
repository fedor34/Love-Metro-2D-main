using UnityEngine;

namespace EndlessMode
{
    /// <summary>
    /// Скриптованная кривая роста сложности бесконечного режима.
    /// </summary>
    [CreateAssetMenu(menuName = "Endless/Difficulty Profile", fileName = "DifficultyProfile")]
    public class DifficultyProfile : ScriptableObject
    {
        [Header("Усиление эффектов (в зависимости от минут игры)")]
        public AnimationCurve strengthMultiplier = AnimationCurve.Linear(0, 1, 10, 3);

        [Header("Сколько эффектов спавнить за раунд (минуты -> кол-во)")]
        public AnimationCurve effectsPerRound = AnimationCurve.Linear(0, 1, 10, 3);

        public float EvaluateStrength(float minutesPlayed) => strengthMultiplier.Evaluate(minutesPlayed);
        public int EvaluateCount(float minutesPlayed)
        {
            return Mathf.Max(1, Mathf.RoundToInt(effectsPerRound.Evaluate(minutesPlayed)));
        }
    }
} 