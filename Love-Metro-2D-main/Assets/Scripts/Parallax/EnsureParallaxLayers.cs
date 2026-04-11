using System.Collections.Generic;
using UnityEngine;

public class EnsureParallaxLayers : MonoBehaviour
{
    [SerializeField] private string[] _likelyBackgroundNames = new[] { "background", "window", "out", "landscape", "city", "parallax", "bg", "square" };
    [SerializeField] private string[] _likelyBackgroundNamesRu = new[]
    {
        "\u0433\u043e\u0440\u043e\u0434",
        "\u0444\u043e\u043d",
        "\u043e\u043a\u043d\u043e",
        "\u043f\u0435\u0439\u0437\u0430\u0436",
        "\u0434\u0430\u043b\u044c\u043d\u0438\u0439",
        "\u043d\u0435\u0431\u043e",
        "\u0443\u043b\u0438\u0446",
        "\u0437\u0430\u0434\u043d",
        "\u043a\u0432\u0430\u0434\u0440\u0430\u0442"
    };
    [SerializeField] private string _backgroundRootName = "Background";
    [SerializeField] private string[] _sortingLayerHints = new[] { "background", "back", "\u0444\u043e\u043d" };

    [SerializeField] private float _parallaxSpeed = 6f;
    [SerializeField] private bool _scrollByTransform = true;
    [SerializeField] private float _transformScrollScale = 6f;
    [SerializeField] private bool _verboseLogging;

    private void Start()
    {
        ConfigureParallaxLayers();
    }

    private void ConfigureParallaxLayers()
    {
        HashSet<SpriteRenderer> candidates = CollectCandidateRenderers();
        int configured = 0;

        foreach (SpriteRenderer renderer in candidates)
        {
            if (TryConfigureRenderer(renderer))
                configured++;
        }

        Diagnostics.Log($"[EnsureParallaxLayers] Configured {configured} background sprite(s) for parallax.");
    }

    private HashSet<SpriteRenderer> CollectCandidateRenderers()
    {
        var candidates = new HashSet<SpriteRenderer>();
        AddBackgroundRootChildren(candidates);

        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (ShouldConfigureRenderer(renderer))
                candidates.Add(renderer);
        }

        return candidates;
    }

    private void AddBackgroundRootChildren(HashSet<SpriteRenderer> candidates)
    {
        if (candidates == null || string.IsNullOrWhiteSpace(_backgroundRootName))
            return;

        GameObject backgroundRoot = GameObject.Find(_backgroundRootName);
        if (backgroundRoot == null)
            return;

        SpriteRenderer[] rootRenderers = backgroundRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < rootRenderers.Length; i++)
            candidates.Add(rootRenderers[i]);
    }

    private bool ShouldConfigureRenderer(SpriteRenderer renderer)
    {
        return ParallaxRendererClassifier.IsLikelyBackgroundRenderer(
            renderer,
            _likelyBackgroundNames,
            _likelyBackgroundNamesRu,
            _sortingLayerHints);
    }

    private bool TryConfigureRenderer(SpriteRenderer renderer)
    {
        if (renderer == null)
            return false;

        renderer.gameObject.isStatic = false;

        ParallaxLayer layer = renderer.GetComponent<ParallaxLayer>();
        if (layer == null)
            layer = renderer.gameObject.AddComponent<ParallaxLayer>();

        layer.SetParallaxSpeed(_parallaxSpeed);
        layer.SetScrollByTransform(_scrollByTransform);
        layer.SetTransformScrollScale(_transformScrollScale);

        if (_verboseLogging)
        {
            Diagnostics.Log(
                $"[EnsureParallaxLayers] Attached ParallaxLayer to '{renderer.gameObject.name}' " +
                $"(mat='{renderer.sharedMaterial?.name}', shader='{renderer.sharedMaterial?.shader?.name}').");
        }

        return true;
    }
}
