using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EndlessMode
{
    /// <summary>
    /// Управляет волнами эффектов в бесконечном режиме.
    /// Добавьте компонент на пустой GameObject в игровой сцене или сделайте префаб.
    /// </summary>
    public class EndlessModeManager : MonoBehaviour
    {
        public static EndlessModeManager Instance { get; private set; }

        [Header("Пул эффектов")]
        public List<EffectPoolEntry> effectPool = new List<EffectPoolEntry>();

        [Header("Профиль сложности")]
        public DifficultyProfile difficultyProfile;

        [Header("Длительность")]
        [Tooltip("Секунд активной фазы эффекта")] public float roundDuration = 40f;
        [Tooltip("Пауза перед следующей волной")] public float breakDuration = 10f;

        [Header("Границы генерации точки (позиция по X)")]
        public Vector2 spawnRangeX = new Vector2(-5f, 5f);
        [Header("Границы генерации точки (позиция по Y)")]
        public Vector2 spawnRangeY = new Vector2(-2f, 2f);

        private float _timePlayed;
        private int _roundIndex;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (difficultyProfile == null)
            {
                Debug.LogWarning("EndlessModeManager: DifficultyProfile не назначен — использую значения по умолчанию.");
                difficultyProfile = ScriptableObject.CreateInstance<DifficultyProfile>();
            }

            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            while (true)
            {
                _roundIndex++;
                SpawnRoundEffects();
                yield return new WaitForSeconds(roundDuration);
                yield return new WaitForSeconds(breakDuration);
                _timePlayed += roundDuration + breakDuration;
            }
        }

        private void SpawnRoundEffects()
        {
            float minutes = _timePlayed / 60f;
            int effectsCount = difficultyProfile.EvaluateCount(minutes);
            float strengthMultiplier = difficultyProfile.EvaluateStrength(minutes);

            var candidates = effectPool.Where(e => _roundIndex - e.lastSpawnRound > e.cooldownRounds).ToList();
            if (candidates.Count == 0) return;

            for (int i = 0; i < effectsCount; i++)
            {
                var entry = PickRandomWeighted(candidates);
                if (entry == null) break;
                entry.lastSpawnRound = _roundIndex;
                CreateEffect(entry.effectType, strengthMultiplier);
            }
        }

        private EffectPoolEntry PickRandomWeighted(List<EffectPoolEntry> list)
        {
            float totalWeight = list.Sum(e => e.baseWeight);
            float value = Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var e in list)
            {
                cumulative += e.baseWeight;
                if (value <= cumulative)
                    return e;
            }
            return list[0];
        }

        private void CreateEffect(FieldEffectType type, float strengthMultiplier)
        {
            Vector3 pos = new Vector3(Random.Range(spawnRangeX.x, spawnRangeX.y), Random.Range(spawnRangeY.x, spawnRangeY.y), 0f);
            float baseStrength = 1f;
            float radius = 4f;
            float strength = baseStrength * strengthMultiplier;

            FieldEffectFactory.CreateEffect(type, pos, strength, radius);
        }
    }
} 