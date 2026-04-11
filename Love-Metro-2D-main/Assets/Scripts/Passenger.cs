using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(PassangerAnimator), typeof(Collider2D))]
public partial class Passenger : MonoBehaviour, IFieldEffectTarget
{
    public static float GlobalSpeedMultiplier = 0.7f;

    [Header("Settings (optional)")]
    [Tooltip("Optional ScriptableObject override. When omitted, inspector values are used.")]
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
    private PassangerState _currentState;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private ScoreCounter _scoreCounter;

    private Wandering wanderingState;
    private StayingOnHandrail stayingOnHandrailState;
    private Falling fallingState;
    private Flying flyingState;
    private Matching matchingState;
    private BeingAbsorbed beingAbsorbedState;

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

    private void ApplySettingsFromAsset()
    {
        if (_settings == null)
            return;

        GlobalSpeedMultiplier = _settings.globalSpeedMultiplier;
        _speed = _settings.baseSpeed;
        _minFallingSpeed = _settings.minFallingSpeed;

        _grabingHandrailChance = _settings.handrailGrabChance;
        _handrailMinGrabbingSpeed = _settings.handrailMinGrabbingSpeed;
        _handrailCooldown = _settings.handrailCooldown;
        HandrailStandingTimeInterval = _settings.handrailStandingTimeInterval;

        _launchSensitivity = _settings.launchSensitivity;
        _minImpulseToLaunch = _settings.minImpulseToLaunch;
        _impulseToVelocityScale = _settings.impulseToVelocityScale;
        _globalImpulseScale = _settings.globalImpulseScale;
        _uniformLaunchScale = _settings.uniformLaunchScale;
        _uniformLaunchGamma = _settings.uniformLaunchGamma;
        _flightHorizontalScale = _settings.flightHorizontalScale;
        _flightVerticalScale = _settings.flightVerticalScale;
        _flightVerticalGamma = _settings.flightVerticalGamma;
        _minWindStrengthForFlying = _settings.minWindStrengthForFlying;
        _maxFlyingTime = _settings.maxFlyingTime;

        _maxFlightSpeed = _settings.maxFlightSpeed;
        _flightSpeedMultiplier = _settings.flightSpeedMultiplier;
        _flightDeceleration = _settings.flightDeceleration;
        _maxBounces = _settings.maxBounces;
        _bounceElasticity = _settings.bounceElasticity;
        _wallBounceBoost = _settings.wallBounceBoost;

        _easeOutMinK = _settings.easeOutMinK;
        _easeOutMaxK = _settings.easeOutMaxK;

        _aimAssistRadius = _settings.aimAssistRadius;
        _aimAssistMaxStrength = _settings.aimAssistMaxStrength;
        _turbulenceStrength = _settings.turbulenceStrength;
        _angleSnapDeg = _settings.angleSnapDeg;

        _magnetRadius = _settings.magnetRadius;
        _magnetForce = _settings.magnetForce;
        _repelRadius = _settings.repelRadius;
        _repelForce = _settings.repelForce;

        _rematchCooldown = _settings.rematchCooldown;
        _defaultLayer = _settings.defaultLayer;
    }

    public void Initiate(Vector3 initialMovingDirection, TrainManager train, ScoreCounter scoreCounter)
    {
        ApplySettingsFromAsset();

        _initialMovingDirection = initialMovingDirection;
        CurrentMovingDirection = _initialMovingDirection.normalized;
        _speed *= GlobalSpeedMultiplier;
        _scoreCounter = scoreCounter;

        EnsureRequiredComponents();
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

        if (_launchSensitivity <= 0f) _launchSensitivity = 1.2f;
        if (_minImpulseToLaunch <= 0f) _minImpulseToLaunch = 0.1f;
        if (_aimAssistRadius <= 0f) _aimAssistRadius = 5.0f;
        if (_aimAssistMaxStrength <= 0f) _aimAssistMaxStrength = 1.0f;
        if (_turbulenceStrength < 0f) _turbulenceStrength = 0.6f;
        if (_angleSnapDeg <= 0f) _angleSnapDeg = 10f;
        if (_impulseToVelocityScale <= 0f) _impulseToVelocityScale = 3.2f;
        if (_maxFlightSpeed <= 0f) _maxFlightSpeed = 56f;
        if (_flightSpeedMultiplier <= 0f || _flightSpeedMultiplier > 2f) _flightSpeedMultiplier = 0.7f;
        if (_globalImpulseScale <= 0f || _globalImpulseScale > 2f) _globalImpulseScale = 0.8f;
        if (_uniformLaunchScale <= 0f) _uniformLaunchScale = 1.8f;
        if (_uniformLaunchGamma <= 0f) _uniformLaunchGamma = 0.75f;
        if (_flightHorizontalScale <= 0f) _flightHorizontalScale = 0.48f;
        if (_flightVerticalScale <= 0f) _flightVerticalScale = 2.88f;
        if (_flightVerticalGamma <= 0f) _flightVerticalGamma = 0.65f;
        if (_minWindStrengthForFlying <= 0f) _minWindStrengthForFlying = 8f;
        if (_maxFlyingTime <= 0f) _maxFlyingTime = 5f;
        _wallBounceBoost = 1f;

        EnsureStateMachineInitialized();
        _currentState = wanderingState;
        _currentState?.Enter();

        _train = train;
        SubscribeCurrentStateToTrainInertia();

        GetAbilities()?.AttachAll();

        _isInitiated = true;
        if (LevelGameplaySettings.SlipperyFloorEnabled)
            _rigidbody.drag = LevelGameplaySettings.SlipperyLinearDrag;

        Diagnostics.Log($"[Passenger][ready] name={name} rb(cdm={_rigidbody.collisionDetectionMode}, interp={_rigidbody.interpolation}, drag={_rigidbody.drag:F2})");
    }

    private void Update()
    {
        if (!_isInitiated || _currentState == null)
            return;

        _currentState.UpdateState();
        if (!IsMatchable && Time.time >= _rematchEnableTime)
            IsMatchable = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_currentState == null)
            return;

        _currentState.OnCollision(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_currentState == null)
            return;

        _currentState.OnTriggerEnter(collision);
    }

    public void ForceToMatchingState(Passenger partner)
    {
        EnsureRequiredComponents();
        EnsureStateMachineInitialized();

        if (partner == null)
        {
            ChangeState(matchingState);
            return;
        }

        if (transform.position.x <= partner.transform.position.x)
        {
            Couple couple = Instantiate(CouplePref).GetComponent<Couple>();
            couple.Init(this, partner);
            AwardMatchPointsFor(partner, couple.transform.position);
        }

        ChangeState(matchingState);
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
        if (IsInCouple || _currentState is BeingAbsorbed)
            return;

        EnsureRequiredComponents();
        EnsureStateMachineInitialized();
        beingAbsorbedState.SetAbsorptionParameters(absorptionCenter, absorptionForce);
        ChangeState(beingAbsorbedState);
    }
}

public class OnEnteringMatchingState : UnityEvent<Vector3> { }
