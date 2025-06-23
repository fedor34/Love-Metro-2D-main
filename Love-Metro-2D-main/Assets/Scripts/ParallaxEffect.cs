using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Параллакс настройки")]
    [SerializeField] private ParallaxLayer[] _parallaxLayers;
    [SerializeField] private float _speedMultiplier = 1f;
    [SerializeField] private bool _updateViaReflection = false; // Если true, старое поведение, иначе используем только SetTrainSpeed
    
    private TrainManager _trainManager;
    
    private void Start()
    {
        _trainManager = FindObjectOfType<TrainManager>();
        
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
        
        // Получаем текущую скорость поезда через reflection
        float trainSpeed = GetTrainSpeed();
        
        // Обновляем каждый слой параллакса
        foreach (var layer in _parallaxLayers)
        {
            if (layer != null && layer.transform != null)
            {
                layer.UpdateLayer(trainSpeed * _speedMultiplier);
            }
        }
    }
    
    private float GetTrainSpeed()
    {
        // Используем рефлексию для получения приватного поля _currentSpeed
        var speedField = typeof(TrainManager).GetField("_currentSpeed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (speedField != null)
        {
            return (float)speedField.GetValue(_trainManager);
        }
        
        return 0f;
    }
    
    private void InitializeDefaultLayers()
    {
        // Находим все объекты с ParallaxLayer компонентами
        var foundLayers = FindObjectsOfType<ParallaxLayer>();
        _parallaxLayers = foundLayers;
        
        Debug.Log($"[ParallaxEffect] Найдено {foundLayers.Length} слоев параллакса");
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
