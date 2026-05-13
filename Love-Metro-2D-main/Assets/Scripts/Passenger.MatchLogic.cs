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
        return CalculateMatchPointsWith(partner, ResolveScoreService(scoreCounter));
    }

    public int CalculateMatchPointsWith(Passenger partner, LoveMetro.Scoring.IScoreService scoreService)
    {
        int points = scoreService?.BasePointsPerCouple ?? 0;
        GetAbilities()?.InvokeMatched(partner, ref points);
        partner?.GetAbilities()?.InvokeMatched(this, ref points);
        return Mathf.Max(0, points);
    }

    private ScoreCounter ResolveScoreCounter(ScoreCounter scoreCounter = null)
    {
        return scoreCounter != null ? scoreCounter : _scoreCounter;
    }

    private LoveMetro.Scoring.IScoreService ResolveScoreService(ScoreCounter scoreCounter = null)
    {
        ScoreCounter concreteScoreCounter = ResolveScoreCounter(scoreCounter);
        return concreteScoreCounter != null
            ? concreteScoreCounter
            : LoveMetro.Core.RuntimeServices.Instance.ScoreService;
    }

    private void AwardMatchPointsFor(Passenger partner, Vector3 worldPosition)
    {
        LoveMetro.Scoring.IScoreService scoreService = ResolveScoreService();
        if (scoreService == null)
            return;

        scoreService.AwardMatchPoints(worldPosition, CalculateMatchPointsWith(partner, scoreService));
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
        LoveMetro.Pairing.IPairingService service = LoveMetro.Core.RuntimeServices.Instance.PairingService;
        if (service != null)
            return service.TryPair(new LoveMetro.Pairing.PairingRequest(this, other, source: "collision"), out _);

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
        return EnsureMotionController().ClampFlightVelocity(velocity);
    }

    private Vector2 ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        return EnsureMotionController().ReflectVelocity(velocity, normal, boostMultiplier);
    }

    private Vector2 ScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale)
    {
        return EnsureMotionController().ScaleLaunchVelocity(velocity, speedMultiplier, impulseScale);
    }

    private Vector2 GetCurrentVelocity()
    {
        return EnsureMotionController().CurrentVelocity;
    }

    private void ApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
    {
        EnsureMotionController().SetVelocity(ReflectVelocity(velocity, normal, boostMultiplier));
    }

    private void EnterFallingState(Vector2 initialVelocity)
    {
        EnsureRequiredComponents();
        EnsureStateMachineInitialized();
        ChangeState(fallingState);
        fallingState.SetInitialFallingSpeed(initialVelocity);
    }

    private LoveMetro.Passengers.PassengerMotionController EnsureMotionController()
    {
        if (_motionController == null)
            ConfigureMotionController();

        return _motionController;
    }

    private void ConfigureMotionController()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();

        LoveMetro.Passengers.PassengerMotionConfig config = new LoveMetro.Passengers.PassengerMotionConfig(
            _maxFlightSpeed,
            _minFallingSpeed,
            _magnetRadius,
            _magnetForce,
            _repelRadius,
            _repelForce,
            _rematchCooldown);

        if (_motionController == null)
            _motionController = new LoveMetro.Passengers.PassengerMotionController(_rigidbody, config, _bounceElasticity);
        else
            _motionController.Configure(config, _bounceElasticity);
    }
}
