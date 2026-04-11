using UnityEngine;

public static class ParallaxRendererClassifier
{
    public static bool IsParallaxDrivenMaterial(Material material)
    {
        return LooksLikeParallaxMaterial(material) || HasSupportedShaderProperties(material);
    }

    public static bool LooksLikeParallaxMaterial(Material material)
    {
        if (material == null)
            return false;

        string materialName = NormalizeLower(material.name);
        string shaderName = material.shader != null ? NormalizeLower(material.shader.name) : string.Empty;
        return LooksLikeParallaxMaterial(materialName, shaderName);
    }

    public static bool LooksLikeParallaxMaterial(string materialName, string shaderName)
    {
        return NormalizeLower(materialName).Contains("parallax") || NormalizeLower(shaderName).Contains("parallax");
    }

    public static bool HasSupportedShaderProperties(Material material)
    {
        if (material == null)
            return false;

        try
        {
            return material.HasProperty("_elapsedTime") ||
                   material.HasProperty("elapsedTime") ||
                   material.HasProperty("_Speed") ||
                   material.HasProperty("Speed");
        }
        catch
        {
            return false;
        }
    }

    public static bool IsLikelyBackgroundRenderer(
        SpriteRenderer renderer,
        string[] likelyBackgroundNames,
        string[] likelyBackgroundNamesRu,
        string[] sortingLayerHints)
    {
        if (renderer == null)
            return false;

        string objectName = NormalizeLower(renderer.gameObject.name);
        if (MatchesAny(objectName, likelyBackgroundNames) || MatchesAny(objectName, likelyBackgroundNamesRu))
            return true;

        string sortingLayer = NormalizeLower(renderer.sortingLayerName);
        if (MatchesAny(sortingLayer, sortingLayerHints))
            return true;

        return LooksLikeParallaxMaterial(renderer.sharedMaterial);
    }

    public static bool MatchesAny(string text, string[] keys)
    {
        string normalizedText = NormalizeLower(text);
        if (string.IsNullOrEmpty(normalizedText) || keys == null || keys.Length == 0)
            return false;

        for (int i = 0; i < keys.Length; i++)
        {
            string key = NormalizeLower(keys[i]);
            if (!string.IsNullOrEmpty(key) && normalizedText.Contains(key))
                return true;
        }

        return false;
    }

    private static string NormalizeLower(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.ToLowerInvariant();
    }
}
