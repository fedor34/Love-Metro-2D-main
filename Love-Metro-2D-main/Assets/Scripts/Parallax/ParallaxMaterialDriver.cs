using UnityEngine;
using System.Collections.Generic;

public class ParallaxMaterialDriver : MonoBehaviour
{
    [SerializeField] private float _speedNormDivisor = 25f; // умеренная нормализация
    [SerializeField] private float _speedModScale = 0.6f;   // умеренный масштаб модификатора
    [SerializeField] private float _minSpeedMod = 1.0f;
    [SerializeField] private float _maxSpeedMod = 12f;
    [SerializeField] private bool _logOnce = true;

    private TrainManager _train;
    private readonly List<SpriteRenderer> _targets = new List<SpriteRenderer>();
    private bool _logged;

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
            if (matName.Contains("parallax") || shName.Contains("parallax"))
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
        float speed01 = Mathf.Clamp01(s / Mathf.Max(0.01f, _speedNormDivisor));
        float speedMod = Mathf.Clamp(_minSpeedMod + s * _speedModScale, _minSpeedMod, _maxSpeedMod);
        float t = Time.time;

        for (int i = 0; i < _targets.Count; i++)
        {
            var mat = _targets[i]?.material; // instance
            if (mat == null) continue;

            // время
            SafeSet(mat, "_elapsedTime", t);
            SafeSet(mat, "elapsedTime", t);

            // нормализованная скорость (0..1)
            SafeSet(mat, "_Speed", speed01);
            SafeSet(mat, "Speed", speed01);

            // сильный модификатор скорости
            SafeSet(mat, "_SpeedModificator", speedMod);
            SafeSet(mat, "SpeedModificator", speedMod);

            // Offset не трогаем, чтобы не дублировать движение
        }

        if (_logOnce && !_logged)
        {
            _logged = true;
            Debug.Log($"[ParallaxMaterialDriver] tick: speed={s:F2} speed01={speed01:F2} speedMod={speedMod:F2} targets={_targets.Count}");
        }
    }

    private void SafeSet(Material m, string name, float value)
    {
        if (m.HasFloat(name)) m.SetFloat(name, value);
    }

    
}