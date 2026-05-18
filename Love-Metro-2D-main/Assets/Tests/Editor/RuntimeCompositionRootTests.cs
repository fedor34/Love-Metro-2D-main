using LoveMetro.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public class RuntimeCompositionRootTests
{
    [SetUp]
    public void Setup()
    {
        RuntimeServices.Instance.ResetForTests();
        DestroyAll<RuntimeCompositionRoot>();
        DestroyAll<PassengerRegistry>();
        DestroyAll<CouplesManager>();
        DestroyAll<FieldEffectSystem>();
        DestroyAll<ClickDirectionManager>();
        DestroyAll<ManualPairingManager>();
        DestroyAll<TrainManager>();
        DestroyAll<EventSystem>();
    }

    [TearDown]
    public void TearDown()
    {
        RuntimeServices.Instance.ResetForTests();
        DestroyAll<RuntimeCompositionRoot>();
        DestroyAll<PassengerRegistry>();
        DestroyAll<CouplesManager>();
        DestroyAll<FieldEffectSystem>();
        DestroyAll<ClickDirectionManager>();
        DestroyAll<ManualPairingManager>();
        DestroyAll<TrainManager>();
        DestroyAll<EventSystem>();
    }

    [Test]
    public void BindActiveScene_IsIdempotentAndCreatesSingleCoreServices()
    {
        RuntimeCompositionRoot.BindActiveScene(RuntimeCompositionOptions.CoreDefaults);
        RuntimeCompositionRoot.BindActiveScene(RuntimeCompositionOptions.CoreDefaults);

        Assert.AreEqual(1, Object.FindObjectsOfType<RuntimeCompositionRoot>().Length);
        Assert.AreEqual(1, Object.FindObjectsOfType<PassengerRegistry>().Length);
        Assert.AreEqual(1, Object.FindObjectsOfType<CouplesManager>().Length);
        Assert.AreEqual(1, Object.FindObjectsOfType<FieldEffectSystem>().Length);
        Assert.AreEqual(1, Object.FindObjectsOfType<ClickDirectionManager>().Length);
        Assert.AreEqual(1, Object.FindObjectsOfType<ManualPairingManager>().Length);
    }

    [Test]
    public void BindActiveScene_RegistersCreatedServices()
    {
        RuntimeCompositionRoot.BindActiveScene(RuntimeCompositionOptions.CoreDefaults);

        Assert.IsNotNull(RuntimeServices.Instance.PassengerRegistry);
        Assert.IsNotNull(RuntimeServices.Instance.FieldEffectSystem);
        Assert.IsNotNull(RuntimeServices.Instance.InputIntentProvider);
        Assert.IsNotNull(RuntimeServices.Instance.ManualPairingService);
    }

    [Test]
    public void BindActiveScene_RegistersTrainMotionAndStationFlowWhenTrainExists()
    {
        GameObject trainObject = new GameObject("Train");
        TrainManager train = trainObject.AddComponent<TrainManager>();

        try
        {
            RuntimeCompositionRoot.BindActiveScene(RuntimeCompositionOptions.CoreDefaults);

            Assert.AreSame(train, RuntimeServices.Instance.TrainMotionEvents);
            Assert.AreSame(train, RuntimeServices.Instance.StationFlowService);
        }
        finally
        {
            Object.DestroyImmediate(trainObject);
        }
    }

    [Test]
    public void SceneObjectIndex_CoreDefaults_DoesNotCollectHeavySceneArrays()
    {
        GameObject spriteObject = new GameObject("HeavySpriteRenderer");
        GameObject parallaxObject = new GameObject("HeavyParallaxLayer");
        GameObject markerObject = new GameObject("HeavyMonoBehaviourMarker");

        try
        {
            spriteObject.AddComponent<SpriteRenderer>();
            parallaxObject.AddComponent<SpriteRenderer>();
            parallaxObject.AddComponent<ParallaxLayer>();
            markerObject.AddComponent<LightweightCaptureMarker>();

            SceneObjectIndex index = SceneObjectIndex.CaptureActiveScene(RuntimeCompositionOptions.CoreDefaults);

            Assert.IsNull(index.SpriteRenderers);
            Assert.IsNull(index.ParallaxLayers);
            Assert.IsNull(index.MonoBehaviours);
            Assert.IsNull(index.Transforms);
        }
        finally
        {
            Object.DestroyImmediate(spriteObject);
            Object.DestroyImmediate(parallaxObject);
            Object.DestroyImmediate(markerObject);
        }
    }

    private static void DestroyAll<T>() where T : Component
    {
        foreach (T component in Object.FindObjectsOfType<T>())
        {
            if (component != null)
                Object.DestroyImmediate(component.gameObject);
        }
    }

    private sealed class LightweightCaptureMarker : MonoBehaviour
    {
    }
}
