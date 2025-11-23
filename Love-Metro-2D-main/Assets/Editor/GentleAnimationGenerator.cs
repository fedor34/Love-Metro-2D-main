// Generates animation clips and AnimatorOverrideControllers for the "д 3" (female) and "п 3" (male)
// sprite sets, and places them under Assets/Animations/GentleGenerated/ .
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GentleAnimationGenerator
{
    private const string FemaleSpritesPath = "Assets/Sprites/passangers/д 3";
    private const string MaleSpritesPath   = "Assets/Sprites/passangers/п 3";

    private const string FemaleBaseController = "Assets/Animations/Female 1/female1.controller";
    private const string MaleBaseController   = "Assets/Animations/Male 1/Male1.controller";

    private const string OutRoot = "Assets/Animations/GentleGenerated";

    [MenuItem("Tools/Passengers/Generate Gentle Controllers (d3 + m3)")]
    public static void Generate()
    {
        EnsureFolder(OutRoot);
        EnsureFolder(OutRoot + "/Female_d3");
        EnsureFolder(OutRoot + "/Male_m3");

        // Create clips
        var femaleClips = CreateClips(
            FemaleSpritesPath,
            OutRoot + "/Female_d3",
            standingName: "Standing_d3",
            walkingName:  "Walking_d3",
            inLoveName:   "InLove_d3",
            fallingName:  "Falling_d3",
            holdingName:  "Holding_d3",
            holdingFileCandidates: new[]{"держится за ручку.png", "стоит за ручку.png"}
        );

        var maleClips = CreateClips(
            MaleSpritesPath,
            OutRoot + "/Male_m3",
            standingName: "Standing_m3",
            walkingName:  "Walking_m3",
            inLoveName:   "InLove_m3",
            fallingName:  "Falling_m3",
            holdingName:  "Holding_m3",
            holdingFileCandidates: new[]{"стоит за ручку.png", "держится за ручку.png"}
        );

        // Create override controllers
        var femaleBase = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FemaleBaseController);
        var maleBase   = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MaleBaseController);
        if (femaleBase == null || maleBase == null)
        {
            Debug.LogError("GentleAnimationGenerator: Base controllers not found. Check paths.");
            return;
        }

        var femaleAOC = new AnimatorOverrideController(femaleBase);
        ApplyOverrides(femaleAOC, femaleClips);
        var femaleAOCPath = OutRoot + "/PassangerFemale_GENTLE_d3.overrideController";
        AssetDatabase.CreateAsset(femaleAOC, femaleAOCPath);

        var maleAOC = new AnimatorOverrideController(maleBase);
        ApplyOverrides(maleAOC, maleClips);
        var maleAOCPath = OutRoot + "/PassangerMale_GENTLE_m3.overrideController";
        AssetDatabase.CreateAsset(maleAOC, maleAOCPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("GentleAnimationGenerator: Generated animations and override controllers for d3+m3.");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\','/');
            var name = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private struct ClipSet
    {
        public AnimationClip Standing, Walking, InLove, Falling, Holding;
    }

    private static ClipSet CreateClips(string spritesFolder, string outFolder,
        string standingName, string walkingName, string inLoveName, string fallingName, string holdingName,
        string[] holdingFileCandidates)
    {
        var standing = LoadSprite(spritesFolder + "/стоит.png");
        var inLove   = LoadSprite(spritesFolder + "/обнимается.png");
        var falling  = LoadSprite(spritesFolder + "/врезалась.png");
        // "держится за ручку" (female) or "стоит за ручку" (male) — try both
        Sprite holding = null;
        foreach (var cand in holdingFileCandidates)
        {
            holding = holding ?? LoadSprite(System.IO.Path.Combine(spritesFolder, cand).Replace('\\','/'));
        }

        var w1 = LoadSprite(spritesFolder + "/шаг 1.png");
        var w2 = LoadSprite(spritesFolder + "/шаг 2.png");
        var w3 = LoadSprite(spritesFolder + "/шаг 3.png");
        var w4 = LoadSprite(spritesFolder + "/шаг 4.png");

        var set = new ClipSet
        {
            Standing = CreateSingleFrameClip(outFolder + "/" + standingName + ".anim", standing),
            InLove   = CreateSingleFrameClip(outFolder + "/" + inLoveName   + ".anim", inLove),
            Falling  = CreateSingleFrameClip(outFolder + "/" + fallingName  + ".anim", falling),
            Holding  = CreateSingleFrameClip(outFolder + "/" + holdingName  + ".anim", holding),
            Walking  = CreateWalkClip(outFolder + "/" + walkingName  + ".anim", new[]{w1,w2,w3,w4}),
        };
        return set;
    }

    private static Sprite LoadSprite(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"GentleAnimationGenerator: sprite not found: {path}");
        return s;
    }

    private static AnimationClip CreateSingleFrameClip(string path, Sprite sprite)
    {
        var clip = new AnimationClip { frameRate = 60f, name = System.IO.Path.GetFileNameWithoutExtension(path) };
        if (sprite != null)
        {
            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[]
            {
                new ObjectReferenceKeyframe{ time = 0f, value = sprite }
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateWalkClip(string path, Sprite[] frames)
    {
        var clip = new AnimationClip { frameRate = 60f, name = System.IO.Path.GetFileNameWithoutExtension(path) };
        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var list = new List<ObjectReferenceKeyframe>();
        float dt = 0.25f; // 4 кадра за секунду
        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null) continue;
                list.Add(new ObjectReferenceKeyframe { time = i * dt, value = frames[i] });
            }
            // замкнуть цикл
            if (frames.Length > 0 && frames[0] != null)
                list.Add(new ObjectReferenceKeyframe { time = frames.Length * dt, value = frames[0] });
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, list.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void ApplyOverrides(AnimatorOverrideController aoc, ClipSet set)
    {
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        aoc.GetOverrides(pairs);
        // Load base clips to match by name
        var map = new Dictionary<string, AnimationClip>();
        foreach (var kv in pairs)
        {
            if (kv.Key == null) continue;
            map[kv.Key.name] = kv.Key;
        }
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        void TrySet(string baseName, AnimationClip newClip)
        {
            if (newClip == null) return;
            if (map.TryGetValue(baseName, out var baseClip))
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(baseClip, newClip));
        }
        TrySet("Standing", set.Standing);
        TrySet("Walking", set.Walking);
        TrySet("InLove", set.InLove);
        TrySet("Falling", set.Falling);
        TrySet("HoldingOnHandrails", set.Holding);

        // Preserve other mappings
        foreach (var kv in pairs)
        {
            if (overrides.Exists(p => p.Key == kv.Key)) continue;
            overrides.Add(kv); // keep original
        }
        aoc.ApplyOverrides(overrides);
    }
}
#endif

