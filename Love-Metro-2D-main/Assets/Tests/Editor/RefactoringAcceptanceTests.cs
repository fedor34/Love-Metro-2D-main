using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RefactoringAcceptanceTests
{
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
}
