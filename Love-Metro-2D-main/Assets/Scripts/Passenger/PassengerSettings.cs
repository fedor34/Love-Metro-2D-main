using UnityEngine;

/// <summary>
/// ScriptableObject with the shared passenger tuning values.
/// Create an asset via Assets -> Create -> Love Metro -> Passenger Settings.
/// </summary>
[CreateAssetMenu(fileName = "PassengerSettings", menuName = "Love Metro/Passenger Settings")]
public class PassengerSettings : ScriptableObject
{
    [Header("Base Movement")]
    [Tooltip("Global speed multiplier applied to all passengers.")]
    [Range(0.1f, 2f)]
    public float globalSpeedMultiplier = 0.7f;

    [Tooltip("Base walking speed.")]
    public float baseSpeed = 2f;

    [Tooltip("Minimum flight speed before the passenger returns to wandering.")]
    public float minFallingSpeed = 0.5f;

    [Header("Handrail")]
    [Tooltip("Chance to grab a handrail.")]
    [Range(0f, 1f)]
    public float handrailGrabChance = 0.3f;

    [Tooltip("Minimum speed required to grab a handrail.")]
    public float handrailMinGrabbingSpeed = 1f;

    [Tooltip("Cooldown between handrail grabs.")]
    public float handrailCooldown = 0.5f;

    [Tooltip("Minimum and maximum time spent on a handrail.")]
    public Vector2 handrailStandingTimeInterval = new Vector2(1f, 3f);

    [Header("Train Impulse")]
    [Tooltip("How strongly the passenger reacts to train inertia.")]
    public float launchSensitivity = 1.0f;

    [Tooltip("Minimum impulse needed to launch into falling.")]
    public float minImpulseToLaunch = 3.0f;

    [Tooltip("Scale factor that converts impulse into initial flight speed.")]
    public float impulseToVelocityScale = 0.45f;

    [Tooltip("Global scale applied to all impulses.")]
    public float globalImpulseScale = 0.8f;

    [Header("Flight")]
    [Tooltip("Maximum flight speed.")]
    public float maxFlightSpeed = 18f;

    [Tooltip("Overall multiplier applied to flight speed.")]
    public float flightSpeedMultiplier = 0.7f;

    [Tooltip("How quickly flight slows down over time.")]
    public float flightDeceleration = 0.65f;

    [Tooltip("Maximum number of bounces before returning to wandering.")]
    public int maxBounces = 3;

    [Tooltip("Wall bounce elasticity.")]
    [Range(0f, 1f)]
    public float bounceElasticity = 0.95f;

    [Tooltip("Extra speed multiplier applied after wall bounces.")]
    public float wallBounceBoost = 1.0f;

    [Header("Ease-Out")]
    [Tooltip("Minimum ease-out coefficient at low speed.")]
    [Range(0.9f, 1f)]
    public float easeOutMinK = 0.985f;

    [Tooltip("Maximum ease-out coefficient at high speed.")]
    [Range(0.9f, 1f)]
    public float easeOutMaxK = 0.9985f;

    [Header("Aim Assist")]
    [Tooltip("Search radius for aim assist targets.")]
    public float aimAssistRadius = 5.0f;

    [Tooltip("Maximum aim assist strength.")]
    public float aimAssistMaxStrength = 1.2f;

    [Tooltip("Turbulence/noise strength.")]
    public float turbulenceStrength = 0.8f;

    [Tooltip("Angle snap value in degrees.")]
    public float angleSnapDeg = 10f;

    [Header("Magnet")]
    [Tooltip("Radius used to attract a passenger toward the opposite gender.")]
    public float magnetRadius = 3.5f;

    [Tooltip("Magnet attraction strength.")]
    public float magnetForce = 5.0f;

    [Tooltip("Repulsion radius for passengers of the same gender.")]
    public float repelRadius = 2.0f;

    [Tooltip("Repulsion strength.")]
    public float repelForce = 4.0f;

    [Header("Matching")]
    [Tooltip("Cooldown before the passenger can match again after a breakup.")]
    public float rematchCooldown = 0.35f;

    [Header("Launch Shape")]
    [Tooltip("Base scale used when converting click direction into launch impulse.")]
    public float uniformLaunchScale = 1.8f;

    [Tooltip("Gamma used for the click-direction launch curve.")]
    public float uniformLaunchGamma = 0.75f;

    [Tooltip("Horizontal scale used for train impulse while falling.")]
    public float flightHorizontalScale = 0.48f;

    [Tooltip("Vertical scale used for train impulse while falling.")]
    public float flightVerticalScale = 2.88f;

    [Tooltip("Gamma used for the falling vertical impulse curve.")]
    public float flightVerticalGamma = 0.65f;

    [Header("Wind")]
    [Tooltip("Minimum wind strength required to enter the flying state.")]
    public float minWindStrengthForFlying = 8f;

    [Tooltip("Maximum time allowed in the flying state.")]
    public float maxFlyingTime = 5f;

    [Header("Layers")]
    [Tooltip("Default layer assigned to the passenger.")]
    public string defaultLayer = "Default";

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
