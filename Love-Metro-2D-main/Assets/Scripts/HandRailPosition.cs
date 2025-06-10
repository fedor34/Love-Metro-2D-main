using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Точка захвата за поручень. Используется персонажами при падении
/// или во время стояния под поручнем.
/// </summary>
public class HandRailPosition : MonoBehaviour
{
    // Флаг, занята ли точка другим пассажиром
    public bool IsOccupied;

    /// <summary>
    /// Освобождает поручень для следующего пассажира
    /// </summary>
    public void ReleaseHandrail()
    {
        IsOccupied = false;
    }
}
