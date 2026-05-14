using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(PassangerAnimator))]
public partial class Passenger : MonoBehaviour, IFieldEffectTarget, LoveMetro.Passengers.IPassengerStateHost, LoveMetro.Passengers.IPassengerInteractionHost, LoveMetro.Passengers.IPassengerMatchHost
{
    public static float GlobalSpeedMultiplier = 0.7f;

    [Header("Settings (optional)")]
    [Tooltip("Optional ScriptableObject override. When omitted, Resources/PassengerSettings is used.")]
    [SerializeField] private PassengerSettings _settings;

    private delegate void ReleaseHandrail();
    private ReleaseHandrail releaseHandrail;

    [SerializeField] private Vector2 _initialMovingDirection;
    private Vector2 CurrentMovingDirection;
    [SerializeField] private GameObject CouplePref;

    public bool IsFemale;
    [HideInInspector] public bool IsMatchable = true;
    [HideInInspector] public PassangerAnimator PassangerAnimator;

    [SerializeField] private TrainManager _train;

    private float _timeWithoutHolding;
    private LoveMetro.Passengers.PassengerStateRuntime _stateRuntime;
    private LoveMetro.Passengers.PassengerPhysicsRuntime _physicsRuntime;
    private LoveMetro.Passengers.PassengerInteractionRuntime _interactionRuntime;
    private LoveMetro.Passengers.PassengerMatchRuntime _matchRuntime;
    private LoveMetro.Passengers.PassengerStateTuning _stateTuning;
    private ScoreCounter _scoreCounter;
    private bool _stateTuningInitialized;

    private bool _isInitiated = false;

    public PassangersContainer container;

    public bool IsInCouple = false;
    private float _rematchEnableTime = 0f;

    public PassengerSettings Settings => PassengerSettings.Resolve(_settings);

    public void Initiate(Vector3 initialMovingDirection, TrainManager train, ScoreCounter scoreCounter)
    {
        PassengerSettings settings = Settings;
        GlobalSpeedMultiplier = settings.globalSpeedMultiplier;
        RebuildStateTuning();

        _initialMovingDirection = initialMovingDirection;
        CurrentMovingDirection = _initialMovingDirection.normalized;
        _scoreCounter = scoreCounter;

        EnsureRequiredComponents();
        ConfigureMotionController();
        EnsurePhysicsRuntime().SetDefaultLayer(settings.defaultLayer);

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

        AttachAbilities();

        _isInitiated = true;
        if (LevelGameplaySettings.SlipperyFloorEnabled)
            EnsurePhysicsRuntime().SetLinearDamping(LevelGameplaySettings.SlipperyLinearDrag);

        Diagnostics.Log($"[Passenger][ready] name={name} {EnsurePhysicsRuntime().DescribeRigidbody()}");
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

    private LoveMetro.Passengers.PassengerStateTuning EnsureStateTuning()
    {
        if (!_stateTuningInitialized)
            RebuildStateTuning();

        return _stateTuning;
    }

    private void RebuildStateTuning()
    {
        PassengerSettings settings = Settings;
        _stateTuning = new LoveMetro.Passengers.PassengerStateTuning(
            settings.additionalCollisionCheckTimePeriod,
            settings.handrailGrabChance,
            settings.handrailCooldown,
            settings.handrailStandingTimeInterval,
            settings.launchSensitivity,
            settings.minImpulseToLaunch,
            settings.aimAssistRadius,
            settings.aimAssistMaxStrength,
            settings.turbulenceStrength,
            settings.impulseToVelocityScale,
            settings.maxFlightSpeed,
            settings.flightSpeedMultiplier,
            settings.globalImpulseScale,
            settings.uniformLaunchScale,
            settings.uniformLaunchGamma,
            settings.flightHorizontalScale,
            settings.flightVerticalScale,
            settings.flightVerticalGamma,
            settings.minWindStrengthForFlying,
            settings.maxFlyingTime,
            settings.magnetRadius,
            settings.magnetForce,
            settings.repelRadius,
            settings.repelForce,
            settings.flightDeceleration,
            settings.wallBounceBoost,
            settings.maxBounces,
            settings.easeOutMinK,
            settings.easeOutMaxK);
        _stateTuningInitialized = true;
    }
}

public class OnEnteringMatchingState : UnityEvent<Vector3> { }
