using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WanderingPoint : MonoBehaviour
{
    public bool IsOccupied;
    public bool IsUnderHandrail;

    public void Ping() 
    { 
        GetComponent<SpriteRenderer>().color = Color.yellow;
    }

    private void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
