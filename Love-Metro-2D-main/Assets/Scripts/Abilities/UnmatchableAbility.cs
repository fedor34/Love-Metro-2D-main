using UnityEngine;

/// <summary>
/// Passengers with this ability cannot form a couple.
/// When they collide with an opposite-sex passenger, both bounce away and a penalty is applied.
/// </summary>
[CreateAssetMenu(menuName = "Passengers/Ability/Unmatchable", fileName = "UnmatchableAbility")]
public class UnmatchableAbility : PassengerAbility
{
    [Header("Penalty settings")]
    [Tooltip("Минимальный штраф за столкновение")] public int penaltyMin = 20;
    [Tooltip("Максимальный штраф за столкновение")] public int penaltyMax = 50;

    [Header("Visual (optional)")]
    public Color tint = new Color(1f, 0.8f, 0.8f, 1f);

    public override void OnAttach(Passenger self)
    {
        // Важное изменение: больше НЕ запрещаем матчинг глобально.
        // Пассажир с этой способностью может образовать пару с обычным противоположного пола.
        // Блокировка и штраф происходят только при столкновении двух Unmatchable.

        // Небольшой визуальный намёк (опционально)
        var sr = self.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = tint;
    }
}
