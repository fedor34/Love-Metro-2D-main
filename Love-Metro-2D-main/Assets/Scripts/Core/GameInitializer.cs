using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Ensures core runtime systems exist when a scene starts.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    private static readonly string[] BackgroundLayerNames =
    {
        "6_\u0433\u043e\u0440\u043e\u0434_\u0444\u043e\u043d",
        "5_\u0433\u043e\u0440\u043e\u0434_\u0434\u0430\u043b\u044c\u043d\u0438\u0439",
        "4_\u0433\u043e\u0440\u043e\u0434_\u0441\u0440\u0435\u0434\u043d\u0438\u0439",
        "3_\u0433\u043e\u0440\u043e\u0434_\u0431\u043b\u0438\u0436\u043d\u0438\u0439",
        "2_\u0433\u043e\u0440\u043e\u0434_\u0434\u0435\u0440\u0435\u0432\u044c\u044f",
        "1_\u0433\u043e\u0440\u043e\u0434_\u0440\u0435\u043b\u044c\u0441\u044b",
        "Square"
    };

    private static readonly float[] BackgroundLayerSpeeds = { 0.3f, 0.5f, 0.8f, 1.0f, 1.2f, 1.5f, 1.0f };
    private static FieldInfo _backgroundLayersField;

    [Header("Auto-Create Settings")]
    [SerializeField] private bool _createClickDirectionManager = true;
    [SerializeField] private bool _createInertiaArrowHUD = true;
    [SerializeField] private bool _ensureParallaxSystems = false;
    [SerializeField] private bool _createManualPairingManager = true;
    [SerializeField] private bool _ensureBackgroundScroller = true;
    [SerializeField] private bool _ensureParallaxMaterialDriver = false;
    [SerializeField] private bool _replaceParallaxMaterialsWithDefault = false;

    private void Awake()
    {
        InitializeCoreSystems();
    }

    private void Start()
    {
        InitializeUiSystems();
    }

    private void InitializeCoreSystems()
    {
        EnsureComponent<ClickDirectionManager>(_createClickDirectionManager, "ClickDirectionManager", persistent: true);
        EnsureComponent<ManualPairingManager>(_createManualPairingManager, "ManualPairingManager", persistent: true);
        EnsureComponent<BackgroundMaterialOverride>(_replaceParallaxMaterialsWithDefault, "BackgroundMaterialOverride");
        EnsureBackgroundScroller();
        EnsureComponent<EnsureParallaxLayers>(_ensureParallaxSystems, "EnsureParallaxLayers_Auto");
        EnsureComponent<ParallaxMaterialDriver>(_ensureParallaxMaterialDriver, "ParallaxMaterialDriver");
    }

    private void InitializeUiSystems()
    {
        EnsureComponent<InertiaArrowHUD>(_createInertiaArrowHUD, "InertiaArrowHUD");
    }

    private T EnsureComponent<T>(bool shouldCreate, string objectName, bool persistent = false) where T : Component
    {
        if (!shouldCreate)
            return null;

        T existing = FindObjectOfType<T>();
        if (existing != null)
            return existing;

        GameObject gameObject = new GameObject(string.IsNullOrWhiteSpace(objectName) ? typeof(T).Name : objectName, typeof(T));
        if (persistent)
            DontDestroyOnLoad(gameObject);

        Diagnostics.Log($"[GameInitializer] Created {typeof(T).Name}.");
        return gameObject.GetComponent<T>();
    }

    private void EnsureBackgroundScroller()
    {
        if (!_ensureBackgroundScroller || FindObjectOfType<SimpleBackgroundScroller>() != null)
            return;

        GameObject scrollerObject = new GameObject("SimpleBackgroundScroller", typeof(SimpleBackgroundScroller));
        SimpleBackgroundScroller scroller = scrollerObject.GetComponent<SimpleBackgroundScroller>();
        int layerCount = ConfigureBackgroundScroller(scroller);
        Diagnostics.Log($"[GameInitializer] Created SimpleBackgroundScroller with {layerCount} layer(s).");
    }

    private int ConfigureBackgroundScroller(SimpleBackgroundScroller scroller)
    {
        if (scroller == null)
            return 0;

        FieldInfo layersField = GetBackgroundLayersField();
        if (layersField == null)
        {
            Diagnostics.Warn("[GameInitializer] SimpleBackgroundScroller._layers field not found.");
            return 0;
        }

        List<SimpleBackgroundScroller.Layer> configuredLayers = BuildBackgroundLayers();
        layersField.SetValue(scroller, configuredLayers.ToArray());
        return configuredLayers.Count;
    }

    private List<SimpleBackgroundScroller.Layer> BuildBackgroundLayers()
    {
        var layers = new List<SimpleBackgroundScroller.Layer>(BackgroundLayerNames.Length);

        for (int i = 0; i < BackgroundLayerNames.Length; i++)
        {
            SpriteRenderer renderer = ResolveBackgroundRenderer(BackgroundLayerNames[i]);
            if (renderer == null)
                continue;

            layers.Add(new SimpleBackgroundScroller.Layer
            {
                renderer = renderer,
                speedFactor = i < BackgroundLayerSpeeds.Length ? BackgroundLayerSpeeds[i] : 1f
            });

            renderer.gameObject.isStatic = false;
        }

        return layers;
    }

    private static SpriteRenderer ResolveBackgroundRenderer(string objectName)
    {
        // GameObject.Find не находит неактивные объекты (Background выключен в сцене),
        // поэтому ищем через все SpriteRenderer включая inactive.
        SpriteRenderer[] all = FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject.name == objectName)
                return all[i];
        }
        return null;
    }

    private static FieldInfo GetBackgroundLayersField()
    {
        if (_backgroundLayersField == null)
        {
            _backgroundLayersField = typeof(SimpleBackgroundScroller).GetField(
                "_layers",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        return _backgroundLayersField;
    }
}
