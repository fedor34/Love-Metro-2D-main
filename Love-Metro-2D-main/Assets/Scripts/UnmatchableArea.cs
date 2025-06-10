using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Зона, в которой пассажиры не могут образовать пару.
/// Используется для ограничения матчмейкинга в определённых участках поезда.
/// </summary>
public class UnmatchableArea : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        // Пока персонаж внутри зоны, запрещаем ему подбираться в пару
        if (collision.TryGetComponent<WandererNew>(out WandererNew wanderer))
        {
            wanderer.IsMatchable = false;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        // При выходе из зоны снова разрешаем создавать пару
        if (collision.TryGetComponent<WandererNew>(out WandererNew wanderer))
        {
            wanderer.IsMatchable = true;
        }
    }
}
