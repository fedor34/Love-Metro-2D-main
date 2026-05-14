using LoveMetro.Core;
using UnityEngine;

namespace LoveMetro.Passengers
{
    internal interface IPassengerInteractionHost
    {
        global::Passenger Passenger { get; }
        Vector3 Position { get; }
        PassengerStateTuning Tuning { get; }
        PassengerPhysicsRuntime PhysicsRuntime { get; }
        IRuntimeServices Services { get; }
        PassengerInteractionRuntime InteractionRuntime { get; }

        void BreakCoupleOnImpact(global::Passenger hitter);
        bool TryMatchWith(global::Passenger other);
    }
}
