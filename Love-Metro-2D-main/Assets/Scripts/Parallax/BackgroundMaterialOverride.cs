using UnityEngine;

public class BackgroundMaterialOverride : MonoBehaviour
{
    [SerializeField] private bool _replaceParallaxMaterials = true;
    [SerializeField] private string _newShaderName = "Sprites/Default";

    private void Awake()
    {
        if (!_replaceParallaxMaterials) return;
        var shader = Shader.Find(_newShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"[BackgroundMaterialOverride] Shader '{_newShaderName}' not found.");
            return;
        }

        int replaced = 0;
        foreach (var r in FindObjectsOfType<SpriteRenderer>(true))
        {
            var mat = r.sharedMaterial;
            string matName = mat != null ? mat.name.ToLower() : string.Empty;
            string shName = (mat != null && mat.shader != null) ? mat.shader.name.ToLower() : string.Empty;
            if (matName.Contains("parallax") || shName.Contains("parallax"))
            {
                var newMat = new Material(shader);
                r.material = newMat; // instance material without parallax
                var pl = r.GetComponent<ParallaxLayer>();
                if (pl != null) pl.enabled = false;
                replaced++;
            }
        }
        if (replaced > 0)
            Debug.Log($"[BackgroundMaterialOverride] Replaced {replaced} parallax material(s) with '{_newShaderName}'.");
    }
}