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
}
