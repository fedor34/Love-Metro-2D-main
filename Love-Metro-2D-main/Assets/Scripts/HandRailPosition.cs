using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandRailPosition : MonoBehaviour
{
    public bool IsOccupied;

    public void ReleaseHandrail()
    {
        IsOccupied = false;
    }
}
