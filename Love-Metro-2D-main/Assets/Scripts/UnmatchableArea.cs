using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnmatchableArea : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.TryGetComponent<WandererNew>(out WandererNew wanderer))
        {
            wanderer.IsMatchable = false;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<WandererNew>(out WandererNew wanderer))
        {
            wanderer.IsMatchable = true;
        }
    }
}
