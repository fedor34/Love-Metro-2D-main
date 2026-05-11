using UnityEngine;

/// <summary>
/// ScriptableObject with the shared passenger tuning and physics values.
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

    [Tooltip("Cooldown before additional collision checks can run again.")]
    public float additionalCollisionCheckTimePeriod = 2f;

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

    [Header("Rigidbody")]
    [Tooltip("Collision detection mode assigned to passenger rigidbodies.")]
    public CollisionDetectionMode2D collisionDetectionMode = CollisionDetectionMode2D.Continuous;

    [Tooltip("Interpolation mode assigned to passenger rigidbodies.")]
    public RigidbodyInterpolation2D interpolation = RigidbodyInterpolation2D.Interpolate;

    [Tooltip("Whether passenger rigidbody rotation is frozen.")]
    public bool freezeRotation = true;

    [Tooltip("Gravity scale assigned to passenger rigidbodies.")]
    public float gravityScale = 0f;

    [Tooltip("Linear damping used while the passenger is walking or standing.")]
    public float defaultLinearDamping = 0.2f;

    [Tooltip("Angular damping used while the passenger is walking or standing.")]
    public float defaultAngularDamping = 0.2f;

    [Tooltip("Linear damping used while the passenger is airborne.")]
    public float airborneLinearDamping = 0.02f;

    [Tooltip("Angular damping used while the passenger is airborne.")]
    public float airborneAngularDamping = 0.02f;

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

    public static PassengerSettings Resolve(PassengerSettings overrideSettings)
    {
        return overrideSettings != null ? overrideSettings : Default;
    }

    private void OnValidate()
    {
        globalSpeedMultiplier = Mathf.Clamp(globalSpeedMultiplier, 0.1f, 2f);
        baseSpeed = Mathf.Max(0f, baseSpeed);
        minFallingSpeed = Mathf.Max(0f, minFallingSpeed);
        additionalCollisionCheckTimePeriod = Mathf.Max(0f, additionalCollisionCheckTimePeriod);

        handrailGrabChance = Mathf.Clamp01(handrailGrabChance);
        handrailMinGrabbingSpeed = Mathf.Max(0f, handrailMinGrabbingSpeed);
        handrailCooldown = Mathf.Max(0f, handrailCooldown);
        handrailStandingTimeInterval.x = Mathf.Max(0f, handrailStandingTimeInterval.x);
        handrailStandingTimeInterval.y = Mathf.Max(handrailStandingTimeInterval.x, handrailStandingTimeInterval.y);

        launchSensitivity = Mathf.Max(0.05f, launchSensitivity);
        minImpulseToLaunch = Mathf.Max(0f, minImpulseToLaunch);
        impulseToVelocityScale = Mathf.Max(0.01f, impulseToVelocityScale);
        globalImpulseScale = Mathf.Clamp(globalImpulseScale, 0.01f, 2f);
        maxFlightSpeed = Mathf.Max(0.01f, maxFlightSpeed);
        flightSpeedMultiplier = Mathf.Clamp(flightSpeedMultiplier, 0.01f, 2f);
        flightDeceleration = Mathf.Max(0f, flightDeceleration);
        maxBounces = Mathf.Max(1, maxBounces);
        bounceElasticity = Mathf.Clamp01(bounceElasticity);
        wallBounceBoost = Mathf.Max(0f, wallBounceBoost);

        easeOutMinK = Mathf.Clamp(easeOutMinK, 0.9f, 1f);
        easeOutMaxK = Mathf.Clamp(easeOutMaxK, easeOutMinK, 1f);
        aimAssistRadius = Mathf.Max(0f, aimAssistRadius);
        aimAssistMaxStrength = Mathf.Max(0f, aimAssistMaxStrength);
        turbulenceStrength = Mathf.Max(0f, turbulenceStrength);
        angleSnapDeg = Mathf.Max(0f, angleSnapDeg);

        magnetRadius = Mathf.Max(0f, magnetRadius);
        magnetForce = Mathf.Max(0f, magnetForce);
        repelRadius = Mathf.Max(0f, repelRadius);
        repelForce = Mathf.Max(0f, repelForce);
        rematchCooldown = Mathf.Max(0f, rematchCooldown);
        uniformLaunchScale = Mathf.Max(0.01f, uniformLaunchScale);
        uniformLaunchGamma = Mathf.Max(0.01f, uniformLaunchGamma);
        flightHorizontalScale = Mathf.Max(0f, flightHorizontalScale);
        flightVerticalScale = Mathf.Max(0f, flightVerticalScale);
        flightVerticalGamma = Mathf.Max(0.01f, flightVerticalGamma);
        minWindStrengthForFlying = Mathf.Max(0f, minWindStrengthForFlying);
        maxFlyingTime = Mathf.Max(0.01f, maxFlyingTime);

        defaultLinearDamping = Mathf.Max(0f, defaultLinearDamping);
        defaultAngularDamping = Mathf.Max(0f, defaultAngularDamping);
        airborneLinearDamping = Mathf.Max(0f, airborneLinearDamping);
        airborneAngularDamping = Mathf.Max(0f, airborneAngularDamping);
    }
}
