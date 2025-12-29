using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassangersContainer : MonoBehaviour
{
    [SerializeField] public List<Passenger> Passangers;

    public void RemovePassanger(Passenger p)
    {
        if (Passangers.Contains(p))
            Passangers.Remove(p);
    }

    /// <summary>
    /// Полностью очищает контейнер: удаляет null-записи и уничтожает живые объекты.
    /// </summary>
    public void DestroyAllPassengers()
    {
        if (Passangers == null) return;

        // Сначала убираем null-ы, затем уничтожаем оставшихся.
        Passangers.RemoveAll(x => x == null);
        for (int i = Passangers.Count - 1; i >= 0; i--)
        {
            var p = Passangers[i];
            if (p == null) continue;
            p.container = null; // предотвращаем повторное удаление в RemoveFromContainerAndDestroy
            Destroy(p.gameObject);
        }
        Passangers.Clear();
    }
}
