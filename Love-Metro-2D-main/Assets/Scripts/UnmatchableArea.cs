using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnmatchableArea : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Passenger>(out Passenger passenger))
        {
            passenger.IsMatchable = false;
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Passenger>(out Passenger passenger))
        {
            passenger.IsMatchable = true;
        }
    }
}
