using System.Collections.Generic;
using LoveMetro.Passengers;
using UnityEngine;

public partial class Passenger
{
    private readonly List<Passenger> _sameGenderRepelBuffer = new List<Passenger>(8);

    private void EnsureStateMachineInitialized()
    {
        PassengerStateContext context = new PassengerStateContext(this);
        _stateMachine ??= new PassengerStateMachine(context);
        _stateFactory ??= new PassengerStateFactory(context);

        if (wanderingState != null)
            return;

        wanderingState = _stateFactory.Create(PassengerStateId.Wandering);
        fallingState = (IPassengerFallingState)_stateFactory.Create(PassengerStateId.Falling);
        flyingState = (IPassengerFlyingState)_stateFactory.Create(PassengerStateId.Flying);
        stayingOnHandrailState = _stateFactory.Create(PassengerStateId.StayingOnHandrail);
        matchingState = _stateFactory.Create(PassengerStateId.Matching);
        beingAbsorbedState = (IPassengerAbsorptionState)_stateFactory.Create(PassengerStateId.BeingAbsorbed);
    }

    private void ChangeState(IPassengerState newState)
    {
        EnsureStateMachineInitialized();
        _stateMachine.ConfigureTrain(_train);
        _stateMachine.ChangeState(newState);
        _currentState = _stateMachine.CurrentState;
    }

    internal void StateChangeState(PassengerStateId id)
    {
        EnsureStateMachineInitialized();
        switch (id)
        {
            case PassengerStateId.Wandering:
                ChangeState(wanderingState);
                break;
            case PassengerStateId.Falling:
                ChangeState(fallingState);
                break;
            case PassengerStateId.Flying:
                ChangeState(flyingState);
                break;
            case PassengerStateId.Matching:
                ChangeState(matchingState);
                break;
            case PassengerStateId.StayingOnHandrail:
                ChangeState(stayingOnHandrailState);
                break;
            case PassengerStateId.BeingAbsorbed:
                ChangeState(beingAbsorbedState);
                break;
        }
    }

    private void SubscribeCurrentStateToTrainInertia()
    {
        _stateMachine?.ConfigureTrain(_train);
    }

    private void UnsubscribeCurrentStateFromTrainInertia()
    {
        _stateMachine?.Clear();
        _currentState = null;
    }

    private bool IsCurrentState(PassengerStateId id)
    {
        return _currentState?.Id == id;
    }

    internal PassangerAnimator StateAnimator => PassangerAnimator;
    internal Vector2 StateCurrentMovingDirection
    {
        get => CurrentMovingDirection;
        set => CurrentMovingDirection = value;
    }

    internal float StateTimeWithoutHolding
    {
        get => _timeWithoutHolding;
        set => _timeWithoutHolding = value;
    }

    internal float StateAdditionalCollisionCheckTimePeriod => _aditionalCollisionCheckTimePeriod;
    internal float StateGrabbingHandrailChance => _grabingHandrailChance;
    internal float StateHandrailCooldown => _handrailCooldown;
    internal Vector2 StateHandrailStandingTimeInterval => HandrailStandingTimeInterval;
    internal float StateLaunchSensitivity => _launchSensitivity;
    internal float StateMinImpulseToLaunch => _minImpulseToLaunch;
    internal float StateAimAssistRadius => _aimAssistRadius;
    internal float StateAimAssistMaxStrength => _aimAssistMaxStrength;
    internal float StateTurbulenceStrength => _turbulenceStrength;
    internal float StateImpulseToVelocityScale => _impulseToVelocityScale;
    internal float StateMaxFlightSpeed => _maxFlightSpeed;
    internal float StateFlightSpeedMultiplier => _flightSpeedMultiplier;
    internal float StateGlobalImpulseScale => _globalImpulseScale;
    internal float StateUniformLaunchScale => _uniformLaunchScale;
    internal float StateUniformLaunchGamma => _uniformLaunchGamma;
    internal float StateFlightHorizontalScale => _flightHorizontalScale;
    internal float StateFlightVerticalScale => _flightVerticalScale;
    internal float StateFlightVerticalGamma => _flightVerticalGamma;
    internal float StateMinWindStrengthForFlying => _minWindStrengthForFlying;
    internal float StateMaxFlyingTime => _maxFlyingTime;
    internal float StateMagnetRadius => _magnetRadius;
    internal float StateMagnetForce => _magnetForce;
    internal float StateRepelRadius => _repelRadius;
    internal float StateRepelForce => _repelForce;
    internal float StateFlightDeceleration => _flightDeceleration;
    internal float StateWallBounceBoost => _wallBounceBoost;
    internal int StateMaxBounces => _maxBounces;
    internal float StateEaseOutMinK => _easeOutMinK;
    internal float StateEaseOutMaxK => _easeOutMaxK;

    internal void StateEnterFallingState(Vector2 initialVelocity)
    {
        EnterFallingState(initialVelocity);
    }

    internal void StateSetBodyType(RigidbodyType2D bodyType)
    {
        EnsureRequiredComponents();
        _rigidbody.bodyType = bodyType;
    }

