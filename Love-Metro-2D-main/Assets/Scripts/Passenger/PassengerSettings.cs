using UnityEngine;

/// <summary>
/// ScriptableObject содержащий все настройки пассажира.
/// Заменяет magic numbers на конфигурируемые значения.
/// Создать ассет: Assets -> Create -> Love Metro -> Passenger Settings
/// </summary>
[CreateAssetMenu(fileName = "PassengerSettings", menuName = "Love Metro/Passenger Settings")]
public class PassengerSettings : ScriptableObject
{
    [Header("=== Базовое движение ===")]
    [Tooltip("Глобальный множитель скорости всех пассажиров")]
    [Range(0.1f, 2f)]
    public float globalSpeedMultiplier = 0.7f;

    [Tooltip("Базовая скорость ходьбы")]
    public float baseSpeed = 2f;

    [Tooltip("Минимальная скорость для остановки полёта")]
    public float minFallingSpeed = 0.5f;

    [Header("=== Поручни ===")]
    [Tooltip("Шанс схватиться за поручень (0-1)")]
    [Range(0f, 1f)]
    public float handrailGrabChance = 0.3f;

    [Tooltip("Минимальная скорость для схватывания за поручень")]
    public float handrailMinGrabbingSpeed = 1f;

    [Tooltip("Кулдаун между схватываниями за поручень")]
    public float handrailCooldown = 0.5f;

    [Tooltip("Время стояния на поручне (min, max)")]
    public Vector2 handrailStandingTimeInterval = new Vector2(1f, 3f);

    [Header("=== Импульс от поезда ===")]
    [Tooltip("Чувствительность к импульсу поезда")]
    public float launchSensitivity = 1.0f;

    [Tooltip("Минимальный импульс для запуска в полёт")]
    public float minImpulseToLaunch = 3.0f;

    [Tooltip("Масштаб конвертации импульса в скорость")]
    public float impulseToVelocityScale = 0.45f;

    [Tooltip("Глобальный масштаб импульсов")]
    public float globalImpulseScale = 0.8f;

    [Header("=== Полёт (Flight/Falling) ===")]
    [Tooltip("Максимальная скорость полёта")]
    public float maxFlightSpeed = 18f;

    [Tooltip("Множитель скорости полёта")]
    public float flightSpeedMultiplier = 0.7f;

    [Tooltip("Замедление полёта")]
    public float flightDeceleration = 0.65f;

    [Tooltip("Максимальное количество отскоков")]
    public int maxBounces = 3;

    [Tooltip("Упругость при отскоке от стен (0-1)")]
    [Range(0f, 1f)]
    public float bounceElasticity = 0.95f;

    [Tooltip("Ускорение при ударе о стену")]
    public float wallBounceBoost = 1.0f;

    [Header("=== Ease-Out затухание ===")]
    [Tooltip("Минимальный коэффициент затухания (при низкой скорости)")]
    [Range(0.9f, 1f)]
    public float easeOutMinK = 0.985f;

    [Tooltip("Максимальный коэффициент затухания (при высокой скорости)")]
    [Range(0.9f, 1f)]
    public float easeOutMaxK = 0.9985f;

    [Header("=== Aim Assist ===")]
    [Tooltip("Радиус поиска цели для прицела")]
    public float aimAssistRadius = 5.0f;

    [Tooltip("Максимальная сила притяжения к цели")]
    public float aimAssistMaxStrength = 1.2f;

    [Tooltip("Сила турбулентности/шума")]
    public float turbulenceStrength = 0.8f;

    [Tooltip("Привязка угла в градусах")]
    public float angleSnapDeg = 10f;

    [Header("=== Магнитное притяжение ===")]
    [Tooltip("Радиус магнитного притяжения к противоположному полу")]
    public float magnetRadius = 3.5f;

    [Tooltip("Сила магнитного притяжения")]
    public float magnetForce = 5.0f;

    [Tooltip("Радиус отталкивания от своего пола")]
    public float repelRadius = 2.0f;

    [Tooltip("Сила отталкивания")]
    public float repelForce = 4.0f;

    [Header("=== Matching ===")]
    [Tooltip("Кулдаун перед повторным matching после разрыва")]
    public float rematchCooldown = 0.35f;

    [Header("=== Масштабирование запуска ===")]
    [Tooltip("Равномерный масштаб силы запуска")]
    public float uniformLaunchScale = 1.8f;

    [Tooltip("Гамма для нелинейного масштабирования")]
    public float uniformLaunchGamma = 0.75f;

    [Tooltip("Масштаб горизонтальной составляющей в полёте")]
    public float flightHorizontalScale = 0.48f;

    [Tooltip("Масштаб вертикальной составляющей в полёте")]
    public float flightVerticalScale = 2.88f;

    [Tooltip("Гамма вертикальной составляющей")]
    public float flightVerticalGamma = 0.65f;

    [Header("=== Ветер и Flying state ===")]
    [Tooltip("Минимальная сила ветра для перехода в Flying")]
    public float minWindStrengthForFlying = 8f;

    [Tooltip("Максимальное время полёта в состоянии Flying")]
    public float maxFlyingTime = 5f;

    [Header("=== Слои (Layers) ===")]
    public string fallingLayer = "Falling";
    public string defaultLayer = "Default";

    /// <summary>
    /// Синглтон для быстрого доступа к дефолтным настройкам.
    /// Настоящий ассет должен быть назначен через инспектор.
    /// </summary>
    private static PassengerSettings _default;
    public static PassengerSettings Default
    {
        get
        {
            if (_default == null)
            {
                _default = Resources.Load<PassengerSettings>("PassengerSettings");
                if (_default == null)
                {
                    Debug.LogWarning("PassengerSettings not found in Resources. Creating default instance.");
                    _default = CreateInstance<PassengerSettings>();
                }
            }
            return _default;
        }
    }
}
