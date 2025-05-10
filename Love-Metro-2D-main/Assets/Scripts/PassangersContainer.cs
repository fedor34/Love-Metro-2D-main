using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassangersContainer : MonoBehaviour
{
    [SerializeField] public List<WandererNew> Passangers;

    public void RemovePassanger(WandererNew p)
    {
        if (Passangers.Contains(p))
            Passangers.Remove(p);
    }
}
