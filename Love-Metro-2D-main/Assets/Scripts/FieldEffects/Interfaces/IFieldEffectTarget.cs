using UnityEngine;

/// <summary>
/// Интерфейс для объектов, на которые могут влиять эффекты поля
/// </summary>
public interface IFieldEffectTarget
{
    /// <summary>
    /// Применить силу к объекту
    /// </summary>
    /// <param name="force">Вектор силы</param>
    /// <param name="effectType">Тип эффекта</param>
    void ApplyFieldForce(Vector2 force, FieldEffectType effectType);
    
    /// <summary>
    /// Применить силу к объекту с режимом силы
    /// </summary>
    /// <param name="force">Вектор силы</param>
    /// <param name="forceMode">Режим применения силы</param>
    void ApplyFieldForce(Vector3 force, ForceMode2D forceMode);
    
    /// <summary>
    /// Получить позицию объекта
    /// </summary>
    /// <returns>Позиция в мировых координатах</returns>
    Vector3 GetPosition();
    
    /// <summary>
    /// Получить Rigidbody2D объекта
    /// </summary>
    /// <returns>Компонент Rigidbody2D</returns>
    Rigidbody2D GetRigidbody();
    
    /// <summary>
    /// Проверить, может ли объект быть подвержен эффекту
    /// </summary>
    /// <param name="effectType">Тип эффекта</param>
    /// <returns>True если объект может быть подвержен эффекту</returns>
    bool CanBeAffectedBy(FieldEffectType effectType);
    
    /// <summary>
    /// Уведомить об входе в зону эффекта
    /// </summary>
    /// <param name="effect">Эффект поля</param>
    void OnEnterFieldEffect(IFieldEffect effect);
    
    /// <summary>
    /// Уведомить о выходе из зоны эффекта
    /// </summary>
    /// <param name="effect">Эффект поля</param>
    void OnExitFieldEffect(IFieldEffect effect);
} 