using UnityEngine;

/// <summary>
/// Типы эффектов поля
/// </summary>
public enum FieldEffectType
{
    // Эффекты движения
    Gravity,        // Притяжение к центру
    Repulsion,      // Отталкивание от центра
    Wind,           // Постоянная сила в направлении
    Magnetic,       // Притяжение только определенных объектов
    Vortex,         // Закручивающая сила
    
    // Модификаторы движения
    Slowdown,       // Замедление
    Speedup,        // Ускорение
    Friction,       // Трение
    Bounce,         // Отскок
    
    // Триггерные эффекты
    Teleport,       // Телепортация
    Checkpoint,     // Точка сохранения
    Activator,      // Активация других объектов
    
    // Специальные эффекты
    Shield,         // Защита от других эффектов
    Multiplier,     // Усиление других эффектов
    
    // Визуальные и звуковые
    Visual,         // Чисто визуальный эффект
    Audio,          // Звуковой эффект
    
    Custom          // Пользовательский эффект
}

/// <summary>
/// Данные эффекта поля
/// </summary>
[System.Serializable]
public class FieldEffectData
{
    [Header("Основные параметры")]
    public FieldEffectType effectType;
    public float strength = 1f;
    public float radius = 5f;
    public Vector3 center = Vector3.zero;
    
    [Header("Настройки применения")]
    public bool affectsFalling = true;
    public bool affectsWandering = true;
    public bool affectsHandrail = false;
    public bool affectsMatching = false;
    
    [Header("Дополнительные параметры")]
    public Vector3 direction = Vector3.zero;    // Направление для Wind эффектов
    public float minStrength = 0f;             // Минимальная сила
    public float maxStrength = 10f;            // Максимальная сила
    public bool useInverseSquare = false;      // Использовать закон обратных квадратов
    public AnimationCurve strengthCurve;       // Кривая силы по расстоянию
    
    [Header("Временные параметры")]
    public float duration = -1f;               // Продолжительность (-1 = бесконечно)
    public float delay = 0f;                   // Задержка перед активацией
    public bool isPulsing = false;             // Пульсирующий эффект
    public float pulseFrequency = 1f;          // Частота пульсации
    public float pulseAmplitude = 0.5f;        // Амплитуда пульсации
    
    [Header("Условия активации")]
    public LayerMask affectedLayers = -1;      // Слои объектов, на которые действует эффект
    public string[] requiredTags;             // Необходимые теги
    public string[] excludedTags;             // Исключенные теги
    
    [Header("Композиция эффектов")]
    public bool stackable = true;              // Может ли складываться с другими эффектами
    public int priority = 0;                   // Приоритет эффекта
    public bool overrideOtherEffects = false; // Переопределяет другие эффекты
    
    [Header("Физические параметры")]
    public ForceMode2D forceMode = ForceMode2D.Force;
    public bool respectMass = true;            // Учитывать массу объекта
    public float massMultiplier = 1f;          // Множитель массы
    
    [Header("Визуализация")]
    public bool showVisualEffect = true;
    public Color effectColor = Color.yellow;
    public GameObject particleEffectPrefab;
    public bool showDebugInfo = false;
    
    /// <summary>
    /// Конструктор по умолчанию
    /// </summary>
    public FieldEffectData()
    {
        effectType = FieldEffectType.Gravity;
        strength = 1f;
        radius = 5f;
        center = Vector3.zero;
        InitializeDefaults();
    }
    
    /// <summary>
    /// Конструктор с основными параметрами
    /// </summary>
    public FieldEffectData(FieldEffectType type, float str, float rad, Vector3 pos)
    {
        effectType = type;
        strength = str;
        radius = rad;
        center = pos;
        InitializeDefaults();
    }
    
    /// <summary>
    /// Полный конструктор
    /// </summary>
    public FieldEffectData(FieldEffectType type, float str, float rad, Vector3 pos, Vector3 dir)
    {
        effectType = type;
        strength = str;
        radius = rad;
        center = pos;
        direction = dir;
        InitializeDefaults();
    }
    
    private void InitializeDefaults()
    {
        // Инициализируем кривую силы по умолчанию
        if (strengthCurve == null)
        {
            strengthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        }
        
        // Устанавливаем параметры по умолчанию в зависимости от типа эффекта
        SetDefaultsForEffectType();
    }
    
