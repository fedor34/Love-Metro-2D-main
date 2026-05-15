using System.Collections.Generic;
using LoveMetro.Input;
using LoveMetro.Train;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LoveMetro.Core
{
    public struct RuntimeCompositionOptions
    {
        public bool EnsureClickDirectionManager;
        public bool EnsureManualPairingManager;
        public bool EnsureInertiaArrowHud;
        public bool EnsureParallaxSystems;
        public bool EnsureBackgroundScroller;
        public bool EnsureParallaxMaterialDriver;
        public bool EnsureBackgroundMaterialOverride;

        public static RuntimeCompositionOptions CoreDefaults => new RuntimeCompositionOptions
        {
            EnsureClickDirectionManager = true,
            EnsureManualPairingManager = true,
            EnsureInertiaArrowHud = false,
            EnsureParallaxSystems = false,
            EnsureBackgroundScroller = false,
            EnsureParallaxMaterialDriver = false,
            EnsureBackgroundMaterialOverride = false
        };

        public static RuntimeCompositionOptions GameplayDefaults => new RuntimeCompositionOptions
        {
            EnsureClickDirectionManager = true,
            EnsureManualPairingManager = true,
            EnsureInertiaArrowHud = true,
            EnsureParallaxSystems = false,
            EnsureBackgroundScroller = true,
            EnsureParallaxMaterialDriver = false,
            EnsureBackgroundMaterialOverride = false
        };
    }

    public sealed class RuntimeCompositionRoot : MonoBehaviour
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

        public static RuntimeCompositionRoot Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            Ensure();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BindAfterSceneLoad()
        {
            BindActiveScene(RuntimeCompositionOptions.CoreDefaults);
        }

        public static RuntimeCompositionRoot Ensure()
        {
            if (Instance != null)
                return Instance;

            RuntimeCompositionRoot existing = Object.FindObjectOfType<RuntimeCompositionRoot>();
            if (existing != null)
            {
                Instance = existing;
                MarkPersistent(existing.gameObject);
                return existing;
            }

            GameObject rootObject = new GameObject("[RuntimeCompositionRoot]");
            RuntimeCompositionRoot root = rootObject.AddComponent<RuntimeCompositionRoot>();
            MarkPersistent(rootObject);
            return root;
        }

        public static SceneObjectIndex BindActiveScene()
        {
            return BindActiveScene(RuntimeCompositionOptions.CoreDefaults);
        }

        public static SceneObjectIndex BindActiveScene(RuntimeCompositionOptions options)
        {
            return Ensure().Bind(options);
        }

        public static void CleanupAfterSceneChange()
        {
            Couple.ClearCache();
            RuntimeServices.Instance.PassengerRegistry?.CleanupNullReferences();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                UnityLifecycle.SafeDestroy(gameObject);
                return;
            }

            Instance = this;
            MarkPersistent(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private SceneObjectIndex Bind(RuntimeCompositionOptions options)
        {
            SceneObjectIndex index = SceneObjectIndex.CaptureActiveScene();
            EnsureCoreComponents(index, options);
            RegisterServices(index);
            ConfigureScene(index);
            return index;
        }

        private void EnsureCoreComponents(SceneObjectIndex index, RuntimeCompositionOptions options)
        {
            index.PassengerRegistry = EnsurePersistentComponent(index.PassengerRegistry, "[PassengerRegistry]");
            index.CouplesManager = EnsurePersistentComponent(index.CouplesManager, "[CouplesManager]");
            index.FieldEffectSystem = EnsurePersistentComponent(index.FieldEffectSystem, "[FieldEffectSystem]");

            if (options.EnsureClickDirectionManager)
                index.ClickDirectionManager = EnsureSceneComponent(index.ClickDirectionManager, "ClickDirectionManager", persistent: true);

            if (options.EnsureManualPairingManager)
                index.ManualPairingManager = EnsureSceneComponent(index.ManualPairingManager, "ManualPairingManager", persistent: true);

            if (options.EnsureInertiaArrowHud)
                index.InertiaArrowHud = EnsureSceneComponent(index.InertiaArrowHud, "InertiaArrowHUD", persistent: false);

            if (options.EnsureParallaxSystems)
                index.EnsureParallaxLayers = EnsureSceneComponent(index.EnsureParallaxLayers, "EnsureParallaxLayers_Auto", persistent: false);

            if (options.EnsureParallaxMaterialDriver)
                index.ParallaxMaterialDriver = EnsureSceneComponent(index.ParallaxMaterialDriver, "ParallaxMaterialDriver", persistent: false);

            if (options.EnsureBackgroundMaterialOverride)
                index.BackgroundMaterialOverride = EnsureSceneComponent(index.BackgroundMaterialOverride, "BackgroundMaterialOverride", persistent: false);

            EnsureBackgroundScroller(index, options);
            EnsureEventSystem();
        }

        private void RegisterServices(SceneObjectIndex index)
        {
            RuntimeServices services = RuntimeServices.Instance;

            services.RegisterPassengerRegistry(index.PassengerRegistry);
            services.RegisterFieldEffectSystem(index.FieldEffectSystem);
            services.RegisterInputIntentProvider(index.ClickDirectionManager);
            services.RegisterManualPairingService(index.ManualPairingManager);

            if (index.ScoreCounter != null)
                services.RegisterScoreService(index.ScoreCounter);

            if (index.TrainManager != null)
            {
                services.RegisterTrainMotionEvents(index.TrainManager);
                services.RegisterStationFlowService(index.TrainManager);
            }
        }

        private void ConfigureScene(SceneObjectIndex index)
        {
            ITrainMotionEvents trainEvents = index.TrainManager;
            IStationFlowService stationFlow = index.TrainManager;

            index.TrainManager?.Configure(index.PassangerSpawner, index.ParallaxEffect, index.PassangersContainer);
            index.ParallaxEffect?.Configure(trainEvents, index.ParallaxLayers);
            index.SimpleBackgroundScroller?.ConfigureTrain(trainEvents);
            index.BackgroundGroupScroller?.Configure(
                trainEvents,
                index.ResolveTransformByName("Background"),
                index.ResolveTransformsByName(BackgroundLayerNames));
            index.ParallaxMaterialDriver?.Configure(trainEvents, index.SpriteRenderers);
            index.BackgroundMaterialOverride?.Configure(index.SpriteRenderers);
            index.ScoreCounter?.ConfigureTrainEvents(trainEvents);
            index.CouplesManager?.Configure(
                index.PassengerRegistry,
                trainEvents,
                stationFlow,
                RuntimeServices.Instance.ScoreService);
            index.FieldEffectSystem?.RegisterSceneComponents(index.MonoBehaviours);
        }

        private void EnsureBackgroundScroller(SceneObjectIndex index, RuntimeCompositionOptions options)
        {
            if (!options.EnsureBackgroundScroller)
                return;

            index.SimpleBackgroundScroller = EnsureSceneComponent(index.SimpleBackgroundScroller, "SimpleBackgroundScroller", persistent: false);
            int layerCount = ConfigureBackgroundScroller(index.SimpleBackgroundScroller, index);
            Diagnostics.Log($"[RuntimeCompositionRoot] Configured SimpleBackgroundScroller with {layerCount} layer(s).");
        }

        private static int ConfigureBackgroundScroller(SimpleBackgroundScroller scroller, SceneObjectIndex index)
        {
            if (scroller == null || index == null)
                return 0;

            var configuredLayers = new List<SimpleBackgroundScroller.Layer>(BackgroundLayerNames.Length);
            for (int i = 0; i < BackgroundLayerNames.Length; i++)
            {
                SpriteRenderer renderer = index.ResolveSpriteRendererByObjectName(BackgroundLayerNames[i]);
                if (renderer == null)
                    continue;

                configuredLayers.Add(new SimpleBackgroundScroller.Layer
                {
                    renderer = renderer,
                    speedFactor = i < BackgroundLayerSpeeds.Length ? BackgroundLayerSpeeds[i] : 1f
                });

                renderer.gameObject.isStatic = false;
            }

            scroller.ConfigureLayers(configuredLayers.ToArray());
            return configuredLayers.Count;
        }

        private static T EnsurePersistentComponent<T>(T existing, string objectName) where T : Component
        {
            if (existing != null)
            {
                MarkPersistent(existing.transform.root.gameObject);
                return existing;
            }

            GameObject gameObject = new GameObject(objectName, typeof(T));
            MarkPersistent(gameObject);
            Diagnostics.Log($"[RuntimeCompositionRoot] Created {typeof(T).Name}.");
            return gameObject.GetComponent<T>();
        }

        private static T EnsureSceneComponent<T>(T existing, string objectName, bool persistent) where T : Component
        {
            if (existing != null)
            {
                if (persistent)
                    MarkPersistent(existing.transform.root.gameObject);
                return existing;
            }

            GameObject gameObject = new GameObject(string.IsNullOrWhiteSpace(objectName) ? typeof(T).Name : objectName, typeof(T));
            if (persistent)
                MarkPersistent(gameObject);

            Diagnostics.Log($"[RuntimeCompositionRoot] Created {typeof(T).Name}.");
            return gameObject.GetComponent<T>();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            MarkPersistent(eventSystem);
        }

        private static void MarkPersistent(GameObject gameObject)
        {
            if (Application.isPlaying && gameObject != null)
                DontDestroyOnLoad(gameObject);
        }
    }
}
