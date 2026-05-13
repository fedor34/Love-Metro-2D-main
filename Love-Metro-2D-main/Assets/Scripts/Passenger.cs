using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(PassangerAnimator), typeof(Collider2D))]
public partial class Passenger : MonoBehaviour, IFieldEffectTarget, LoveMetro.Passengers.IPassengerStateHost
{
    public static float GlobalSpeedMultiplier = 0.7f;

    [Header("Settings (optional)")]
    [Tooltip("Optional ScriptableObject override. When omitted, Resources/PassengerSettings is used.")]
    [SerializeField] private PassengerSettings _settings;

    private delegate void ReleaseHandrail();
    private ReleaseHandrail releaseHandrail;

    [SerializeField] private float _speed;
    [SerializeField] private Vector2 _initialMovingDirection;
    [Range(0, 1)]
    [SerializeField] private float _grabingHandrailChance;
    private Vector2 CurrentMovingDirection;
    [SerializeField] private Vector2 HandrailStandingTimeInterval;
    [SerializeField] private GameObject CouplePref;
    [SerializeField] private float _handrailMinGrabbingSpeed;
    [SerializeField] private float _minFallingSpeed;
    [SerializeField] private float _handrailCooldown = 0;
    [SerializeField] private float _aditionalCollisionCheckTimePeriod;
    [SerializeField] private string _defaultLayer = "Default";

    public bool IsFemale;
    [HideInInspector] public bool IsMatchable = true;
    [HideInInspector] public PassangerAnimator PassangerAnimator;

    [SerializeField] private TrainManager _train;

    private float _timeWithoutHolding;
    private LoveMetro.Passengers.PassengerStateRuntime _stateRuntime;
    private LoveMetro.Passengers.PassengerMotionController _motionController;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private ScoreCounter _scoreCounter;

    private bool _isInitiated = false;

    public PassangersContainer container;

    public bool IsInCouple = false;
    private float _rematchEnableTime = 0f;
    [SerializeField] private float _rematchCooldown = 0.35f;

    [Header("Impulse tuning (train inertia)")]
    [SerializeField] private float _launchSensitivity = 1.0f;
    [SerializeField] private float _minImpulseToLaunch = 3.0f;
    [SerializeField] private float _aimAssistRadius = 5.0f;
    [SerializeField] private float _aimAssistMaxStrength = 1.2f;
    [SerializeField] private float _turbulenceStrength = 0.8f;
    [SerializeField] private float _angleSnapDeg = 10f;
    [SerializeField] private float _impulseToVelocityScale = 0.45f;
    [SerializeField] private float _maxFlightSpeed = 18f;
    [SerializeField] private float _flightSpeedMultiplier = 0.7f;
    [SerializeField] private float _globalImpulseScale = 0.8f;
    [SerializeField] private float _uniformLaunchScale = 1.8f;
    [SerializeField] private float _uniformLaunchGamma = 0.75f;
    [SerializeField] private float _flightHorizontalScale = 0.48f;
    [SerializeField] private float _flightVerticalScale = 2.88f;
    [SerializeField] private float _flightVerticalGamma = 0.65f;
    [SerializeField] private float _minWindStrengthForFlying = 8f;
    [SerializeField] private float _maxFlyingTime = 5f;

    [Header("Billiards-style tuning")]
    [SerializeField] private float _magnetRadius = 3.5f;
    [SerializeField] private float _magnetForce = 5.0f;
    [SerializeField] private float _repelRadius = 2.0f;
    [SerializeField] private float _repelForce = 4.0f;
    [SerializeField] private float _flightDeceleration = 0.65f;
    [SerializeField] private float _bounceElasticity = 0.95f;
    [SerializeField] private float _wallBounceBoost = 1.0f;
    [SerializeField] private int _maxBounces = 3;
    [SerializeField] private float _easeOutMinK = 0.985f;
    [SerializeField] private float _easeOutMaxK = 0.9985f;

    public PassengerSettings Settings => PassengerSettings.Resolve(_settings);

