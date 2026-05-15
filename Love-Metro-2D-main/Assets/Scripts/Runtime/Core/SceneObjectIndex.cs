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
            return new SceneObjectIndex
            {
                PassengerRegistry = Object.FindObjectOfType<PassengerRegistry>(),
                CouplesManager = Object.FindObjectOfType<CouplesManager>(),
                FieldEffectSystem = Object.FindObjectOfType<FieldEffectSystem>(),
                ClickDirectionManager = Object.FindObjectOfType<ClickDirectionManager>(),
                ManualPairingManager = Object.FindObjectOfType<ManualPairingManager>(),
                TrainManager = Object.FindObjectOfType<TrainManager>(),
                PassangerSpawner = Object.FindObjectOfType<PassangerSpawner>(),
                PassangersContainer = Object.FindObjectOfType<PassangersContainer>(),
                ParallaxEffect = Object.FindObjectOfType<ParallaxEffect>(),
                SimpleBackgroundScroller = Object.FindObjectOfType<SimpleBackgroundScroller>(),
                BackgroundGroupScroller = Object.FindObjectOfType<BackgroundGroupScroller>(),
                ParallaxMaterialDriver = Object.FindObjectOfType<ParallaxMaterialDriver>(),
                BackgroundMaterialOverride = Object.FindObjectOfType<BackgroundMaterialOverride>(),
                EnsureParallaxLayers = Object.FindObjectOfType<EnsureParallaxLayers>(),
                InertiaArrowHud = Object.FindObjectOfType<InertiaArrowHUD>(),
                ScoreCounter = Object.FindObjectOfType<ScoreCounter>(),
                SpriteRenderers = Object.FindObjectsOfType<SpriteRenderer>(true),
                ParallaxLayers = Object.FindObjectsOfType<ParallaxLayer>(true),
                MonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>(true),
                Transforms = Object.FindObjectsOfType<Transform>(true)
            };
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
    }
}
