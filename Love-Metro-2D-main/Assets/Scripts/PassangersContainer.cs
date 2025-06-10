using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Контейнер для хранения всех пассажиров на сцене.
/// Позволяет удалять их из списка при уничтожении.
/// </summary>
public class PassangersContainer : MonoBehaviour
{
    [SerializeField] public List<WandererNew> Passangers;

    /// <summary>
    /// Удаляет пассажира из списка, если он там присутствует
    /// </summary>
    public void RemovePassanger(WandererNew p)
    {
        if (Passangers.Contains(p))
            Passangers.Remove(p);
    }
}
