using UnityEngine;

public partial class Passenger
{
    public void ApplyFieldForce(Vector2 force, FieldEffectType effectType)
    {
        if (!_isInitiated)
            return;

        if (effectType == FieldEffectType.Wind && force.magnitude > 0.1f)
            LogPassengerEvent("wind", $"{name} force={force.magnitude:F1} state={_currentState?.GetType().Name} inCouple={IsInCouple} initiated={_isInitiated}");

        if (effectType == FieldEffectType.Wind && TryHandleWindForce(force))
            return;

        if (_currentState is Falling)
        {
            ApplyDirectedForce(force, ForceMode2D.Force);
            return;
        }

        if (_currentState is Wandering)
        {
            ApplyDirectedForce(force, ForceMode2D.Force);
            return;
        }

        if (_currentState is Flying && flyingState != null)
            flyingState.UpdateWindEffect(force, force.magnitude);
    }

    public void ApplyFieldForce(Vector3 force, ForceMode2D forceMode)
    {
        if (!_isInitiated)
            return;

        if (_currentState is Falling)
        {
            ApplyDirectedForce(force, forceMode);
            return;
        }

        if (_currentState is Wandering)
            UpdateCurrentMovingDirection(force);
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public Rigidbody2D GetRigidbody()
    {
        return _rigidbody;
    }

    public bool CanBeAffectedBy(FieldEffectType effectType)
    {
        if (!_isInitiated || IsInCouple || _currentState is BeingAbsorbed)
            return false;

        switch (effectType)
        {
            case FieldEffectType.Gravity:
            case FieldEffectType.Repulsion:
            case FieldEffectType.Wind:
            case FieldEffectType.Vortex:
            case FieldEffectType.Magnetic:
                return _currentState is Wandering || _currentState is Falling || _currentState is Flying;
            case FieldEffectType.Slowdown:
            case FieldEffectType.Speedup:
            case FieldEffectType.Friction:
                return _currentState is Wandering;
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

        if (windStrength >= _minWindStrengthForFlying
            && _currentState is not Flying
            && _currentState is not BeingAbsorbed
            && !IsInCouple)
        {
            EnsureStateMachineInitialized();
            ChangeState(flyingState);
            flyingState.SetFlyingParameters(force, windStrength);
            LogPassengerEvent("wind", $"{name} entered flying with force={windStrength:F1}");
            return true;
        }

        if (_currentState is Flying && flyingState != null)
        {
            flyingState.UpdateWindEffect(force, windStrength);
            return true;
        }

        if (_currentState is Wandering)
        {
            UpdateCurrentMovingDirection(force);
            float forceMultiplier = windStrength < _minWindStrengthForFlying ? 0.5f : 2f;
            _rigidbody.AddForce(force * forceMultiplier, ForceMode2D.Force);
            LogPassengerEvent("wind", $"{name} pushed while wandering force={windStrength:F1} multiplier={forceMultiplier:F1}");
            return true;
        }

        if (windStrength > 5f && _currentState is not BeingAbsorbed)
        {
            _rigidbody.AddForce(force, ForceMode2D.Force);
            LogPassengerEvent("wind", $"{name} forced by wind force={windStrength:F1}");
            return true;
        }

        return false;
    }

    private void ApplyDirectedForce(Vector3 force, ForceMode2D forceMode)
    {
        UpdateCurrentMovingDirection(force);
        if (_rigidbody != null)
            _rigidbody.AddForce(force, forceMode);
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

        if (effectData.strength <= _handrailMinGrabbingSpeed || _currentState is StayingOnHandrail)
            return;

        EnsureStateMachineInitialized();
        ChangeState(fallingState);
    }
}
