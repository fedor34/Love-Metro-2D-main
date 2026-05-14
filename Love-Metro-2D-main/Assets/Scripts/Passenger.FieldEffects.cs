using UnityEngine;
using LoveMetro.Passengers;

public partial class Passenger
{
    public void ApplyFieldForce(Vector2 force, FieldEffectType effectType)
    {
        if (!_isInitiated)
            return;

        if (effectType == FieldEffectType.Wind && force.magnitude > 0.1f)
            LogPassengerEvent("wind", $"{name} force={force.magnitude:F1} state={GetCurrentStateName()} inCouple={IsInCouple} initiated={_isInitiated}");

        if (effectType == FieldEffectType.Wind && TryHandleWindForce(force))
            return;

        if (IsCurrentState(PassengerStateId.Falling))
        {
            ApplyDirectedForce(force, ForceMode2D.Force);
            return;
        }

        if (IsCurrentState(PassengerStateId.Wandering))
        {
            ApplyDirectedForce(force, ForceMode2D.Force);
            return;
        }

        if (IsCurrentState(PassengerStateId.Flying))
            _stateRuntime?.UpdateFlyingWind(force, force.magnitude);
    }

    public void ApplyFieldForce(Vector3 force, ForceMode2D forceMode)
    {
        if (!_isInitiated)
            return;

        if (IsCurrentState(PassengerStateId.Falling))
        {
            ApplyDirectedForce(force, forceMode);
            return;
        }

        if (IsCurrentState(PassengerStateId.Wandering))
            UpdateCurrentMovingDirection(force);
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public Rigidbody2D GetRigidbody()
    {
        return EnsurePhysicsRuntime().Rigidbody;
    }

    public bool CanBeAffectedBy(FieldEffectType effectType)
    {
        if (!_isInitiated || IsInCouple || IsCurrentState(PassengerStateId.BeingAbsorbed))
            return false;

        switch (effectType)
        {
            case FieldEffectType.Gravity:
            case FieldEffectType.Repulsion:
            case FieldEffectType.Wind:
            case FieldEffectType.Vortex:
            case FieldEffectType.Magnetic:
                return IsCurrentState(PassengerStateId.Wandering)
                    || IsCurrentState(PassengerStateId.Falling)
                    || IsCurrentState(PassengerStateId.Flying);
            case FieldEffectType.Slowdown:
            case FieldEffectType.Speedup:
            case FieldEffectType.Friction:
                return IsCurrentState(PassengerStateId.Wandering);
            default:
                return true;
        }
    }

    public void OnEnterFieldEffect(IFieldEffect effect)
    {
        if (IsInCouple || effect == null)
            return;

        FieldEffectData effectData = effect.GetEffectData();
        if (effectData.effectType == FieldEffectType.Gravity && effect is GravityFieldEffectNew gravityEffect)
            HandleGravityFieldEntry(effectData, gravityEffect);
    }

    public void OnExitFieldEffect(IFieldEffect effect)
    {
    }

    private bool TryHandleWindForce(Vector2 force)
    {
        float windStrength = force.magnitude;
        float minWindStrengthForFlying = Settings.minWindStrengthForFlying;

        if (windStrength >= minWindStrengthForFlying
            && !IsCurrentState(PassengerStateId.Flying)
            && !IsCurrentState(PassengerStateId.BeingAbsorbed)
            && !IsInCouple)
        {
            EnsureStateRuntimeInitialized();
            _stateRuntime.EnterFlying(force, windStrength);
            LogPassengerEvent("wind", $"{name} entered flying with force={windStrength:F1}");
            return true;
        }

        if (IsCurrentState(PassengerStateId.Flying))
        {
            _stateRuntime?.UpdateFlyingWind(force, windStrength);
            return true;
        }

        if (IsCurrentState(PassengerStateId.Wandering))
        {
            UpdateCurrentMovingDirection(force);
            float forceMultiplier = windStrength < minWindStrengthForFlying ? 0.5f : 2f;
            EnsurePhysicsRuntime().AddForce(force * forceMultiplier, ForceMode2D.Force);
            LogPassengerEvent("wind", $"{name} pushed while wandering force={windStrength:F1} multiplier={forceMultiplier:F1}");
            return true;
        }

        if (windStrength > 5f && !IsCurrentState(PassengerStateId.BeingAbsorbed))
        {
            EnsurePhysicsRuntime().AddForce(force, ForceMode2D.Force);
            LogPassengerEvent("wind", $"{name} forced by wind force={windStrength:F1}");
            return true;
        }

        return false;
    }

    private void ApplyDirectedForce(Vector3 force, ForceMode2D forceMode)
    {
        UpdateCurrentMovingDirection(force);
        EnsurePhysicsRuntime().AddForce(force, forceMode);
    }

    private void UpdateCurrentMovingDirection(Vector3 force)
    {
        if (force.sqrMagnitude > 0.0001f)
            CurrentMovingDirection = ((Vector2)force).normalized;
    }

    private void HandleGravityFieldEntry(FieldEffectData effectData, GravityFieldEffectNew gravityEffect)
    {
        float distanceToCenter = Vector3.Distance(transform.position, effectData.center);
        if (gravityEffect._createBlackHoleEffect && distanceToCenter <= gravityEffect._eventHorizonRadius)
        {
            ForceToAbsorptionState(effectData.center, effectData.strength);
            return;
        }

        if (effectData.strength <= Settings.handrailMinGrabbingSpeed || IsCurrentState(PassengerStateId.StayingOnHandrail))
            return;

        EnsureStateRuntimeInitialized();
        _stateRuntime.ChangeState(PassengerStateId.Falling);
    }
}
