using LoveMetro.Core;

namespace LoveMetro.Passengers
{
    internal interface IPassengerMatchHost
    {
        global::Passenger Passenger { get; }
        global::ScoreCounter ScoreCounter { get; }
        IRuntimeServices Services { get; }
        PassengerMatchRuntime MatchRuntime { get; }
    }
}
