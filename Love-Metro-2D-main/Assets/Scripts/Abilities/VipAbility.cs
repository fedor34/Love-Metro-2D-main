using UnityEngine;

/// <summary>
/// VIP: visual override + bonus points on match when both are VIP.
/// </summary>
[CreateAssetMenu(menuName = "Passengers/Ability/VIP", fileName = "VipAbility")]
public class VipAbility : PassengerAbility
{
    [Header("Visual override (optional)")]
    public RuntimeAnimatorController femaleController;
    public RuntimeAnimatorController maleController;

    [Header("Scoring")]
    public int pairBonus = 500;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-link common controllers if fields are empty (editor only)
        if (femaleController == null)
        {
            var aoc = UnityEditor.AssetDatabase.FindAssets("PassangerFemale_VIP t:AnimatorOverrideController");
            if (aoc != null && aoc.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(aoc[0]);
                femaleController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            }
        }
        if (maleController == null)
        {
            var aoc = UnityEditor.AssetDatabase.FindAssets("PassangerMale_VIP t:AnimatorOverrideController");
            if (aoc != null && aoc.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(aoc[0]);
                maleController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            }
        }
    }
#endif

    public override void OnAttach(Passenger self)
    {
        var anim = self.GetComponent<Animator>();
        if (anim == null) return;
        RuntimeAnimatorController ctrl = self.IsFemale ? femaleController : maleController;
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;
    }

    public override void OnMatched(Passenger self, Passenger partner, ref int points)
    {
        // VIP ability always doubles the points for this passenger
        points *= 2;

        // Additional bonus if both partners have VIP ability
        if (partner != null)
        {
            var pr = partner.GetComponent<PassengerAbilities>();
            if (pr != null && pr.HasAbility<VipAbility>())
            {
                points += Mathf.Max(0, pairBonus);
            }
        }
    }
}
