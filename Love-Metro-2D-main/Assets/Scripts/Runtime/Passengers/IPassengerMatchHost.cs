using LoveMetro.Core;
using UnityEngine;

namespace LoveMetro.Passengers
{
    internal interface IPassengerMatchHost
    {
        global::Passenger Passenger { get; }
        global::ScoreCounter ScoreCounter { get; }
        IRuntimeServices Services { get; }
        PassengerMatchRuntime MatchRuntime { get; }
        PassengerPairFormationRuntime PairFormationRuntime { get; }
        GameObject CouplePrefab { get; }
        Vector3 Position { get; }
        global::Couple CurrentCouple { get; }

        void ChangeToMatchingState();
    }
}
