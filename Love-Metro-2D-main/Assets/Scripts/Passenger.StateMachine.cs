using LoveMetro.Passengers;
using UnityEngine;

public partial class Passenger
{
    private void EnsureStateRuntimeInitialized()
    {
        EnsureStateTuning();

        if (_stateRuntime == null)
            _stateRuntime = new PassengerStateRuntime(this);

        _stateRuntime.ConfigureTrain(_train);
    }

    private void ChangeState(PassengerStateId id)
    {
        EnsureStateRuntimeInitialized();
        _stateRuntime.ChangeState(id);
    }

    private void UnsubscribeCurrentStateFromTrainInertia()
    {
        _stateRuntime?.Clear();
    }

    private bool IsCurrentState(PassengerStateId id)
    {
        return _stateRuntime?.CurrentStateId == id;
    }

    string IPassengerStateHost.Name => name;
    Vector3 IPassengerStateHost.Position
    {
        get => transform.position;
        set => transform.position = value;
    }

    PassengerSettings IPassengerStateHost.Settings => Settings;
    PassangerAnimator IPassengerStateHost.Animator
    {
        get
        {
            if (PassangerAnimator == null)
                PassangerAnimator = GetComponent<PassangerAnimator>();
            return PassangerAnimator;
        }
    }
    bool IPassengerStateHost.IsInCouple => IsInCouple;
    bool IPassengerStateHost.IsMatchable
    {
        get => IsMatchable;
        set => IsMatchable = value;
    }

    Vector2 IPassengerStateHost.CurrentMovingDirection
    {
        get => CurrentMovingDirection;
        set => CurrentMovingDirection = value;
    }

    float IPassengerStateHost.TimeWithoutHolding
    {
        get => _timeWithoutHolding;
        set => _timeWithoutHolding = value;
    }

    PassengerStateTuning IPassengerStateHost.Tuning => EnsureStateTuning();

    Passenger IPassengerInteractionHost.Passenger => this;
    Vector3 IPassengerInteractionHost.Position => transform.position;
    PassengerStateTuning IPassengerInteractionHost.Tuning => EnsureStateTuning();
    PassengerPhysicsRuntime IPassengerInteractionHost.PhysicsRuntime => EnsurePhysicsRuntime();
    LoveMetro.Core.IRuntimeServices IPassengerInteractionHost.Services => LoveMetro.Core.RuntimeServices.Instance;
    PassengerInteractionRuntime IPassengerInteractionHost.InteractionRuntime => EnsureInteractionRuntime();
    void IPassengerInteractionHost.BreakCoupleOnImpact(Passenger hitter) => BreakCoupleOnImpact(hitter);
    bool IPassengerInteractionHost.TryMatchWith(Passenger other) => TryMatchWith(other);

    void IPassengerStateHost.ChangeState(PassengerStateId id) => ChangeState(id);
    void IPassengerStateHost.EnterFallingState(Vector2 initialVelocity) => EnterFallingState(initialVelocity);
    void IPassengerStateHost.SetBodyType(RigidbodyType2D bodyType)
    {
        EnsurePhysicsRuntime().SetBodyType(bodyType);
    }

    void IPassengerStateHost.SetDefaultLayer()
    {
        EnsurePhysicsRuntime().SetDefaultLayer(Settings.defaultLayer);
    }

    void IPassengerStateHost.SetColliderEnabled(bool enabled)
    {
        EnsurePhysicsRuntime().SetColliderEnabled(enabled);
    }

    void IPassengerStateHost.SetVelocity(Vector2 velocity)
    {
        EnsurePhysicsRuntime().SetVelocity(velocity);
    }

    void IPassengerStateHost.AddForce(Vector2 force, ForceMode2D mode)
    {
        EnsurePhysicsRuntime().AddForce(force, mode);
    }

    Vector2 IPassengerStateHost.GetVelocity()
    {
        return EnsurePhysicsRuntime().CurrentVelocity;
    }

    void IPassengerStateHost.SetDamping(float linearDamping, float angularDamping)
    {
        EnsurePhysicsRuntime().SetDamping(linearDamping, angularDamping);
    }

    Vector2 IPassengerStateHost.ClampFlightVelocity(Vector2 velocity)
    {
        return EnsurePhysicsRuntime().ClampFlightVelocity(velocity);
    }

    Vector2 IPassengerStateHost.ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        return EnsurePhysicsRuntime().ReflectVelocity(velocity, normal, boostMultiplier);
    }

    Vector2 IPassengerStateHost.ScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale)
    {
        return EnsurePhysicsRuntime().ScaleLaunchVelocity(velocity, speedMultiplier, impulseScale);
    }

    void IPassengerStateHost.ApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        EnsurePhysicsRuntime().ApplyReflectedVelocity(velocity, normal, boostMultiplier);
    }

    void IPassengerStateHost.ForwardTrainSpeedChangeToCurrentState(Vector2 force)
    {
        _stateRuntime?.ForwardTrainSpeedChangeToCurrentState(force);
    }

    int IPassengerStateHost.GetContacts(ContactPoint2D[] contactPoints)
    {
        return EnsurePhysicsRuntime().GetContacts(contactPoints);
    }

    void IPassengerStateHost.AttachHandrail(HandRailPosition handrail)
    {
        if (handrail == null)
            return;

        handrail.IsOccupied = true;
        transform.position = handrail.transform.position;
        releaseHandrail += handrail.ReleaseHandrail;
    }

    void IPassengerStateHost.ReleaseHandrail()
    {
        releaseHandrail?.Invoke();
        releaseHandrail = null;
    }

    void IPassengerStateHost.RemovePassengerAndDestroy()
    {
        RemoveFromContainerAndDestroy();
    }

    void IPassengerStateHost.LogEvent(string category, string message)
    {
        LogPassengerEvent(category, message);
    }
}
