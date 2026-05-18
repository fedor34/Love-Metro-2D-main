using UnityEngine;

namespace LoveMetro.Core
{
    public sealed class SceneObjectIndex
    {
        public PassengerRegistry PassengerRegistry { get; internal set; }
        public CouplesManager CouplesManager { get; internal set; }
        public FieldEffectSystem FieldEffectSystem { get; internal set; }
        public ClickDirectionManager ClickDirectionManager { get; internal set; }
        public ManualPairingManager ManualPairingManager { get; internal set; }
        public TrainManager TrainManager { get; internal set; }
        public PassangerSpawner PassangerSpawner { get; internal set; }
        public PassangersContainer PassangersContainer { get; internal set; }
        public ParallaxEffect ParallaxEffect { get; internal set; }
        public SimpleBackgroundScroller SimpleBackgroundScroller { get; internal set; }
        public BackgroundGroupScroller BackgroundGroupScroller { get; internal set; }
        public ParallaxMaterialDriver ParallaxMaterialDriver { get; internal set; }
        public BackgroundMaterialOverride BackgroundMaterialOverride { get; internal set; }
        public EnsureParallaxLayers EnsureParallaxLayers { get; internal set; }
        public InertiaArrowHUD InertiaArrowHud { get; internal set; }
        public ScoreCounter ScoreCounter { get; internal set; }
        public SpriteRenderer[] SpriteRenderers { get; internal set; }
        public ParallaxLayer[] ParallaxLayers { get; internal set; }
        public MonoBehaviour[] MonoBehaviours { get; internal set; }
        public Transform[] Transforms { get; internal set; }

        public static SceneObjectIndex CaptureActiveScene()
        {
            return CaptureActiveScene(RuntimeCompositionOptions.CoreDefaults);
        }

        public static SceneObjectIndex CaptureActiveScene(RuntimeCompositionOptions options)
        {
            RuntimeServices services = RuntimeServices.Instance;
            SceneObjectIndex index = new SceneObjectIndex
            {
                PassengerRegistry = ResolveService<PassengerRegistry>(services.PassengerRegistry) ?? FindFirst<PassengerRegistry>(),
                CouplesManager = FindFirst<CouplesManager>(),
                FieldEffectSystem = ResolveService<FieldEffectSystem>(services.FieldEffectSystem) ?? FindFirst<FieldEffectSystem>(),
                ClickDirectionManager = ResolveService<ClickDirectionManager>(services.InputIntentProvider) ?? FindFirst<ClickDirectionManager>(),
                ManualPairingManager = ResolveService<ManualPairingManager>(services.ManualPairingService) ?? FindFirst<ManualPairingManager>(),
                TrainManager = ResolveService<TrainManager>(services.TrainMotionEvents) ??
                    ResolveService<TrainManager>(services.StationFlowService) ??
                    FindFirst<TrainManager>(),
                PassangerSpawner = FindFirst<PassangerSpawner>(),
                PassangersContainer = FindFirst<PassangersContainer>(),
                ParallaxEffect = FindFirst<ParallaxEffect>(),
                SimpleBackgroundScroller = FindFirst<SimpleBackgroundScroller>(),
                BackgroundGroupScroller = FindFirst<BackgroundGroupScroller>(),
                ParallaxMaterialDriver = FindFirst<ParallaxMaterialDriver>(),
                BackgroundMaterialOverride = FindFirst<BackgroundMaterialOverride>(),
                EnsureParallaxLayers = FindFirst<EnsureParallaxLayers>(),
                InertiaArrowHud = FindFirst<InertiaArrowHUD>(),
                ScoreCounter = ResolveService<ScoreCounter>(services.ScoreService) ?? FindFirst<ScoreCounter>()
            };

            index.CaptureHeavyArrays(options);
            return index;
        }

        internal void CaptureHeavyArrays(RuntimeCompositionOptions options)
        {
            if (NeedsSpriteRenderers(options))
                SpriteRenderers = FindAll<SpriteRenderer>();

            if (ParallaxEffect != null)
                ParallaxLayers = FindAll<ParallaxLayer>();

            if (options.EnsureBackgroundScroller && BackgroundGroupScroller != null)
                Transforms = FindAll<Transform>();
        }

        public Transform ResolveTransformByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName) || Transforms == null)
                return null;

            for (int i = 0; i < Transforms.Length; i++)
            {
                Transform candidate = Transforms[i];
                if (candidate != null && candidate.name == objectName)
                    return candidate;
            }

            return null;
        }

        public Transform[] ResolveTransformsByName(string[] objectNames)
        {
            if (objectNames == null || objectNames.Length == 0)
                return System.Array.Empty<Transform>();

            Transform[] transforms = new Transform[objectNames.Length];
            for (int i = 0; i < objectNames.Length; i++)
                transforms[i] = ResolveTransformByName(objectNames[i]);

            return transforms;
        }

        public SpriteRenderer ResolveSpriteRendererByObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName) || SpriteRenderers == null)
                return null;

            for (int i = 0; i < SpriteRenderers.Length; i++)
            {
                SpriteRenderer renderer = SpriteRenderers[i];
                if (renderer != null && renderer.gameObject.name == objectName)
                    return renderer;
            }

            return null;
        }

        private static bool NeedsSpriteRenderers(RuntimeCompositionOptions options)
        {
            return options.EnsureBackgroundScroller ||
                options.EnsureParallaxMaterialDriver ||
                options.EnsureBackgroundMaterialOverride;
        }

        private static T FindFirst<T>() where T : Object
        {
            T[] objects = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return objects.Length > 0 ? objects[0] : null;
        }

        private static T ResolveService<T>(object service) where T : Object
        {
            T component = service as T;
            return component != null ? component : null;
        }

        private static T[] FindAll<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }
}
