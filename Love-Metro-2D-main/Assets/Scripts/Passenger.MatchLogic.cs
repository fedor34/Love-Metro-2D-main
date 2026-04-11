using UnityEngine;

public partial class Passenger
{
    private PassengerAbilities _abilities;

    private PassengerAbilities GetAbilities()
    {
        if (_abilities == null)
            _abilities = GetComponent<PassengerAbilities>();

        return _abilities;
    }

    public int CalculateMatchPointsWith(Passenger partner, ScoreCounter scoreCounter = null)
    {
        int points = ResolveScoreCounter(scoreCounter)?.GetBasePointsPerCouple() ?? 0;
        GetAbilities()?.InvokeMatched(partner, ref points);
        partner?.GetAbilities()?.InvokeMatched(this, ref points);
        return Mathf.Max(0, points);
    }

    private ScoreCounter ResolveScoreCounter(ScoreCounter scoreCounter = null)
    {
        return scoreCounter != null ? scoreCounter : _scoreCounter;
    }

    private void AwardMatchPointsFor(Passenger partner, Vector3 worldPosition)
    {
        ScoreCounter scoreCounter = ResolveScoreCounter();
        if (scoreCounter == null)
            return;

        scoreCounter.AwardMatchPoints(WorldToScreenPoint(worldPosition), CalculateMatchPointsWith(partner, scoreCounter));
    }

    private static Vector3 WorldToScreenPoint(Vector3 worldPosition)
    {
        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.WorldToScreenPoint(worldPosition) : Vector3.zero;
    }

    private static bool CanMatch(Passenger first, Passenger second)
    {
        if (first == null || second == null || first == second)
            return false;

        return first.IsFemale != second.IsFemale
            && !first.IsInCouple
            && !second.IsInCouple
            && first.IsMatchable
            && second.IsMatchable;
    }

    public bool CanMatchWith(Passenger other)
    {
        return CanMatch(this, other);
    }

    private bool TryMatchWith(Passenger other)
    {
        if (!CanMatch(this, other))
            return false;

        ForceToMatchingState(other);
        other.ForceToMatchingState(this);
        return true;
    }

    private void BreakCoupleOnImpact(Passenger hitter)
    {
        if (!IsInCouple)
            return;

        GetComponentInParent<Couple>()?.BreakByHit(hitter);
    }

    private Vector2 ClampFlightVelocity(Vector2 velocity)
    {
        if (velocity.magnitude > _maxFlightSpeed)
            return velocity.normalized * _maxFlightSpeed;

        return velocity;
    }

    private Vector2 ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        Vector2 reflected = Vector2.Reflect(velocity, normal) * _bounceElasticity;
        reflected *= boostMultiplier;
        return ClampFlightVelocity(reflected);
    }

    private Vector2 GetCurrentVelocity()
    {
        return _rigidbody != null ? _rigidbody.velocity : Vector2.zero;
    }

    private void ApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        if (_rigidbody == null)
            return;

        _rigidbody.velocity = ReflectVelocity(velocity, normal, boostMultiplier);
    }

    private void EnterFallingState(Vector2 initialVelocity)
    {
        EnsureRequiredComponents();
        EnsureStateMachineInitialized();
        ChangeState(fallingState);
        fallingState.SetInitialFallingSpeed(initialVelocity);
    }
}
