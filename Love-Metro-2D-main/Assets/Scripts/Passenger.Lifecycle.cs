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
        Destroy(gameObject);
    }

    public void EnterCouple(Transform coupleRoot, Vector3 worldPosition, bool faceRight)
    {
        EnsureRequiredComponents();
        Transport(worldPosition);
        PassangerAnimator?.ChangeFacingDirection(faceRight);
        transform.SetParent(coupleRoot);
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
        EnsurePhysicsRuntime().SetColliderEnabled(true);
        EnsurePhysicsRuntime().SetBodyType(RigidbodyType2D.Dynamic);
        PassangerAnimator?.ResetAfterPairBreak();

        Launch(kickVelocity);
        IsMatchable = false;
        _rematchEnableTime = Time.time + _rematchCooldown;
        RefreshCoupleRegistryStatus();
        GetAbilities()?.InvokePairBroken(null);
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
}
