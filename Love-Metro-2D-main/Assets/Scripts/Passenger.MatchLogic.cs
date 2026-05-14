using LoveMetro.Core;
using LoveMetro.Passengers;
using LoveMetro.Scoring;
using UnityEngine;

public partial class Passenger
{
    Passenger IPassengerMatchHost.Passenger => this;
    ScoreCounter IPassengerMatchHost.ScoreCounter => _scoreCounter;
    IRuntimeServices IPassengerMatchHost.Services => RuntimeServices.Instance;
    PassengerMatchRuntime IPassengerMatchHost.MatchRuntime => EnsureMatchRuntime();
    PassengerPairFormationRuntime IPassengerMatchHost.PairFormationRuntime => EnsurePairFormationRuntime();
    GameObject IPassengerMatchHost.CouplePrefab => CouplePref;
    Vector3 IPassengerMatchHost.Position => transform.position;
    Couple IPassengerMatchHost.CurrentCouple => _currentCouple;

    void IPassengerMatchHost.ChangeToMatchingState()
    {
        ChangeState(LoveMetro.Passengers.PassengerStateId.Matching);
    }

    public int CalculateMatchPointsWith(Passenger partner, ScoreCounter scoreCounter = null)
    {
        return EnsureMatchRuntime().CalculateMatchPointsWith(partner, scoreCounter);
    }

    public int CalculateMatchPointsWith(Passenger partner, IScoreService scoreService)
    {
        return EnsureMatchRuntime().CalculateMatchPointsWith(partner, scoreService);
    }

    public bool CanMatchWith(Passenger other)
    {
        return EnsureMatchRuntime().CanMatchWith(other);
    }

    private void AttachAbilities()
    {
        EnsureMatchRuntime().AttachAbilities();
    }

    private void InvokePairBroken(Passenger hitter)
    {
        EnsureMatchRuntime().InvokePairBroken(hitter);
    }
}
