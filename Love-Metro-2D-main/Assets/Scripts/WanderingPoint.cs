using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Убираем принудительное требование SpriteRenderer, так как он нужен только для отладки
public class WanderingPoint : MonoBehaviour
{
    public bool IsOccupied;
    public bool IsUnderHandrail;

    public void Ping() 
    { 
        // Проверяем наличие SpriteRenderer перед использованием
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.yellow;
    }

    private void Start()
    {
        // Убираем неиспользуемую переменную
    }
}
