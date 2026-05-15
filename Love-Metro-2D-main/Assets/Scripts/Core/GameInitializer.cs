using UnityEngine;

/// <summary>
/// Scene-level compatibility facade for runtime composition.
/// </summary>
public class GameInitializer : MonoBehaviour
{
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
        BindScene();
    }

    private void Start()
    {
        BindScene();
    }

    private void BindScene()
    {
        LoveMetro.Core.RuntimeCompositionRoot.BindActiveScene(BuildOptions());
    }

    private LoveMetro.Core.RuntimeCompositionOptions BuildOptions()
    {
        LoveMetro.Core.RuntimeCompositionOptions options = LoveMetro.Core.RuntimeCompositionOptions.GameplayDefaults;
        options.EnsureClickDirectionManager = _createClickDirectionManager;
        options.EnsureManualPairingManager = _createManualPairingManager;
        options.EnsureInertiaArrowHud = _createInertiaArrowHUD;
        options.EnsureParallaxSystems = _ensureParallaxSystems;
        options.EnsureBackgroundScroller = _ensureBackgroundScroller;
        options.EnsureParallaxMaterialDriver = _ensureParallaxMaterialDriver;
        options.EnsureBackgroundMaterialOverride = _replaceParallaxMaterialsWithDefault;
        return options;
    }
}
