using UnityEngine;

/// <summary>
/// Runtime workaround for incorrectly configured side boundaries that use PlatformEffector2D on vertical walls.
/// Disables one-way effectors on the SoftWall layer and ensures attached colliders don't use effectors.
/// Code-only fix: no scene edits required.
/// </summary>
public class BoundaryAutoFix : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyFix()
    {
        int softWall = LayerMask.NameToLayer("SoftWall");
        if (softWall < 0) return;

        var effectors = Object.FindObjectsOfType<PlatformEffector2D>();
        int disabled = 0;
        foreach (var eff in effectors)
        {
            if (eff == null) continue;
            if (eff.gameObject.layer != softWall) continue;

            // Turn off one-way logic for vertical walls
            eff.useOneWay = false;
            eff.enabled = false; // fully disable to behave as a regular collider

            // Ensure colliders on this object are not driven by effectors
            var cols = eff.GetComponents<Collider2D>();
            foreach (var c in cols)
            {
                c.usedByEffector = false;
                c.isTrigger = false;
            }
            disabled++;
        }
        if (disabled > 0)
            Debug.Log($"[BoundaryAutoFix] Disabled {disabled} PlatformEffector2D on SoftWall objects (code-only fix).");
        else
            Debug.Log("[BoundaryAutoFix] No SoftWall effectors found.");
    }
}
