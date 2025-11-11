using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class VIPSetup
{
    private const string FemaleSetPath = "Assets/Sprites/passangers/д 2"; // набор для девушки
    private const string MaleSetPath   = "Assets/Sprites/passangers/п 2"; // набор для парня

    private const string FemaleBasePrefab = "Assets/Prefabs/Passangers/PassangerFemale.prefab";
    private const string MaleBasePrefab   = "Assets/Prefabs/Passangers/PassangerMale1.prefab";

    private const string FemaleVipPrefab  = "Assets/Prefabs/Passangers/PassangerFemale_VIP.prefab";
    private const string MaleVipPrefab    = "Assets/Prefabs/Passangers/PassangerMale_VIP.prefab";

    [MenuItem("Tools/Setup VIP Pair (d2 + p2)")]
    public static void SetupVipPrefabsAndAssign()
    {
        // 1) Сгенерировать/обновить префабы VIP
        var female = BuildVipPrefab(FemaleBasePrefab, FemaleVipPrefab, FemaleSetPath);
        var male   = BuildVipPrefab(MaleBasePrefab, MaleVipPrefab, MaleSetPath);

        if (female == null || male == null)
        {
            EditorUtility.DisplayDialog("VIP Setup", "Не удалось создать VIP префабы. Проверьте, что базовые префабы/спрайты существуют.", "OK");
            return;
        }

        // 2) Назначить в PassangerSpawner в активной сцене
        var spawner = Object.FindObjectOfType<PassangerSpawner>();
        if (spawner != null)
        {
            Undo.RecordObject(spawner, "Assign VIP Prefabs");
            var so = new SerializedObject(spawner);
            so.FindProperty("_specialFemalePrefab").objectReferenceValue = female;
            so.FindProperty("_specialMalePrefab").objectReferenceValue   = male;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("VIP Setup", "Готово! Сгенерированы и назначены VIP префабы (д2/п2).", "OK");
    }

    private static GameObject BuildVipPrefab(string basePrefabPath, string vipPrefabPath, string spriteFolder)
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
        if (basePrefab == null) return null;

        // Загружаем спрайты набора
        var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(spriteFolder)
            .OfType<Sprite>()
            .ToList();
        if (sprites.Count == 0)
        {
            // LoadAllAssetRepresentationsAtPath не работает для папки — загрузим все файлы в каталоге
            var files = Directory.Exists(spriteFolder) ? Directory.GetFiles(spriteFolder, "*.png") : new string[0];
            foreach (var f in files)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\','/'));
                if (s != null) sprites.Add(s);
            }
        }
        if (sprites.Count == 0) return null;

        // Более точный подбор: сначала ищем точное совпадение имени, затем по подстроке
        Sprite FindExactOrContains(params string[] candidates)
        {
            foreach (var c in candidates)
            {
                var exact = sprites.FirstOrDefault(s => s.name.Equals(c, System.StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;
            }
            foreach (var c in candidates)
            {
                var contains = sprites.FirstOrDefault(s => s.name.ToLower().Contains(c.ToLower()));
                if (contains != null) return contains;
            }
            return null;
        }

        // Женский набор часто имеет ровно "стоит"; мужской может иметь "стоит за ручку" — мы приоритетим точное "стоит"
        var spStanding = FindExactOrContains("стоит");
        var spHold     = FindExactOrContains("держится", "держит", "за ручку");
        var spLove     = FindExactOrContains("обнимается", "обним");
        var spFall     = FindExactOrContains("врез", "столк", "падает", "падение");
        var spWalk1    = FindExactOrContains("шаг 1", "шаг1");
        var spWalk2    = FindExactOrContains("шаг 2", "шаг2");
        var spWalk3    = FindExactOrContains("шаг 3", "шаг3");
        var spWalk4    = FindExactOrContains("шаг 4", "шаг4");

        var go = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(basePrefab));
        try
        {
            var sr = go.GetComponent<SpriteRenderer>();
            var animator = go.GetComponent<Animator>();
            if (sr == null || animator == null) return null;

            // Подготовим выходную папку для клипов и override-контроллера
            // ВАЖНО: делаем отдельную папку на каждый VIP-префаб, чтобы клипы не перезаписывали друг друга
            string prefabName = Path.GetFileNameWithoutExtension(vipPrefabPath);
            string outRoot = "Assets/Animations/VIPGenerated";
            string outDir = Path.Combine(outRoot, prefabName);
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            // Создаём/обновляем клипы как отдельные ассеты (НЕ AddObjectToAsset)
            var clipStanding = CreateOrUpdateSingleFrameClip(Path.Combine(outDir, "Standing_VIP.anim"), spStanding, false);
            var clipHold     = CreateOrUpdateSingleFrameClip(Path.Combine(outDir, "Holding_VIP.anim"),  spHold, false);
            var clipLove     = CreateOrUpdateSingleFrameClip(Path.Combine(outDir, "InLove_VIP.anim"),   spLove, true);
            var clipFall     = CreateOrUpdateSingleFrameClip(Path.Combine(outDir, "Falling_VIP.anim"),  spFall, false);
            var clipWalk     = CreateOrUpdateWalkClip(Path.Combine(outDir, "Walking_VIP.anim"), new[]{spWalk1, spWalk2, spWalk3, spWalk4});
            Debug.Log($"[VIPSetup] Built clips for '{prefabName}' in '{outDir}'. Standing='{spStanding?.name}', Hold='{spHold?.name}', Love='{spLove?.name}', Fall='{spFall?.name}', Walk='{spWalk1?.name},{spWalk2?.name},{spWalk3?.name},{spWalk4?.name}'");

            // Создаём AnimatorOverrideController как ассет
            var baseCtrl = animator.runtimeAnimatorController;
            if (baseCtrl == null) return null;
            string aocPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(vipPrefabPath) + ".overrideController");
            var aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(aocPath);
            if (aoc == null)
            {
                aoc = new AnimatorOverrideController(baseCtrl);
                AssetDatabase.CreateAsset(aoc, aocPath);
            }
            else
            {
                aoc.runtimeAnimatorController = baseCtrl;
            }

            var overrides = aoc.clips.ToList();
            for (int i = 0; i < overrides.Count; i++)
            {
                var orig = overrides[i].originalClip;
                var name = orig != null ? orig.name : string.Empty;
                if (name == "Standing") overrides[i].overrideClip = clipStanding;
                else if (name == "HoldingOnHandrails") overrides[i].overrideClip = clipHold;
                else if (name == "InLove") overrides[i].overrideClip = clipLove;
                else if (name == "Falling") overrides[i].overrideClip = clipFall;
                else if (name == "Walking") overrides[i].overrideClip = clipWalk;
            }
            aoc.clips = overrides.ToArray();
            // Log mapping for diagnostics
            foreach (var p in aoc.clips)
            {
                var o = p.originalClip != null ? p.originalClip.name : "<null>";
                var r = p.overrideClip != null ? p.overrideClip.name : "<null>";
                Debug.Log($"[VIPSetup] AOC map '{prefabName}': {o} -> {r}");
            }
            EditorUtility.SetDirty(aoc);
            animator.runtimeAnimatorController = aoc;
            Debug.Log($"[VIPSetup] Assigned AnimatorOverrideController '{aocPath}' to '{prefabName}'");

            // Установим стартовый спрайт
            if (spStanding != null) sr.sprite = spStanding;

            // Сохраняем VIP префаб
            var dir = Path.GetDirectoryName(vipPrefabPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            PrefabUtility.SaveAsPrefabAsset(go, vipPrefabPath);
            return AssetDatabase.LoadAssetAtPath<GameObject>(vipPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(go);
        }
    }

    private static AnimationClip CreateOrUpdateSingleFrameClip(string path, Sprite sprite, bool loop)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        if (sprite == null)
        {
            // Если текстура не найдена — оставляем пустой клип, но не падаем
        }
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        var objRefCurve = new ObjectReferenceKeyframe[1];
        objRefCurve[0] = new ObjectReferenceKeyframe { time = 0f, value = sprite };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, objRefCurve);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    private static AnimationClip CreateOrUpdateWalkClip(string path, Sprite[] frames)
    {
        var valid = frames.Where(f => f != null).ToArray();
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        float dt = 0.12f;
        ObjectReferenceKeyframe[] keys;
        if (valid.Length > 0)
        {
            keys = new ObjectReferenceKeyframe[valid.Length + 1];
            for (int i = 0; i < valid.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i * dt, value = valid[i] };
            }
            keys[keys.Length - 1] = new ObjectReferenceKeyframe { time = valid.Length * dt, value = valid[0] };
        }
        else
        {
            keys = new ObjectReferenceKeyframe[1];
            keys[0] = new ObjectReferenceKeyframe { time = 0f, value = null };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }
}
