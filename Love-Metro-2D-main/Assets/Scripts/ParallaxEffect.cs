using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Параллакс настройки")]
    [SerializeField] private ParallaxLayer[] _parallaxLayers;
    [SerializeField] private float _speedMultiplier = 1.0f; // единичный множитель, скорость напрямую от поезда
    [SerializeField] private bool _updateViaReflection = true; // Можно оставить включённым — будет использоваться только если внешней скорости давно не было
    [SerializeField] private TrainManager _trainManager;

    [Header("Поведение источника скорости")]
    [SerializeField] private bool _preferExternalSpeed = true; // предпочитать скорость, полученную через SetTrainSpeed
    [SerializeField] private float _externalHoldTime = 0.25f;  // столько времени считаем внешний ввод актуальным
    
    private float _lastSpeed;
    private float _lastExternalSetTime = -999f;
    
    private void Start()
    {
        // Инициализируем слои если они не заданы
        if (_parallaxLayers == null || _parallaxLayers.Length == 0)
        {
            InitializeDefaultLayers();
        }
    }
    
    private void Update()
    {
        // Автовосстановление списка слоёв, если они были добавлены после старта
        if ((_parallaxLayers == null || _parallaxLayers.Length == 0))
        {
            InitializeDefaultLayers();
        }
        
        bool hasFreshExternal = _preferExternalSpeed && (Time.time - _lastExternalSetTime) <= _externalHoldTime;
        if (_updateViaReflection && _trainManager != null && !hasFreshExternal)
        {
            _lastSpeed = _trainManager.GetCurrentSpeed();
        }
        
        // Используем скорость поезда напрямую (игнорируя возможные сериализованные множители)
        float speedForLayers = _lastSpeed;
        foreach (var layer in _parallaxLayers)
        {
            if (layer != null && layer.transform != null)
            {
                layer.UpdateLayer(speedForLayers);
            }
        }
    }
    
    private void InitializeDefaultLayers()
    {
        // Находим все объекты с ParallaxLayer компонентами
        var foundLayers = FindObjectsOfType<ParallaxLayer>();
        _parallaxLayers = foundLayers;
    }
    
    // Публичный метод для установки скорости извне
    public void SetTrainSpeed(float speed)
    {
        _lastSpeed = speed;
        _lastExternalSetTime = Time.time;
        // Используем скорость поезда напрямую
        float speedForLayers = speed;
        foreach (var layer in _parallaxLayers)
        {
            if (layer != null)
            {
                layer.UpdateLayer(speedForLayers);
            }
        }
    }
}
