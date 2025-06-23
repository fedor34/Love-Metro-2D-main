using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Параллакс настройки")]
    [SerializeField] private ParallaxLayer[] _parallaxLayers;
    [SerializeField] private float _speedMultiplier = 1f;
    [SerializeField] private bool _updateViaReflection = false; // Если true, старое поведение, иначе используем только SetTrainSpeed
    [SerializeField] private TrainManager _trainManager;
    
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
        if (!_updateViaReflection) return; // Обновляемся только если явно включено

        if (_trainManager == null) return;
        
        // Получаем текущую скорость поезда
        float trainSpeed = _trainManager.GetCurrentSpeed();
        
        // Обновляем каждый слой параллакса
        foreach (var layer in _parallaxLayers)
        {
            if (layer != null && layer.transform != null)
            {
                layer.UpdateLayer(trainSpeed * _speedMultiplier);
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
        foreach (var layer in _parallaxLayers)
        {
            if (layer != null)
            {
                layer.UpdateLayer(speed * _speedMultiplier);
            }
        }
    }
}
