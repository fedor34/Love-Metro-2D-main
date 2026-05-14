using LoveMetro.Pairing;
using LoveMetro.Scoring;
using UnityEngine;

namespace LoveMetro.Passengers
{
    internal sealed class PassengerMatchRuntime
    {
        private readonly IPassengerMatchHost _host;
        private global::PassengerAbilities _abilities;

        public PassengerMatchRuntime(IPassengerMatchHost host)
        {
            _host = host ?? throw new System.ArgumentNullException(nameof(host));
        }

        public bool CanMatchWith(global::Passenger other)
        {
            return CanMatch(_host.Passenger, other);
        }

        public int CalculateMatchPointsWith(global::Passenger partner, global::ScoreCounter scoreCounter = null)
        {
            return CalculateMatchPointsWith(partner, ResolveScoreService(scoreCounter));
        }

        public int CalculateMatchPointsWith(global::Passenger partner, IScoreService scoreService)
        {
            int points = scoreService?.BasePointsPerCouple ?? 0;
            InvokeMatched(partner, ref points);
            ResolveMatchRuntime(partner)?.InvokeMatched(_host.Passenger, ref points);
            return Mathf.Max(0, points);
        }

        public void AwardMatchPointsFor(global::Passenger partner, Vector3 worldPosition)
        {
            IScoreService scoreService = ResolveScoreService();
            if (scoreService == null)
                return;

            scoreService.AwardMatchPoints(worldPosition, CalculateMatchPointsWith(partner, scoreService));
        }

        public bool TryResolvePassengerImpact(global::Passenger other)
        {
            if (other == null)
                return false;

            BreakCoupleOnImpact(other);
            ResolveMatchRuntime(other)?.BreakCoupleOnImpact(_host.Passenger);
            return TryMatchWith(other);
        }

        public bool TryMatchWith(global::Passenger other)
        {
            IPairingService service = _host.Services?.PairingService;
            if (service != null)
                return service.TryPair(new PairingRequest(_host.Passenger, other, source: "collision"), out _);

            if (!CanMatch(_host.Passenger, other))
                return false;

            _host.Passenger.ForceToMatchingState(other);
            other.ForceToMatchingState(_host.Passenger);
            return true;
        }

        public void BreakCoupleOnImpact(global::Passenger hitter)
        {
            global::Passenger passenger = _host.Passenger;
            if (!passenger.IsInCouple)
                return;

            passenger.GetComponentInParent<global::Couple>()?.BreakByHit(hitter);
        }

        public void AttachAbilities()
        {
            EnsureAbilities()?.AttachAll();
        }

        public void InvokePairBroken(global::Passenger hitter)
        {
            EnsureAbilities()?.InvokePairBroken(hitter);
        }

        internal void InvokeMatched(global::Passenger partner, ref int points)
        {
            EnsureAbilities()?.InvokeMatched(partner, ref points);
        }

        public static bool CanMatch(global::Passenger first, global::Passenger second)
        {
            if (first == null || second == null || first == second)
                return false;

            return first.IsFemale != second.IsFemale
                && !first.IsInCouple
                && !second.IsInCouple
                && first.IsMatchable
                && second.IsMatchable;
        }

        private IScoreService ResolveScoreService(global::ScoreCounter scoreCounter = null)
        {
            global::ScoreCounter concreteScoreCounter = scoreCounter != null
                ? scoreCounter
                : _host.ScoreCounter;

            return concreteScoreCounter != null
                ? concreteScoreCounter
                : _host.Services?.ScoreService;
        }

        private global::PassengerAbilities EnsureAbilities()
        {
            if (_abilities == null)
                _abilities = _host.Passenger.GetComponent<global::PassengerAbilities>();

            return _abilities;
        }

        private static PassengerMatchRuntime ResolveMatchRuntime(global::Passenger passenger)
        {
            return (passenger as IPassengerMatchHost)?.MatchRuntime;
        }
    }
}
