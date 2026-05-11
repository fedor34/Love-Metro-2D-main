using System.Text;
using UnityEditor;
using UnityEngine;

public static class PassengerSettingsMigrationReport
{
    private const string PrefabFolder = "Assets/Prefabs/Passangers";

    [MenuItem("Love Metro/Reports/Passenger Settings Migration Report")]
    public static void GenerateReport()
    {
        PassengerSettings settings = LoadSettings();
        if (settings == null)
        {
            Debug.LogWarning("[PassengerSettingsReport] PassengerSettings asset was not found.");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        StringBuilder report = new StringBuilder();
        report.AppendLine("[PassengerSettingsReport] Migration candidates");
        report.AppendLine($"Settings asset: {AssetDatabase.GetAssetPath(settings)}");

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Passenger passenger = prefab != null ? prefab.GetComponent<Passenger>() : null;
            if (passenger == null)
                continue;

            SerializedObject serializedPassenger = new SerializedObject(passenger);
            report.AppendLine();
            report.AppendLine(path);
            AppendFloat(report, serializedPassenger, "_speed", settings.baseSpeed, "baseSpeed");
            AppendFloat(report, serializedPassenger, "_grabingHandrailChance", settings.handrailGrabChance, "handrailGrabChance");
            AppendVector2(report, serializedPassenger, "HandrailStandingTimeInterval", settings.handrailStandingTimeInterval, "handrailStandingTimeInterval");
            AppendFloat(report, serializedPassenger, "_handrailMinGrabbingSpeed", settings.handrailMinGrabbingSpeed, "handrailMinGrabbingSpeed");
            AppendFloat(report, serializedPassenger, "_minFallingSpeed", settings.minFallingSpeed, "minFallingSpeed");
            AppendFloat(report, serializedPassenger, "_handrailCooldown", settings.handrailCooldown, "handrailCooldown");
        }

        Debug.Log(report.ToString());
    }

    private static PassengerSettings LoadSettings()
    {
        string[] guids = AssetDatabase.FindAssets("PassengerSettings t:PassengerSettings", new[] { "Assets/Resources" });
        if (guids == null || guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<PassengerSettings>(path);
    }

    private static void AppendFloat(
        StringBuilder report,
        SerializedObject serializedPassenger,
        string passengerPropertyName,
        float settingsValue,
        string settingsPropertyName)
    {
        SerializedProperty property = serializedPassenger.FindProperty(passengerPropertyName);
        if (property == null)
            return;

        float passengerValue = property.floatValue;
        string status = Mathf.Approximately(passengerValue, settingsValue) ? "same" : "diff";
        report.AppendLine($"  {passengerPropertyName} -> {settingsPropertyName}: prefab={passengerValue} settings={settingsValue} [{status}]");
    }

    private static void AppendVector2(
        StringBuilder report,
        SerializedObject serializedPassenger,
        string passengerPropertyName,
        Vector2 settingsValue,
        string settingsPropertyName)
    {
        SerializedProperty property = serializedPassenger.FindProperty(passengerPropertyName);
        if (property == null)
            return;

        Vector2 passengerValue = property.vector2Value;
        string status = passengerValue == settingsValue ? "same" : "diff";
        report.AppendLine($"  {passengerPropertyName} -> {settingsPropertyName}: prefab={passengerValue} settings={settingsValue} [{status}]");
    }
}
