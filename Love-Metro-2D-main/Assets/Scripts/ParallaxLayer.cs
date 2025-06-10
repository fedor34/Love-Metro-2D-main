using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    // Список спрайтов слоя, который будет дублироваться при движении
    private List<Transform> renderers;
    // Префаб, который используется для клонирования спрайтов слоя
    private GameObject LayerPref;

    // Скорость движения слоя, задаёт эффект параллакса
    public float Speed;

}
