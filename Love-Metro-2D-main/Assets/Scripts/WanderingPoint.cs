using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WanderingPoint : MonoBehaviour
{
    // Является ли точка в данный момент занятой другим персонажем
    public bool IsOccupied;
    // Находится ли точка под поручнем (для вероятности захвата)
    public bool IsUnderHandrail;

    /// <summary>
    /// Визуальный пинг для отладки, подсвечивает точку
    /// </summary>
    public void Ping()
    {
        GetComponent<SpriteRenderer>().color = Color.yellow;
    }

    private void Start()
    {
        // Кэшируем спрайт если понадобится для других эффектов
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
