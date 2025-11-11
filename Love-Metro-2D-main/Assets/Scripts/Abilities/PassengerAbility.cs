using UnityEngine;

/// <summary>
/// Base class for passenger abilities. Implement as ScriptableObject assets.
/// </summary>
public abstract class PassengerAbility : ScriptableObject
{
    // Called once when ability is attached to a spawned passenger
    public virtual void OnAttach(Passenger self) {}

    // Called when self forms a couple with partner; implement to modify points
    public virtual void OnMatched(Passenger self, Passenger partner, ref int points) {}

    // Called when pair is broken
    public virtual void OnPairBroken(Passenger self, Passenger hitter) {}
}

