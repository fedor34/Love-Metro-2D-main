// Editor utility: assign VipAbility asset to all PassangerSpawner components in open scenes
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class AssignVipToSpawners
{
    [MenuItem("Tools/Passengers/Assign VipAbility to Spawners")] 
    private static void AssignVip()
    {
        var guids = AssetDatabase.FindAssets("t:VipAbility VipAbility");
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning("AssignVipToSpawners: VipAbility asset not found. Create it via Create > Passengers > Ability > VIP.");
            return;
        }
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var vip = AssetDatabase.LoadAssetAtPath<VipAbility>(path);
        if (vip == null)
        {
            Debug.LogWarning("AssignVipToSpawners: Failed to load VipAbility asset.");
            return;
        }

        int assigned = 0;
        var spawners = Object.FindObjectsOfType<PassangerSpawner>(true);
        foreach (var sp in spawners)
        {
            if (sp == null) continue;
            var so = new SerializedObject(sp);
            var prop = so.FindProperty("_vipAbility");
            if (prop != null)
            {
                prop.objectReferenceValue = vip;
                so.ApplyModifiedProperties();
                assigned++;
            }
        }
        if (assigned > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"AssignVipToSpawners: Assigned VipAbility to {assigned} spawner(s). Save the scenes to persist.");
        }
        else
        {
            Debug.Log("AssignVipToSpawners: No PassangerSpawner found in open scenes.");
        }
    }
}
#endif

