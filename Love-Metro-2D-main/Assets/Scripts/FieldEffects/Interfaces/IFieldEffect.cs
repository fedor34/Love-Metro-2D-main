using UnityEngine;

/// <summary>
/// Базовый интерфейс для всех эффектов поля
/// </summary>
public interface IFieldEffect
{
    /// <summary>
    /// Применить эффект к цели
    /// </summary>
    /// <param name="target">Цель эффекта</param>
    /// <param name="deltaTime">Время с последнего обновления</param>
    void ApplyEffect(IFieldEffectTarget target, float deltaTime);
    
    /// <summary>
    /// Убрать эффект с цели
    /// </summary>
    /// <param name="target">Цель эффекта</param>
    void RemoveEffect(IFieldEffectTarget target);
    
    /// <summary>
    /// Проверить, находится ли цель в зоне действия эффекта
    /// </summary>
    /// <param name="targetPosition">Позиция цели</param>
    /// <returns>True если цель в зоне действия</returns>
    bool IsInEffectZone(Vector3 targetPosition);
    
    /// <summary>
    /// Получить данные эффекта
    /// </summary>
    FieldEffectData GetEffectData();
} 