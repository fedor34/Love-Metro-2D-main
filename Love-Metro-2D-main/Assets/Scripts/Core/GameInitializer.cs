using UnityEngine;

/// <summary>
/// Автоматически инициализирует ключевые системы игры при запуске сцены
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Auto-Create Settings")]
    [SerializeField] private bool _createClickDirectionManager = true;
    [SerializeField] private bool _createInertiaArrowHUD = true;
    [SerializeField] private bool _ensureParallaxSystems = false; // выключаем старый автопараллакс
    [SerializeField] private bool _ensureBackgroundScroller = false;
    [SerializeField] private bool _ensureParallaxMaterialDriver = true;
    [SerializeField] private bool _replaceParallaxMaterialsWithDefault = false; // если true — насильно отключим шейдерный параллакс
    
    void Awake()
    {
        InitializeCore();
    }
    
    void Start()
    {
        InitializeUI();
    }
    
    private void InitializeCore()
    {
        if (_createClickDirectionManager && FindObjectOfType<ClickDirectionManager>() == null)
        {
            GameObject clickManager = new GameObject("ClickDirectionManager", typeof(ClickDirectionManager));
            DontDestroyOnLoad(clickManager);
            Debug.Log("[GameInitializer] Created ClickDirectionManager");
        }

        if (_replaceParallaxMaterialsWithDefault && FindObjectOfType<BackgroundMaterialOverride>() == null)
        {
            new GameObject("BackgroundMaterialOverride", typeof(BackgroundMaterialOverride));
            Debug.Log("[GameInitializer] Created BackgroundMaterialOverride");
        }

        if (_ensureBackgroundScroller && FindObjectOfType<SimpleBackgroundScroller>() == null)
        {
            var go = new GameObject("SimpleBackgroundScroller", typeof(SimpleBackgroundScroller));
            var scroller = go.GetComponent<SimpleBackgroundScroller>();

            var names = new string[] { "6_город_фон", "5_город_дальний", "4_город_средний", "3_город_ближний", "2_город_деревья", "1_город_рельсы", "Square" };
            var speeds = new float[] { 0.3f, 0.5f, 0.8f, 1.0f, 1.2f, 1.5f, 1.0f };
            var layers = new System.Collections.Generic.List<SimpleBackgroundScroller.Layer>();
            for (int i = 0; i < names.Length; i++)
            {
                var obj = GameObject.Find(names[i]);
                if (obj == null) continue;
                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                layers.Add(new SimpleBackgroundScroller.Layer { renderer = sr, speedFactor = i < speeds.Length ? speeds[i] : 1f });
                obj.isStatic = false;
            }
            var so = new SimpleBackgroundScroller.Layer[layers.Count];
            for (int i = 0; i < layers.Count; i++) so[i] = layers[i];
            var fld = typeof(SimpleBackgroundScroller).GetField("_layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fld?.SetValue(scroller, so);

            Debug.Log($"[GameInitializer] Created SimpleBackgroundScroller with {layers.Count} layer(s)");
        }

        if (_ensureParallaxSystems && FindObjectOfType<EnsureParallaxLayers>() == null)
        {
            new GameObject("EnsureParallaxLayers_Auto", typeof(EnsureParallaxLayers));
            Debug.Log("[GameInitializer] Created EnsureParallaxLayers");
        }

        if (_ensureParallaxMaterialDriver && FindObjectOfType<ParallaxMaterialDriver>() == null)
        {
            new GameObject("ParallaxMaterialDriver", typeof(ParallaxMaterialDriver));
            Debug.Log("[GameInitializer] Created ParallaxMaterialDriver");
        }
    }
    
    private void InitializeUI()
    {
        if (_createInertiaArrowHUD && FindObjectOfType<InertiaArrowHUD>() == null)
        {
            GameObject arrowHUD = new GameObject("InertiaArrowHUD", typeof(InertiaArrowHUD));
            Debug.Log("[GameInitializer] Created InertiaArrowHUD");
        }
    }
}
