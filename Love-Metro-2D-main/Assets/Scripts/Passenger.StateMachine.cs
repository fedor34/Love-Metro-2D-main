using System.Collections.Generic;
using LoveMetro.Passengers;
using UnityEngine;

public partial class Passenger
{
    private readonly List<Passenger> _sameGenderRepelBuffer = new List<Passenger>(8);

    private void EnsureStateRuntimeInitialized()
    {
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
    PassangerAnimator IPassengerStateHost.Animator => PassangerAnimator;
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

    float IPassengerStateHost.AdditionalCollisionCheckTimePeriod => _aditionalCollisionCheckTimePeriod;
    float IPassengerStateHost.GrabbingHandrailChance => _grabingHandrailChance;
    float IPassengerStateHost.HandrailCooldown => _handrailCooldown;
    Vector2 IPassengerStateHost.HandrailStandingTimeInterval => HandrailStandingTimeInterval;
    float IPassengerStateHost.LaunchSensitivity => _launchSensitivity;
    float IPassengerStateHost.MinImpulseToLaunch => _minImpulseToLaunch;
    float IPassengerStateHost.AimAssistRadius => _aimAssistRadius;
    float IPassengerStateHost.AimAssistMaxStrength => _aimAssistMaxStrength;
    float IPassengerStateHost.TurbulenceStrength => _turbulenceStrength;
    float IPassengerStateHost.ImpulseToVelocityScale => _impulseToVelocityScale;
    float IPassengerStateHost.MaxFlightSpeed => _maxFlightSpeed;
    float IPassengerStateHost.FlightSpeedMultiplier => _flightSpeedMultiplier;
    float IPassengerStateHost.GlobalImpulseScale => _globalImpulseScale;
    float IPassengerStateHost.UniformLaunchScale => _uniformLaunchScale;
    float IPassengerStateHost.UniformLaunchGamma => _uniformLaunchGamma;
    float IPassengerStateHost.FlightHorizontalScale => _flightHorizontalScale;
    float IPassengerStateHost.FlightVerticalScale => _flightVerticalScale;
    float IPassengerStateHost.FlightVerticalGamma => _flightVerticalGamma;
    float IPassengerStateHost.MinWindStrengthForFlying => _minWindStrengthForFlying;
    float IPassengerStateHost.MaxFlyingTime => _maxFlyingTime;
    float IPassengerStateHost.MagnetRadius => _magnetRadius;
    float IPassengerStateHost.MagnetForce => _magnetForce;
    float IPassengerStateHost.RepelRadius => _repelRadius;
    float IPassengerStateHost.RepelForce => _repelForce;
    float IPassengerStateHost.FlightDeceleration => _flightDeceleration;
    float IPassengerStateHost.WallBounceBoost => _wallBounceBoost;
    int IPassengerStateHost.MaxBounces => _maxBounces;
    float IPassengerStateHost.EaseOutMinK => _easeOutMinK;
    float IPassengerStateHost.EaseOutMaxK => _easeOutMaxK;

    void IPassengerStateHost.ChangeState(PassengerStateId id) => ChangeState(id);
    void IPassengerStateHost.EnterFallingState(Vector2 initialVelocity) => EnterFallingState(initialVelocity);
    void IPassengerStateHost.SetBodyType(RigidbodyType2D bodyType)
    {
        EnsureRequiredComponents();
        _rigidbody.bodyType = bodyType;
    }

    void IPassengerStateHost.SetDefaultLayer()
    {
        gameObject.layer = LayerMask.NameToLayer(_defaultLayer);
    }

    void IPassengerStateHost.SetColliderEnabled(bool enabled)
    {
        EnsureRequiredComponents();
        _collider.enabled = enabled;
    }

    void IPassengerStateHost.SetVelocity(Vector2 velocity)
    {
        EnsureMotionController().SetVelocity(velocity);
    }

    void IPassengerStateHost.AddForce(Vector2 force, ForceMode2D mode)
    {
        EnsureMotionController().AddForce(force, mode);
    }

    Vector2 IPassengerStateHost.GetVelocity()
    {
        return EnsureMotionController().CurrentVelocity;
    }

    Vector2 IPassengerStateHost.GetVelocity(Passenger passenger)
    {
        return passenger != null ? passenger.EnsureMotionController().CurrentVelocity : Vector2.zero;
    }

    void IPassengerStateHost.SetDamping(float linearDamping, float angularDamping)
    {
        EnsureRequiredComponents();
        _rigidbody.drag = linearDamping;
        _rigidbody.angularDrag = angularDamping;
    }

    Vector2 IPassengerStateHost.ClampFlightVelocity(Vector2 velocity)
    {
        return ClampFlightVelocity(velocity);
    }

    Vector2 IPassengerStateHost.ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        return ReflectVelocity(velocity, normal, boostMultiplier);
    }

    Vector2 IPassengerStateHost.ScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale)
    {
        return ScaleLaunchVelocity(velocity, speedMultiplier, impulseScale);
    }

    void IPassengerStateHost.ApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        ApplyReflectedVelocity(velocity, normal, boostMultiplier);
    }

    void IPassengerStateHost.ApplyReflectedVelocity(Passenger passenger, Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        if (passenger != null)
            passenger.ApplyReflectedVelocity(velocity, normal, boostMultiplier);
    }

    float IPassengerStateHost.GetWallBounceBoost(Passenger passenger)
    {
        return passenger != null ? passenger._wallBounceBoost : _wallBounceBoost;
    }

    void IPassengerStateHost.ForwardTrainSpeedChangeToCurrentState(Vector2 force)
    {
        _stateRuntime?.ForwardTrainSpeedChangeToCurrentState(force);
    }

    Vector2 IPassengerStateHost.GetImpulseTargetWorld(Vector2 position)
    {
        return GetImpulseTargetWorld(position);
    }

    float IPassengerStateHost.GetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical)
    {
        return GetNormalizedTargetDelta(position, targetWorld, vertical);
    }

    Vector2 IPassengerStateHost.GetCollisionNormal(Collision2D collision, Vector2 fallback)
    {
        return GetCollisionNormal(collision, fallback);
    }

    bool IPassengerStateHost.TryResolvePassengerImpact(Passenger other)
    {
        return TryResolvePassengerImpact(other);
    }

    Passenger IPassengerStateHost.FindClosestOpposite(float radius)
    {
        return FindClosestOpposite(this, radius);
    }

    void IPassengerStateHost.CollectSameGenderPassengers(List<Passenger> results)
    {
        CollectSameGenderPassengers(results);
    }

    int IPassengerStateHost.GetContacts(ContactPoint2D[] contactPoints)
    {
        EnsureRequiredComponents();
        return _rigidbody.GetContacts(contactPoints);
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
