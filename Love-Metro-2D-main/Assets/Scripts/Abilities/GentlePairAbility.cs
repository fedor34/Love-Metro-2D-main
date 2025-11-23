using UnityEngine;

/// <summary>
/// "Нежная" пара: матч возможен только при низкой относительной скорости.
/// На высокой скорости они просто отскакивают без штрафов.
/// Желательно использовать для пары разного пола (например, д3 + м3).
/// Опционально можно подтянуть спец. контроллеры анимации.
/// </summary>
[CreateAssetMenu(menuName = "Passengers/Ability/Gentle", fileName = "GentlePairAbility")]
public class GentlePairAbility : PassengerAbility
{
    [Header("Match conditions")]
    [Tooltip("Максимальная относительная скорость (ед/с) для образования пары")] 
    public float maxRelativeSpeedToMatch = 3.0f;

    [Header("Visual override (optional)")]
    public RuntimeAnimatorController femaleController;
    public RuntimeAnimatorController maleController;

    public override void OnAttach(Passenger self)
    {
        // Визуальная замена (если задана)
        var anim = self.GetComponent<Animator>();
        if (anim != null)
        {
            var ctrl = self.IsFemale ? femaleController : maleController;
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Автоподхват сгенерированных контроллеров, если поля пусты
        if (femaleController == null)
        {
            var g = UnityEditor.AssetDatabase.FindAssets("PassangerFemale_GENTLE_d3 t:AnimatorOverrideController");
            if (g != null && g.Length > 0)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g[0]);
                femaleController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(p);
            }
        }
        if (maleController == null)
        {
            var g = UnityEditor.AssetDatabase.FindAssets("PassangerMale_GENTLE_m3 t:AnimatorOverrideController");
            if (g != null && g.Length > 0)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g[0]);
                maleController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(p);
            }
        }
    }
#endif
}
