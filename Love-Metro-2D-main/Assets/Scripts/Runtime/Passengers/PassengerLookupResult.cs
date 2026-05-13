using System.Collections.Generic;

namespace LoveMetro.Passengers
{
    public readonly struct PassengerLookupResult
    {
        public PassengerLookupResult(global::Passenger closestPassenger, IReadOnlyList<global::Passenger> passengers)
        {
            ClosestPassenger = closestPassenger;
            Passengers = passengers;
        }

        public global::Passenger ClosestPassenger { get; }
        public IReadOnlyList<global::Passenger> Passengers { get; }
        public bool HasPassenger => ClosestPassenger != null || (Passengers != null && Passengers.Count > 0);
    }
}
