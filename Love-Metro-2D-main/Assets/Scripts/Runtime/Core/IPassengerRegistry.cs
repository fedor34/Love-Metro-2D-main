using System.Collections.Generic;

namespace LoveMetro.Core
{
    public interface IPassengerRegistry
    {
        IReadOnlyList<global::Passenger> AllPassengers { get; }
        IReadOnlyList<global::Passenger> Males { get; }
        IReadOnlyList<global::Passenger> Females { get; }
        IReadOnlyList<global::Passenger> Singles { get; }
        int MaleSinglesCount { get; }
        int FemaleSinglesCount { get; }

        void Register(global::Passenger passenger);
        void Unregister(global::Passenger passenger);
        void UpdateCoupleStatus(global::Passenger passenger);
        global::Passenger FindClosestOpposite(global::Passenger self, float radius);
        void GetSameGenderInRadius(global::Passenger self, float radius, List<global::Passenger> results);
        int GetPossiblePairsCount();
        void CleanupNullReferences();
        void ClearAll();
    }
}