    private void SetDefaultsForEffectType()
    {
        switch (effectType)
        {
            case FieldEffectType.Gravity:
                forceMode = ForceMode2D.Force;
                respectMass = true;
                effectColor = Color.yellow;
                break;
                
            case FieldEffectType.Repulsion:
                forceMode = ForceMode2D.Force;
                respectMass = true;
                effectColor = Color.red;
                break;
                
            case FieldEffectType.Wind:
                forceMode = ForceMode2D.Force;
                respectMass = false;
                effectColor = Color.cyan;
                break;
                
            case FieldEffectType.Magnetic:
                forceMode = ForceMode2D.Force;
                respectMass = true;
                useInverseSquare = true;
                effectColor = Color.magenta;
                break;
                
            case FieldEffectType.Vortex:
                forceMode = ForceMode2D.Force;
                respectMass = false;
                effectColor = Color.blue;
                break;
                
            case FieldEffectType.Slowdown:
                forceMode = ForceMode2D.Impulse;
                respectMass = false;
                effectColor = new Color(1f, 0.5f, 0f); // Orange color
                break;
                
            case FieldEffectType.Speedup:
                forceMode = ForceMode2D.Impulse;
                respectMass = false;
                effectColor = Color.green;
                break;
                
            case FieldEffectType.Teleport:
                forceMode = ForceMode2D.Impulse;
                respectMass = false;
                stackable = false;
                effectColor = Color.white;
                break;
        }
    }
    
    /// <summary>
    /// Получить эффективную силу на определенном расстоянии
    /// </summary>
    public float GetEffectiveStrength(float distance)
    {
        if (distance > radius) return 0f;
        
        float baseStrength = strength;
        
        // Применяем пульсацию
        if (isPulsing)
        {
            float pulseValue = Mathf.Sin(Time.time * pulseFrequency * 2f * Mathf.PI) * pulseAmplitude;
            baseStrength += pulseValue;
        }
        
        // Применяем кривую силы
        float normalizedDistance = distance / radius;
        float curveMultiplier = strengthCurve.Evaluate(normalizedDistance);
        
        // Применяем закон обратных квадратов если нужно
        if (useInverseSquare && distance > 0.1f)
        {
            curveMultiplier /= (distance * distance);
        }
        
        float finalStrength = baseStrength * curveMultiplier;
        return Mathf.Clamp(finalStrength, minStrength, maxStrength);
    }
    
    /// <summary>
    /// Проверить, может ли эффект действовать на объект
    /// </summary>
    public bool CanAffectObject(GameObject obj)
    {
        // Проверяем слой
        if (((1 << obj.layer) & affectedLayers) == 0)
            return false;
        
        // Проверяем необходимые теги
        if (requiredTags != null && requiredTags.Length > 0)
        {
            bool hasRequiredTag = false;
            foreach (string tag in requiredTags)
            {
                if (obj.CompareTag(tag))
                {
                    hasRequiredTag = true;
                    break;
                }
            }
            if (!hasRequiredTag) return false;
        }
        
        // Проверяем исключенные теги
        if (excludedTags != null && excludedTags.Length > 0)
        {
            foreach (string tag in excludedTags)
            {
                if (obj.CompareTag(tag))
                    return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Создать копию данных эффекта
    /// </summary>
    public FieldEffectData Clone()
    {
        var clone = new FieldEffectData(effectType, strength, radius, center, direction);
        
        // Копируем все параметры
        clone.minStrength = minStrength;
        clone.maxStrength = maxStrength;
        clone.useInverseSquare = useInverseSquare;
        clone.strengthCurve = new AnimationCurve(strengthCurve.keys);
        
        clone.duration = duration;
        clone.delay = delay;
        clone.isPulsing = isPulsing;
        clone.pulseFrequency = pulseFrequency;
        clone.pulseAmplitude = pulseAmplitude;
        
        clone.affectedLayers = affectedLayers;
        clone.requiredTags = requiredTags?.Clone() as string[];
        clone.excludedTags = excludedTags?.Clone() as string[];
        
        clone.stackable = stackable;
        clone.priority = priority;
        clone.overrideOtherEffects = overrideOtherEffects;
        
        clone.forceMode = forceMode;
        clone.respectMass = respectMass;
        clone.massMultiplier = massMultiplier;
        
        clone.showVisualEffect = showVisualEffect;
        clone.effectColor = effectColor;
        clone.particleEffectPrefab = particleEffectPrefab;
        clone.showDebugInfo = showDebugInfo;
        
        return clone;
    }
} 