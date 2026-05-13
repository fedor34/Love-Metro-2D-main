using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RefactoringAcceptanceTests
{
    private static readonly string[] ForbiddenRuntimeDiscoveryTokens =
    {
        "FindObjectOfType<",
        "FindObjectsOfType<",
        "Object.FindObjectOfType<",
        "Object.FindObjectsOfType<",
        "GameObject.Find(",
        "Resources.FindObjectsOfTypeAll(",
        "BindingFlags"
    };

    [Test]
    public void RuntimeUiCode_DoesNotUseReflectionConfiguration()
    {
        string uiRoot = Path.Combine(Application.dataPath, "Scripts", "UI");
        foreach (string file in Directory.GetFiles(uiRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            Assert.IsFalse(source.Contains("BindingFlags"), $"{file} still uses BindingFlags.");
            Assert.IsFalse(source.Contains("GetField("), $"{file} still uses reflective field access.");
        }
    }

    [Test]
    public void PassengerMovementStrategies_UseRegistryBeforeSceneScanFallback()
    {
        string path = Path.Combine(Application.dataPath, "Scripts", "Movement", "PassengerMovementStrategies.cs");
        string source = File.ReadAllText(path);

        StringAssert.Contains("PassengerRegistry.Instance.AllPassengers", source);
    }

    [Test]
    public void PassengerSettings_ResolveProvidesSingleDefaultSource()
    {
        PassengerSettings defaultSettings = PassengerSettings.Default;
        Assert.IsNotNull(defaultSettings);
        Assert.AreSame(defaultSettings, PassengerSettings.Resolve(null));

        PassengerSettings overrideSettings = ScriptableObject.CreateInstance<PassengerSettings>();
        try
        {
            Assert.AreSame(overrideSettings, PassengerSettings.Resolve(overrideSettings));
        }
        finally
        {
            Object.DestroyImmediate(overrideSettings);
        }
    }

    [Test]
    public void PassengerPrefabs_UseExplicitPassengerSettingsAssets()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Passangers" });
        Assert.IsNotEmpty(prefabGuids);

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Passenger passenger = prefab != null ? prefab.GetComponent<Passenger>() : null;
            if (passenger == null)
                continue;

            SerializedObject serializedPassenger = new SerializedObject(passenger);
            SerializedProperty settings = serializedPassenger.FindProperty("_settings");
            Assert.IsNotNull(settings, $"{path} has no _settings property.");
            Assert.IsNotNull(settings.objectReferenceValue, $"{path} must reference a PassengerSettings asset.");
        }
    }

    [Test]
    public void PassengerRuntimeCode_DoesNotOverwriteTuningFallbacks()
    {
        string path = Path.Combine(Application.dataPath, "Scripts", "Passenger.cs");
        string source = File.ReadAllText(path);

        Assert.IsFalse(source.Contains("_speed *= GlobalSpeedMultiplier"), "Passenger speed must come from PassengerSettings.");
        Assert.IsFalse(source.Contains("if (_launchSensitivity <="), "Launch sensitivity fallback must live in PassengerSettings validation.");
        Assert.IsFalse(source.Contains("_wallBounceBoost = 1f"), "Wall bounce boost must come from PassengerSettings.");
    }

    [Test]
    public void TrainRuntimeCode_DoesNotOverwriteSerializedMovementSettings()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        foreach (string file in Directory.GetFiles(scriptsRoot, "TrainManager*.cs", SearchOption.TopDirectoryOnly))
        {
            string source = File.ReadAllText(file);
            Assert.IsFalse(source.Contains("ApplySerializedFallbacks"), $"{file} still overwrites serialized TrainManager settings.");
        }
    }

    [Test]
    public void RuntimeSceneDiscovery_IsLimitedToInstallersDiagnosticsAndDeprecatedCode()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        var approved = new HashSet<string>
        {
            NormalizeAssetPath(Path.Combine(scriptsRoot, "Core", "GameInitializer.cs")),
            NormalizeAssetPath(Path.Combine(scriptsRoot, "Core", "GameBootstrap.cs")),
            NormalizeAssetPath(Path.Combine(scriptsRoot, "Core", "AutoAttachCollisionDebugger.cs")),
            NormalizeAssetPath(Path.Combine(scriptsRoot, "Core", "FindAllBoundaries.cs")),
            NormalizeAssetPath(Path.Combine(scriptsRoot, "Core", "LayerCollisionMatrixDecoder.cs")),
            NormalizeAssetPath(Path.Combine(scriptsRoot, "UI", "MenuInitializer.cs")),
            NormalizeAssetPath(Path.Combine(scriptsRoot, "Parallax", "EnsureParallaxLayers.cs")),
            NormalizeAssetPath(Path.Combine(scriptsRoot, "Parallax", "ParallaxBootstrap.cs")),
            NormalizeAssetPath(Path.Combine(scriptsRoot, "FieldEffects", "Core", "FieldEffectMenus.cs"))
        };

        foreach (string file in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string normalized = NormalizeAssetPath(file);
            if (normalized.Contains("/_Deprecated/"))
                continue;

            if (!ContainsForbiddenRuntimeDiscovery(file))
                continue;

            Assert.IsTrue(approved.Contains(normalized), $"{normalized} uses runtime scene discovery outside an approved installer/diagnostic file.");
        }
    }

    [Test]
    public void GameplayHotPaths_DoNotUseRuntimeSceneDiscovery()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        string[] hotPathFiles =
        {
            Path.Combine(scriptsRoot, "Couple.cs"),
            Path.Combine(scriptsRoot, "CouplesManager.cs"),
            Path.Combine(scriptsRoot, "Passenger.StateMachine.cs"),
            Path.Combine(scriptsRoot, "Movement", "PassengerMovementStrategies.cs"),
            Path.Combine(scriptsRoot, "ScoreCounter.cs"),
            Path.Combine(scriptsRoot, "TrainManager.Motion.cs"),
            Path.Combine(scriptsRoot, "ParallaxEffect.cs"),
            Path.Combine(scriptsRoot, "Parallax", "SimpleBackgroundScroller.cs"),
            Path.Combine(scriptsRoot, "Parallax", "BackgroundGroupScroller.cs"),
            Path.Combine(scriptsRoot, "Parallax", "ParallaxMaterialDriver.cs"),
            Path.Combine(scriptsRoot, "Parallax", "BackgroundMaterialOverride.cs"),
            Path.Combine(scriptsRoot, "FieldEffects", "Core", "FieldEffectSystem.cs"),
            Path.Combine(scriptsRoot, "Core", "VipBoundaryReturnSystem.cs"),
            Path.Combine(scriptsRoot, "UI", "SettingsPanel.cs"),
            Path.Combine(scriptsRoot, "UI", "CharactersPanel.cs"),
            Path.Combine(scriptsRoot, "UI", "InertiaArrowHUD.cs"),
            Path.Combine(scriptsRoot, "UI", "EnsureEventSystem.cs")
        };

        foreach (string file in hotPathFiles)
            Assert.IsFalse(ContainsForbiddenRuntimeDiscovery(file), $"{NormalizeAssetPath(file)} uses runtime scene discovery in a gameplay/UI hot path.");
    }

    [Test]
    public void ScoreCounter_DoesNotOwnFloatingScorePresentationImplementation()
    {
        string path = Path.Combine(Application.dataPath, "Scripts", "ScoreCounter.cs");
        string source = File.ReadAllText(path);

        Assert.IsFalse(source.Contains("IEnumerator"), "ScoreCounter must delegate floating-score coroutines to FloatingScorePresenter.");
        Assert.IsFalse(source.Contains("CreateFloatingText"), "ScoreCounter must not create floating score text directly.");
        Assert.IsFalse(source.Contains("WorldToCanvasPoint"), "ScoreCounter must delegate world-to-canvas conversion to ScoreHudView.");
        Assert.IsFalse(source.Contains("ShowFloatingDelta"), "ScoreCounter must delegate floating penalty presentation.");
        Assert.IsFalse(source.Contains("ScorePointsFromMatching"), "ScoreCounter must delegate match score presentation.");
    }

    [Test]
    public void Passenger_DoesNotDeclareNestedStateClasses()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(scriptsRoot, "Passenger*.cs", SearchOption.TopDirectoryOnly));

        string legacyStatesRoot = Path.Combine(scriptsRoot, "Passenger", "States");
        if (Directory.Exists(legacyStatesRoot))
            files.AddRange(Directory.GetFiles(legacyStatesRoot, "*.cs", SearchOption.AllDirectories));

        string[] forbiddenStateClassTokens =
        {
            "class Wandering",
            "class Falling",
            "class Flying",
            "class Matching",
            "class StayingOnHandrail",
            "class BeingAbsorbed",
            "class PassangerState"
        };

        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            foreach (string token in forbiddenStateClassTokens)
                Assert.IsFalse(source.Contains(token), $"{file} still declares nested passenger state token {token}.");
        }
    }

    [Test]
    public void Passenger_DoesNotOwnStateInstancesOrFactory()
    {
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        string[] files = Directory.GetFiles(scriptsRoot, "Passenger*.cs", SearchOption.TopDirectoryOnly);
        string[] forbiddenTokens =
        {
            "IPassengerState _currentState",
            "PassengerStateMachine _stateMachine",
            "PassengerStateFactory _stateFactory",
            "new PassengerStateFactory",
            "wanderingState",
            "fallingState",
            "flyingState",
            "matchingState",
            "stayingOnHandrailState",
            "beingAbsorbedState"
        };

        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            foreach (string token in forbiddenTokens)
                Assert.IsFalse(source.Contains(token), $"{file} still owns passenger state runtime detail {token}.");
        }
    }

    [Test]
    public void PassengerStateContext_DoesNotExposePassengerOrStatePassThroughs()
    {
        string contextPath = Path.Combine(Application.dataPath, "Scripts", "Runtime", "Passengers", "PassengerStateContext.cs");
        string source = File.ReadAllText(contextPath);
        string[] forbiddenTokens =
        {
            "global::Passenger Passenger",
            "Passenger.State",
            "StateAnimator",
            "StateCurrentMovingDirection",
            "StateTimeWithoutHolding",
            "StateSet",
            "StateGet",
            "StateEnter",
            "StateApply",
            "StateForward",
            "StateFind",
            "StateCollect",
            "StateRelease",
            "StateLog"
        };

        foreach (string token in forbiddenTokens)
            Assert.IsFalse(source.Contains(token), $"PassengerStateContext still exposes passenger state pass-through token {token}.");
    }

    [Test]
    public void ExtractedPassengerStates_UseContextAndMotionControllerForPassengerAccess()
    {
        string statesRoot = Path.Combine(Application.dataPath, "Scripts", "Runtime", "Passengers", "States");
        foreach (string file in Directory.GetFiles(statesRoot, "*PassengerState.cs", SearchOption.TopDirectoryOnly))
        {
            string source = File.ReadAllText(file);
            Assert.IsFalse(source.Contains("_rigidbody.velocity"), $"{file} writes Rigidbody2D.velocity directly.");
            Assert.IsFalse(source.Contains("_rigidbody.AddForce"), $"{file} calls Rigidbody2D.AddForce directly.");
            Assert.IsFalse(source.Contains("GetRigidbody().velocity"), $"{file} reads Rigidbody2D.velocity directly.");
            Assert.IsFalse(source.Contains("Passenger._"), $"{file} reaches into Passenger private fields.");
            Assert.IsFalse(source.Contains("Passanger._"), $"{file} reaches into Passenger private fields.");
            Assert.IsFalse(source.Contains("_collider"), $"{file} reaches into Passenger collider field.");
            Assert.IsFalse(source.Contains("releaseHandrail"), $"{file} reaches into Passenger handrail delegate.");
        }
    }

    [Test]
    public void PassengerStateFactory_DoesNotUseLegacyStateFallback()
    {
        string factoryPath = Path.Combine(Application.dataPath, "Scripts", "Runtime", "Passengers", "PassengerStateFactory.cs");
        string contextPath = Path.Combine(Application.dataPath, "Scripts", "Runtime", "Passengers", "PassengerStateContext.cs");

        string factorySource = File.ReadAllText(factoryPath);
        string contextSource = File.ReadAllText(contextPath);

        Assert.IsFalse(factorySource.Contains("CreateLegacyState"), "PassengerStateFactory still uses legacy state fallback.");
        Assert.IsFalse(contextSource.Contains("CreateLegacyState"), "PassengerStateContext still exposes legacy state fallback.");
        Assert.IsFalse(contextSource.Contains("legacyStateFactory"), "PassengerStateContext still stores legacy state factory.");
    }

    [Test]
    public void PassengerPrefabs_DoNotUseEmptyPhysicsIncludeLayers()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Passangers" });
        Assert.IsNotEmpty(prefabGuids);

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<Passenger>() == null)
                continue;

            Rigidbody2D rigidbody = prefab.GetComponent<Rigidbody2D>();
            Assert.IsNotNull(rigidbody, $"{path} has no Rigidbody2D.");
            Assert.AreNotEqual(0, rigidbody.includeLayers.value, $"{path} Rigidbody2D includeLayers is empty.");

            Collider2D collider = prefab.GetComponent<Collider2D>();
            Assert.IsNotNull(collider, $"{path} has no Collider2D.");
            Assert.AreNotEqual(0, collider.includeLayers.value, $"{path} Collider2D includeLayers is empty.");
        }
    }

    [Test]
    public void KeyPhysicsLayers_CanCollide()
    {
        AssertLayersCollide("Default", "Wall");
        AssertLayersCollide("Default", "SoftWall");
        AssertLayersCollide("Falling", "Wall");
        AssertLayersCollide("Falling", "SoftWall");
    }

    private static void AssertLayersCollide(string firstLayerName, string secondLayerName)
    {
        int firstLayer = LayerMask.NameToLayer(firstLayerName);
        int secondLayer = LayerMask.NameToLayer(secondLayerName);

        Assert.GreaterOrEqual(firstLayer, 0, $"Layer {firstLayerName} is missing.");
        Assert.GreaterOrEqual(secondLayer, 0, $"Layer {secondLayerName} is missing.");
        Assert.IsFalse(
            Physics2D.GetIgnoreLayerCollision(firstLayer, secondLayer),
            $"{firstLayerName} and {secondLayerName} are ignored in Physics2D settings.");
    }

    private static bool ContainsForbiddenRuntimeDiscovery(string file)
    {
        foreach (string line in File.ReadLines(file))
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//") || trimmed.StartsWith("///") || trimmed.StartsWith("*"))
                continue;

            for (int i = 0; i < ForbiddenRuntimeDiscoveryTokens.Length; i++)
            {
                if (line.Contains(ForbiddenRuntimeDiscoveryTokens[i]))
                    return true;
            }
        }

        return false;
    }

    private static string NormalizeAssetPath(string path)
    {
        return path.Replace('\\', '/');
    }
}