    private void ApplySettingsFromAsset()
    {
        PassengerSettings settings = Settings;

        GlobalSpeedMultiplier = settings.globalSpeedMultiplier;
        _speed = settings.baseSpeed * settings.globalSpeedMultiplier;
        _minFallingSpeed = settings.minFallingSpeed;
        _aditionalCollisionCheckTimePeriod = settings.additionalCollisionCheckTimePeriod;

        _grabingHandrailChance = settings.handrailGrabChance;
        _handrailMinGrabbingSpeed = settings.handrailMinGrabbingSpeed;
        _handrailCooldown = settings.handrailCooldown;
        HandrailStandingTimeInterval = settings.handrailStandingTimeInterval;

        _launchSensitivity = settings.launchSensitivity;
        _minImpulseToLaunch = settings.minImpulseToLaunch;
        _impulseToVelocityScale = settings.impulseToVelocityScale;
        _globalImpulseScale = settings.globalImpulseScale;
        _uniformLaunchScale = settings.uniformLaunchScale;
        _uniformLaunchGamma = settings.uniformLaunchGamma;
        _flightHorizontalScale = settings.flightHorizontalScale;
        _flightVerticalScale = settings.flightVerticalScale;
        _flightVerticalGamma = settings.flightVerticalGamma;
        _minWindStrengthForFlying = settings.minWindStrengthForFlying;
        _maxFlyingTime = settings.maxFlyingTime;

        _maxFlightSpeed = settings.maxFlightSpeed;
        _flightSpeedMultiplier = settings.flightSpeedMultiplier;
        _flightDeceleration = settings.flightDeceleration;
        _maxBounces = settings.maxBounces;
        _bounceElasticity = settings.bounceElasticity;
        _wallBounceBoost = settings.wallBounceBoost;

        _easeOutMinK = settings.easeOutMinK;
        _easeOutMaxK = settings.easeOutMaxK;

        _aimAssistRadius = settings.aimAssistRadius;
        _aimAssistMaxStrength = settings.aimAssistMaxStrength;
        _turbulenceStrength = settings.turbulenceStrength;
        _angleSnapDeg = settings.angleSnapDeg;

        _magnetRadius = settings.magnetRadius;
        _magnetForce = settings.magnetForce;
        _repelRadius = settings.repelRadius;
        _repelForce = settings.repelForce;

        _rematchCooldown = settings.rematchCooldown;
        _defaultLayer = settings.defaultLayer;
    }

    public void Initiate(Vector3 initialMovingDirection, TrainManager train, ScoreCounter scoreCounter)
    {
        ApplySettingsFromAsset();

        _initialMovingDirection = initialMovingDirection;
        CurrentMovingDirection = _initialMovingDirection.normalized;
        _scoreCounter = scoreCounter;

        EnsureRequiredComponents();
        ConfigureMotionController();
        gameObject.layer = LayerMask.NameToLayer(_defaultLayer);

        var spriteRenderer = GetComponent<SpriteRenderer>();
        var animator = GetComponent<Animator>();
        string controllerName = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.name
            : "<null>";
        string spriteName = spriteRenderer != null && spriteRenderer.sprite != null
            ? spriteRenderer.sprite.name
            : "<null>";
        Diagnostics.Log($"[Passenger][init] name={name} female={IsFemale} layer={gameObject.layer} sprite='{spriteName}' ctrl='{controllerName}'");

        _train = train;
        EnsureStateRuntimeInitialized();
        _stateRuntime.ConfigureTrain(_train);
        _stateRuntime.ChangeState(LoveMetro.Passengers.PassengerStateId.Wandering);

        GetAbilities()?.AttachAll();

        _isInitiated = true;
        if (LevelGameplaySettings.SlipperyFloorEnabled)
            _rigidbody.drag = LevelGameplaySettings.SlipperyLinearDrag;

        Diagnostics.Log($"[Passenger][ready] name={name} rb(cdm={_rigidbody.collisionDetectionMode}, interp={_rigidbody.interpolation}, drag={_rigidbody.drag:F2})");
    }

    private void Update()
    {
        if (!_isInitiated || _stateRuntime == null || !_stateRuntime.HasCurrentState)
            return;

        _stateRuntime.UpdateState();
        if (!IsMatchable && Time.time >= _rematchEnableTime)
            IsMatchable = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_stateRuntime == null || !_stateRuntime.HasCurrentState)
            return;

        _stateRuntime.OnCollision(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_stateRuntime == null || !_stateRuntime.HasCurrentState)
            return;

        _stateRuntime.OnTriggerEnter(collision);
    }

    public void ForceToMatchingState(Passenger partner)
    {
        EnsureRequiredComponents();
        EnsureStateRuntimeInitialized();

        if (partner == null)
        {
            _stateRuntime.ChangeState(LoveMetro.Passengers.PassengerStateId.Matching);
            return;
        }

        if (transform.position.x <= partner.transform.position.x)
        {
            Couple couple = Instantiate(CouplePref).GetComponent<Couple>();
            couple.Init(this, partner);
            AwardMatchPointsFor(partner, couple.transform.position);
        }

        _stateRuntime.ChangeState(LoveMetro.Passengers.PassengerStateId.Matching);
    }

    public void Launch(Vector2 initialVelocity)
    {
        EnterFallingState(initialVelocity);
    }

    public void Transport(Vector3 position)
    {
        transform.position = position;
    }

    public void ForceToAbsorptionState(Vector3 absorptionCenter, float absorptionForce)
    {
        if (IsInCouple || IsCurrentState(LoveMetro.Passengers.PassengerStateId.BeingAbsorbed))
            return;

        EnsureRequiredComponents();
        EnsureStateRuntimeInitialized();
        _stateRuntime.EnterAbsorption(absorptionCenter, absorptionForce);
    }
}

public class OnEnteringMatchingState : UnityEvent<Vector3> { }
