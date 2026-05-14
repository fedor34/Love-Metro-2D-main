using UnityEngine;

public partial class Passenger
{
    private void Awake()
    {
        int colliderCount = EnsurePhysicsRuntime().EnsureSolidChildColliders();
        EnsureRequiredComponents();
        ConfigureRigidbody();
        Diagnostics.Log($"[Passenger][awake] name={name} cols={colliderCount} {EnsurePhysicsRuntime().DescribeRigidbody()} layer={gameObject.layer}");

        EnsurePhysicsRuntime().NormalizeVipColliderIfNeeded();
    }

    private void Start()
    {
        RegisterInRuntimeSystems();
    }

    private void OnDestroy()
    {
        UnsubscribeCurrentStateFromTrainInertia();
        DetachFromContainer();
        UnregisterFromRuntimeSystems();
    }

    public void RemoveFromContainerAndDestroy()
    {
        DetachFromContainer();
        LoveMetro.Core.UnityLifecycle.SafeDestroy(gameObject);
    }

    public void EnterCouple(Transform coupleRoot, Vector3 worldPosition, bool faceRight)
    {
        Couple couple = coupleRoot != null ? coupleRoot.GetComponent<Couple>() : null;
        EnterCoupleInternal(couple, coupleRoot, worldPosition, faceRight);
    }

    internal void EnterCouple(Couple couple, Vector3 worldPosition, bool faceRight)
    {
        EnterCoupleInternal(couple, couple != null ? couple.transform : null, worldPosition, faceRight);
    }

    private void EnterCoupleInternal(Couple couple, Transform coupleRoot, Vector3 worldPosition, bool faceRight)
    {
        EnsureRequiredComponents();
        Transport(worldPosition);
        PassangerAnimator?.ChangeFacingDirection(faceRight);
        transform.SetParent(coupleRoot);
        _currentCouple = couple;
        IsInCouple = true;
        RefreshCoupleRegistryStatus();
    }

    public void ExitCouple(Vector2 kickVelocity)
    {
        transform.SetParent(null);
        BreakFromCouple(kickVelocity);
    }

    public void BreakFromCouple(Vector2 kickVelocity)
    {
        EnsureRequiredComponents();
        IsInCouple = false;
        _currentCouple = null;
        EnsurePhysicsRuntime().SetColliderEnabled(true);
        EnsurePhysicsRuntime().SetBodyType(RigidbodyType2D.Dynamic);
        PassangerAnimator?.ResetAfterPairBreak();

        Launch(kickVelocity);
        IsMatchable = false;
        _rematchEnableTime = Time.time + Settings.rematchCooldown;
        RefreshCoupleRegistryStatus();
        InvokePairBroken(null);
    }

    public string GetCurrentStateName()
    {
        return _stateRuntime != null ? _stateRuntime.CurrentStateName : "None";
    }

    private void LogPassengerEvent(string category, string message)
    {
        Diagnostics.Log($"[Passenger][{category}] {message}");
    }

    private void EnsureRequiredComponents()
    {
        EnsurePhysicsRuntime().EnsureRequiredComponents();
        PassangerAnimator = EnsurePhysicsRuntime().Animator;
    }

    private void ConfigureRigidbody()
    {
        EnsurePhysicsRuntime().ConfigureRigidbody(Settings);
        PassangerAnimator = EnsurePhysicsRuntime().Animator;
    }

    public void ResetPhysicsCollisionFilters()
    {
        EnsurePhysicsRuntime().ResetCollisionFilters();
    }

    private void RegisterInRuntimeSystems()
    {
        PassengerRegistry.Instance?.Register(this);
        FieldEffectSystem.Instance?.RegisterTarget(this);
    }

    private void UnregisterFromRuntimeSystems()
    {
        PassengerRegistry.Instance?.Unregister(this);
        FieldEffectSystem.Instance?.UnregisterTarget(this);
    }

    private void DetachFromContainer()
    {
        if (container != null)
            container.RemovePassenger(this);
    }

    private void RefreshCoupleRegistryStatus()
    {
        PassengerRegistry.Instance?.UpdateCoupleStatus(this);
    }

    private LoveMetro.Passengers.PassengerPhysicsRuntime EnsurePhysicsRuntime()
    {
        if (_physicsRuntime == null)
            _physicsRuntime = new LoveMetro.Passengers.PassengerPhysicsRuntime(this);

        return _physicsRuntime;
    }

    private LoveMetro.Passengers.PassengerInteractionRuntime EnsureInteractionRuntime()
    {
        if (_interactionRuntime == null)
            _interactionRuntime = new LoveMetro.Passengers.PassengerInteractionRuntime(this);

        return _interactionRuntime;
    }

    private LoveMetro.Passengers.PassengerMatchRuntime EnsureMatchRuntime()
    {
        if (_matchRuntime == null)
            _matchRuntime = new LoveMetro.Passengers.PassengerMatchRuntime(this);

        return _matchRuntime;
    }

    private LoveMetro.Passengers.PassengerPairFormationRuntime EnsurePairFormationRuntime()
    {
        if (_pairFormationRuntime == null)
            _pairFormationRuntime = new LoveMetro.Passengers.PassengerPairFormationRuntime(this);

        return _pairFormationRuntime;
    }
}
