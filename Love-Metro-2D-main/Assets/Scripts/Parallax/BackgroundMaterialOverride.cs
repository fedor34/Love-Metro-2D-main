using UnityEngine;

public class BackgroundMaterialOverride : MonoBehaviour
{
    [SerializeField] private bool _replaceParallaxMaterials = true;
    [SerializeField] private string _newShaderName = "Sprites/Default";
    [SerializeField] private bool _disableParallaxLayers = true;

    private void Awake()
    {
        ApplyOverride();
    }

    private void ApplyOverride()
    {
        if (!_replaceParallaxMaterials)
            return;

        Shader shader = ResolveReplacementShader();
        if (shader == null)
            return;

        int replaced = 0;
        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!ShouldReplaceRenderer(renderers[i]))
                continue;

            ReplaceRendererMaterial(renderers[i], shader);
            replaced++;
        }

        if (replaced > 0)
        {
            Diagnostics.Log(
                $"[BackgroundMaterialOverride] Replaced {replaced} parallax material(s) with '{_newShaderName}'.");
        }
    }

    private Shader ResolveReplacementShader()
    {
        Shader shader = Shader.Find(_newShaderName);
        if (shader == null)
            Diagnostics.Warn($"[BackgroundMaterialOverride] Shader '{_newShaderName}' not found.");

        return shader;
    }

    private static bool ShouldReplaceRenderer(SpriteRenderer renderer)
    {
        return renderer != null && ParallaxRendererClassifier.LooksLikeParallaxMaterial(renderer.sharedMaterial);
    }

    private void ReplaceRendererMaterial(SpriteRenderer renderer, Shader shader)
    {
        if (renderer == null || shader == null)
            return;

        renderer.material = new Material(shader);
        DisableParallaxLayerIfNeeded(renderer);
    }

    private void DisableParallaxLayerIfNeeded(SpriteRenderer renderer)
    {
        if (!_disableParallaxLayers || renderer == null)
            return;

        ParallaxLayer layer = renderer.GetComponent<ParallaxLayer>();
        if (layer != null)
            layer.enabled = false;
    }
}
