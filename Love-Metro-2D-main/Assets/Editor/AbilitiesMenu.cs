// Adds explicit Assets/Create menu entries for abilities
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AbilitiesMenu
{
    // Ensure the item appears exactly alongside other abilities
    [MenuItem("Assets/Create/Passengers/Ability/Gentle", priority = 201)]
    private static void CreateGentleAbility()
    {
        var asset = ScriptableObject.CreateInstance<GentlePairAbility>();
        ProjectWindowUtil.CreateAsset(asset, "GentlePairAbility.asset");
    }
}
#endif

