// Editor helper to create GentlePairAbility asset via menu
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateGentleAbilityAsset
{
    [MenuItem("Tools/Passengers/Create Gentle Pair Ability Asset")]
    private static void CreateAsset()
    {
        var asset = ScriptableObject.CreateInstance<GentlePairAbility>();
        string dir = "Assets/personas";
        if (!AssetDatabase.IsValidFolder(dir)) dir = "Assets";
        string path = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(dir, "GentlePairAbility.asset"));
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        Debug.Log($"Created GentlePairAbility at {path}");
    }
}
#endif

