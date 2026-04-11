using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor helper for validating and repairing core Love Metro scene wiring.
/// </summary>
public class SceneSetupTool : EditorWindow
{
    private const string ResourcesDir = "Assets/Resources";
    private const string PassengerSettingsPath = ResourcesDir + "/PassengerSettings.asset";

    [MenuItem("Tools/Love Metro/Setup Scene", false, 1)]
    public static void SetupScene()
    {
        int fixCount = 0;
        var log = new StringBuilder();

        fixCount += EnsureScoreCounterTrainReference(log);
        fixCount += EnsureBackgroundActive(log);
        fixCount += EnsurePassengerSettingsAsset(log);

        if (fixCount > 0)
            EditorSceneManager.MarkAllScenesDirty();

        string message = BuildSetupMessage(fixCount, log);
        Debug.Log(message);
        EditorUtility.DisplayDialog("Love Metro - Scene Setup", message, "OK");
    }

    [MenuItem("Tools/Love Metro/Check Scene Status", false, 2)]
    public static void CheckSceneStatus()
    {
        var status = new StringBuilder();
        status.AppendLine("=== Love Metro Scene Status ===");
        status.AppendLine();

        AppendObjectStatus<TrainManager>(status, "TrainManager");
        AppendScoreCounterStatus(status);
        AppendObjectStatus<PassangerSpawner>(status, "PassangerSpawner");
        AppendPassengerSettingsStatus(status);
        AppendBackgroundStatus(status);

        string message = status.ToString();
        Debug.Log(message);
        EditorUtility.DisplayDialog("Love Metro - Scene Status", message, "OK");
    }

    private static int EnsureScoreCounterTrainReference(StringBuilder log)
    {
        TrainManager trainManager = FindSceneObject<TrainManager>();
        ScoreCounter scoreCounter = FindSceneObject<ScoreCounter>();

        if (trainManager == null)
        {
            Debug.LogWarning("[SceneSetup] TrainManager not found in scene.");
            log.AppendLine("Missing TrainManager.");
            return 0;
        }

        log.AppendLine("TrainManager found: " + trainManager.name);
        if (scoreCounter == null)
        {
            Debug.LogWarning("[SceneSetup] ScoreCounter not found in scene.");
            log.AppendLine("Missing ScoreCounter.");
            return 0;
        }

        var serializedCounter = new SerializedObject(scoreCounter);
        SerializedProperty trainProperty = serializedCounter.FindProperty("_trainManager");
        if (trainProperty == null)
        {
            log.AppendLine("ScoreCounter._trainManager field not found.");
            return 0;
        }

        if (trainProperty.objectReferenceValue != null)
        {
            log.AppendLine("ScoreCounter._trainManager already assigned.");
            return 0;
        }

        trainProperty.objectReferenceValue = trainManager;
        serializedCounter.ApplyModifiedProperties();
        EditorUtility.SetDirty(scoreCounter);
        log.AppendLine("Assigned ScoreCounter._trainManager.");
        return 1;
    }

    private static int EnsureBackgroundActive(StringBuilder log)
    {
        GameObject background = FindSceneGameObject("Background");
        if (background == null)
        {
            Debug.LogWarning("[SceneSetup] Background object not found.");
            log.AppendLine("Background object not found.");
            return 0;
        }

        if (background.activeSelf)
        {
            log.AppendLine("Background already active.");
            return 0;
        }

        background.SetActive(true);
        EditorUtility.SetDirty(background);
        log.AppendLine("Activated Background.");
        return 1;
    }

    private static int EnsurePassengerSettingsAsset(StringBuilder log)
    {
        if (!Directory.Exists(ResourcesDir))
        {
            Directory.CreateDirectory(ResourcesDir);
            AssetDatabase.Refresh();
        }

        PassengerSettings existing = AssetDatabase.LoadAssetAtPath<PassengerSettings>(PassengerSettingsPath);
        if (existing != null)
        {
            log.AppendLine("PassengerSettings.asset already exists.");
            return 0;
        }

        PassengerSettings settings = ScriptableObject.CreateInstance<PassengerSettings>();
        AssetDatabase.CreateAsset(settings, PassengerSettingsPath);
        AssetDatabase.SaveAssets();
        log.AppendLine("Created PassengerSettings.asset in Resources.");
        return 1;
    }

    private static string BuildSetupMessage(int fixCount, StringBuilder log)
    {
        var message = new StringBuilder();
        message.AppendLine($"Applied fixes: {fixCount}");
        message.AppendLine();
        message.Append(log);

        if (fixCount > 0)
        {
            message.AppendLine();
            message.AppendLine("Save the scene to persist these changes.");
        }

        return message.ToString();
    }

    private static void AppendObjectStatus<T>(StringBuilder status, string label) where T : Object
    {
        T found = FindSceneObject<T>();
        status.AppendLine(found != null ? $"{label}: found" : $"{label}: missing");
    }

    private static void AppendScoreCounterStatus(StringBuilder status)
    {
        ScoreCounter scoreCounter = FindSceneObject<ScoreCounter>();
        if (scoreCounter == null)
        {
            status.AppendLine("ScoreCounter: missing");
            return;
        }

        var serializedCounter = new SerializedObject(scoreCounter);
        SerializedProperty trainProperty = serializedCounter.FindProperty("_trainManager");
        bool linked = trainProperty != null && trainProperty.objectReferenceValue != null;
        status.AppendLine(linked
            ? "ScoreCounter._trainManager: assigned"
            : "ScoreCounter._trainManager: missing");
    }

    private static void AppendPassengerSettingsStatus(StringBuilder status)
    {
        bool hasSettings = File.Exists(PassengerSettingsPath);
        status.AppendLine(hasSettings
            ? "PassengerSettings.asset: found"
            : "PassengerSettings.asset: missing");
    }

    private static void AppendBackgroundStatus(StringBuilder status)
    {
        GameObject background = FindSceneGameObject("Background");
        if (background == null)
        {
            status.AppendLine("Background: missing");
            return;
        }

        status.AppendLine(background.activeSelf ? "Background: active" : "Background: inactive");
    }

    private static T FindSceneObject<T>() where T : Object
    {
        return Object.FindObjectOfType<T>(true);
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.name =