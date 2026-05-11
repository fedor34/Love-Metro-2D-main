using System.Collections.Generic;
using UnityEngine;

public class PassangersContainer : MonoBehaviour
{
    [SerializeField] public List<Passenger> Passangers;

    public IReadOnlyList<Passenger> Passengers => EnsurePassengers();
    public int Count => EnsurePassengers().Count;

    public void AddPassenger(Passenger passenger)
    {
        if (passenger == null)
            return;

        List<Passenger> passengers = EnsurePassengers();
        if (!passengers.Contains(passenger))
        {
            passengers.Add(passenger);
            passenger.container = this;
        }
    }

    public bool RemovePassenger(Passenger passenger)
    {
        if (passenger == null || Passangers == null)
            return false;

        return Passangers.Remove(passenger);
    }

    public void ClearPassengers()
    {
        EnsurePassengers().Clear();
    }

    public void RemovePassanger(Passenger p)
    {
        RemovePassenger(p);
    }

    /// <summary>
    /// Удаляет все null-ссылки из списка
    /// </summary>
    public void CleanupNullReferences()
    {
        Passangers?.RemoveAll(p => p == null);
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

    private List<Passenger> EnsurePassengers()
    {
        if (Passangers == null)
            Passangers = new List<Passenger>();

        return Passangers;
    }
}
