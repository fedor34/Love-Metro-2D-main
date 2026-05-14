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

    public int CalculateMatchPointsWith(Passenger partner, ScoreCounter scoreCounter = null)
    {
        return EnsureMatchRuntime().CalculateMatchPointsWith(partner, scoreCounter);
    }

    public int CalculateMatchPointsWith(Passenger partner, IScoreService scoreService)
    {
        return EnsureMatchRuntime().CalculateMatchPointsWith(partner, scoreService);
    }

    private void AwardMatchPointsFor(Passenger partner, Vector3 worldPosition)
    {
        EnsureMatchRuntime().AwardMatchPointsFor(partner, worldPosition);
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
