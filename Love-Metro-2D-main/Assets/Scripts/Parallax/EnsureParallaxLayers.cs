using UnityEngine;
using System.Collections.Generic;

public class EnsureParallaxLayers : MonoBehaviour
{
    [SerializeField] private string[] _likelyBackgroundNames = new[] { "background", "window", "out", "landscape", "city", "parallax", "bg", "square" };
    [SerializeField] private string[] _likelyBackgroundNamesRu = new[] { "город", "фон", "окно", "пейзаж", "дальний", "небо", "улиц", "задн", "квадрат" };
    [SerializeField] private string _backgroundRootName = "Background"; // родительский узел в сцене, если есть
    [SerializeField] private string[] _sortingLayerHints = new[] { "background", "back", "фон" };

    [SerializeField] private float _parallaxSpeed = 6f;   // умеренная базовая скорость
    [SerializeField] private bool _scrollByTransform = true;
    [SerializeField] private float _transformScrollScale = 6f; // умеренный множитель, избегаем телепортов

    private void Start()
    {
        var toProcess = new HashSet<SpriteRenderer>();

        // 1) По корню Background (если есть)
        var bgRoot = GameObject.Find(_backgroundRootName);
        if (bgRoot != null)
        {
            foreach (var r in bgRoot.GetComponentsInChildren<SpriteRenderer>(true))
                toProcess.Add(r);
        }

        // 2) По ключевым словам в имени объекта (EN + RU)
        foreach (var r in FindObjectsOfType<SpriteRenderer>(true))
        {
            string n = r.gameObject.name.ToLower();
            if (MatchesAny(n, _likelyBackgroundNames) || MatchesAny(n, _likelyBackgroundNamesRu))
                toProcess.Add(r);
        }

        // 3) По названию sorting layer
        foreach (var r in FindObjectsOfType<SpriteRenderer>(true))
        {
            string layer = r.sortingLayerName != null ? r.sortingLayerName.ToLower() : string.Empty;
            if (MatchesAny(layer, _sortingLayerHints))
                toProcess.Add(r);
        }

        // 4) По названию материала/шейдера
        foreach (var r in FindObjectsOfType<SpriteRenderer>(true))
        {
            var mat = r.sharedMaterial;
            string matName = mat != null ? mat.name.ToLower() : string.Empty;
            string shName = (mat != null && mat.shader != null) ? mat.shader.name.ToLower() : string.Empty;
            if (matName.Contains("parallax") || shName.Contains("parallax"))
                toProcess.Add(r);
        }

        int attached = 0;
        foreach (var r in toProcess)
        {
            if (r == null) continue;
            if (r.gameObject.isStatic) r.gameObject.isStatic = false;

            var layer = r.GetComponent<ParallaxLayer>();
            if (layer == null) layer = r.gameObject.AddComponent<ParallaxLayer>();
            layer.SetParallaxSpeed(_parallaxSpeed);
            layer.SetScrollByTransform(_scrollByTransform);
            layer.SetTransformScrollScale(_transformScrollScale);
            attached++;
            Debug.Log($"[EnsureParallaxLayers] Attached ParallaxLayer to '{r.gameObject.name}' (mat='{r.sharedMaterial?.name}', shader='{r.sharedMaterial?.shader?.name}').");
        }

        Debug.Log($"[EnsureParallaxLayers] Configured {attached} background sprite(s) for parallax (VERY FAST). ");
    }

    private bool MatchesAny(string text, string[] keys)
    {
        foreach (var k in keys)
        {
            if (text.Contains(k)) return true;
        }
        return false;
    }
}