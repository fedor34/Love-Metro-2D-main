using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Настройки параллакса")]
    [SerializeField] private float _parallaxSpeed = 1f;
    [SerializeField] private bool _useTrainSpeed = true;
    [SerializeField] private Vector2 _scrollDirection = Vector2.left;
    
    private List<Transform> renderers;
    private GameObject LayerPref;
    private Material _material;
    private Vector2 _offset;

    public float Speed; // Оставляем для совместимости
    
    private void Start()
    {
        // Получаем материал спрайта
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.material != null)
        {
            _material = spriteRenderer.material;
        }
    }
    
    /// <summary>
    /// Обновляет слой параллакса с заданной скоростью
    /// </summary>
    /// <param name="trainSpeed">Скорость поезда</param>
    public void UpdateLayer(float trainSpeed)
    {
        if (!_useTrainSpeed || _material == null) return;
        
        // Вычисляем смещение на основе скорости поезда
        float effectiveSpeed = trainSpeed * _parallaxSpeed;
        _offset += _scrollDirection * effectiveSpeed * Time.deltaTime;
        
        // Применяем смещение к материалу
        _material.SetVector("_Offset", _offset);
        
        // Также можем использовать mainTextureOffset для простых материалов
        _material.mainTextureOffset = _offset;
    }
    
    /// <summary>
    /// Устанавливает скорость параллакса
    /// </summary>
    public void SetParallaxSpeed(float speed)
    {
        _parallaxSpeed = speed;
    }
    
    /// <summary>
    /// Включает/выключает использование скорости поезда
    /// </summary>
    public void SetUseTrainSpeed(bool use)
    {
        _useTrainSpeed = use;
    }
}