    internal void StateSetDefaultLayer()
    {
        gameObject.layer = LayerMask.NameToLayer(_defaultLayer);
    }

    internal void StateSetColliderEnabled(bool enabled)
    {
        EnsureRequiredComponents();
        _collider.enabled = enabled;
    }

    internal void StateSetVelocity(Vector2 velocity)
    {
        EnsureMotionController().SetVelocity(velocity);
    }

    internal void StateAddForce(Vector2 force, ForceMode2D mode)
    {
        EnsureMotionController().AddForce(force, mode);
    }

    internal Vector2 StateCurrentVelocity => EnsureMotionController().CurrentVelocity;

    internal void StateSetDamping(float linearDamping, float angularDamping)
    {
        EnsureRequiredComponents();
        _rigidbody.drag = linearDamping;
        _rigidbody.angularDrag = angularDamping;
    }

    internal Vector2 StateClampFlightVelocity(Vector2 velocity)
    {
        return ClampFlightVelocity(velocity);
    }

    internal Vector2 StateReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        return ReflectVelocity(velocity, normal, boostMultiplier);
    }

    internal Vector2 StateScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale)
    {
        return ScaleLaunchVelocity(velocity, speedMultiplier, impulseScale);
    }

    internal void StateApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        ApplyReflectedVelocity(velocity, normal, boostMultiplier);
    }

    internal void StateForwardTrainSpeedChangeToCurrentState(Vector2 force)
    {
        _currentState?.OnTrainSpeedChange(force);
    }

    internal Vector2 StateGetImpulseTargetWorld(Vector2 position)
    {
        return GetImpulseTargetWorld(position);
    }

    internal float StateGetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical)
    {
        return GetNormalizedTargetDelta(position, targetWorld, vertical);
    }

    internal Vector2 StateGetCollisionNormal(Collision2D collision, Vector2 fallback)
    {
        return GetCollisionNormal(collision, fallback);
    }

    internal bool StateTryResolvePassengerImpact(Passenger other)
    {
        return TryResolvePassengerImpact(other);
    }

    internal Passenger StateFindClosestOpposite(float radius)
    {
        return FindClosestOpposite(this, radius);
    }

    internal void StateCollectSameGenderPassengers(List<Passenger> results)
    {
        CollectSameGenderPassengers(results);
    }

    internal int StateGetContacts(ContactPoint2D[] contactPoints)
    {
        EnsureRequiredComponents();
        return _rigidbody.GetContacts(contactPoints);
    }

    internal void StateAttachHandrail(HandRailPosition handrail)
    {
        if (handrail == null)
            return;

        handrail.IsOccupied = true;
        transform.position = handrail.transform.position;
        releaseHandrail += handrail.ReleaseHandrail;
    }

    internal void StateReleaseHandrail()
    {
        releaseHandrail?.Invoke();
        releaseHandrail = null;
    }

    internal void StateLogEvent(string category, string message)
    {
        LogPassengerEvent(category, message);
    }

    private Vector2 GetImpulseTargetWorld(Vector2 position)
    {
        return ClickDirectionManager.HasReleasePoint
            ? ClickDirectionManager.LastReleaseWorld
            : position + ClickDirectionManager.GetCurrentDirection() * 5f;
    }

    private static float GetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            float worldDelta = vertical ? targetWorld.y - position.y : targetWorld.x - position.x;
            return Mathf.Clamp(worldDelta, -1f, 1f);
        }

        Vector3 positionScreen = mainCamera.WorldToScreenPoint(position);
        Vector3 targetScreen = mainCamera.WorldToScreenPoint(targetWorld);
        float delta = vertical ? targetScreen.y - positionScreen.y : targetScreen.x - positionScreen.x;
        float divisor = vertical ? Mathf.Max(1f, (float)Screen.height) : Mathf.Max(1f, (float)Screen.width);
        return Mathf.Clamp(delta / divisor, -1f, 1f);
    }

    private static Vector2 GetCollisionNormal(Collision2D collision, Vector2 fallback)
    {
        return collision != null && collision.contacts.Length > 0
            ? collision.contacts[0].normal
            : fallback;
    }

    private bool TryResolvePassengerImpact(Passenger other)
    {
        if (other == null)
            return false;

        BreakCoupleOnImpact(other);
        other.BreakCoupleOnImpact(this);
        return TryMatchWith(other);
    }

    private void CollectSameGenderPassengers(List<Passenger> results)
    {
        if (results == null)
            return;

        LoveMetro.Core.IPassengerRegistry registry = ResolvePassengerRegistry();
        if (registry != null)
        {
            registry.GetSameGenderInRadius(this, _repelRadius, results);
            return;
        }

        results.Clear();
    }

    private static Passenger FindClosestOpposite(Passenger self, float radius)
    {
        return ResolvePassengerRegistry()?.FindClosestOpposite(self, radius);
    }

    private static LoveMetro.Core.IPassengerRegistry ResolvePassengerRegistry()
    {
        return LoveMetro.Core.RuntimeServices.Instance.PassengerRegistry ?? PassengerRegistry.Instance;
    }
}
