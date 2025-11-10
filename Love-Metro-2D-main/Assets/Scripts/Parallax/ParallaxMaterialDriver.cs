using UnityEngine;
using System.Collections.Generic;

public class ParallaxMaterialDriver : MonoBehaviour
{
    [Header("Speed coupling")]
    [SerializeField] private float _maxTrainSpeed = 480f;   // синхронизировано с новым максимумом поезда
    [SerializeField] private float _offsetScale = 0.8f;     // множитель сдвига на единицу норм. скорости
    [SerializeField] private float _elapsedTimeScale = 1.0f; // базовая скорость времени для шейдера
    [SerializeField] private bool _logOnce = true;

    [SerializeField] private float _baseSpeedMod = 1.4f;   // базовый множитель скорости в шейдере

    private TrainManager _train;
    private readonly List<SpriteRenderer> _targets = new List<SpriteRenderer>();
    private bool _logged;
    private float _parallaxOffset = 0f;

    private void Awake()
    {
        _train = FindObjectOfType<TrainManager>();
        CacheTargets();
        DisableParallaxLayerOnTargets();
    }

    private void CacheTargets()
    {
        _targets.Clear();
        foreach (var r in FindObjectsOfType<SpriteRenderer>(true))
        {
            var mat = r.sharedMaterial;
            string matName = mat != null ? mat.name.ToLower() : string.Empty;
            string shName = (mat != null && mat.shader != null) ? mat.shader.name.ToLower() : string.Empty;
            bool looksParallaxByName = matName.Contains("parallax") || shName.Contains("parallax");
            bool hasProps = false;
            if (mat != null)
            {
                try
                {
                    hasProps = mat.HasProperty("_elapsedTime") || mat.HasProperty("elapsedTime") ||
                               mat.HasProperty("_Speed") || mat.HasProperty("Speed");
                }
                catch { }
            }

            if (looksParallaxByName || hasProps)
            {
                _targets.Add(r);
                r.gameObject.isStatic = false;
            }
        }
        Debug.Log($"[ParallaxMaterialDriver] Found {_targets.Count} parallax materials");
    }

    private void DisableParallaxLayerOnTargets()
    {
        foreach (var r in _targets)
        {
            var pl = r.GetComponent<ParallaxLayer>();
            if (pl != null) pl.enabled = false;
        }
    }

    private void Update()
    {
        if (_train == null || _targets.Count == 0) return;
        float s = Mathf.Abs(_train.GetCurrentSpeed());
        bool held = ClickDirectionManager.IsMouseHeld;
        float speed01 = held ? Mathf.Clamp01(s / Mathf.Max(0.01f, _maxTrainSpeed)) : 0f;

        // Время должно всегда идти линейно — шейдер уже умножает его на скорость
        _parallaxOffset += _elapsedTimeScale * Time.deltaTime;

        for (int i = 0; i < _targets.Count; i++)
        {
            var mat = _targets[i]?.material;
            if (mat == null) continue;

            SafeSet(mat, "_elapsedTime", _parallaxOffset);
            SafeSet(mat, "elapsedTime", _parallaxOffset);

            // Передаём только нормированную скорость
            SafeSet(mat, "_Speed", speed01);
            SafeSet(mat, "Speed", speed01);

            // Базовый множитель скорости (не зависит от поезда)
            SafeSet(mat, "_SpeedModificator", _baseSpeedMod);
            SafeSet(mat, "SpeedModificator", _baseSpeedMod);
        }

        if (_logOnce && !_logged)
        {
            _logged = true;
            Debug.Log($"[ParallaxMaterialDriver] tick: speed={s:F2} speed01={speed01:F2} offset(time)={_parallaxOffset:F2} targets={_targets.Count}");
        }
    }

    private void SafeSet(Material m, string name, float value)
    {
        if (m.HasFloat(name)) m.SetFloat(name, value);
    }

    
}
