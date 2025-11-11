using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds and runs abilities for a passenger.
/// </summary>
[DisallowMultipleComponent]
public class PassengerAbilities : MonoBehaviour
{
    [SerializeField] private List<PassengerAbility> _abilities = new List<PassengerAbility>();

    private Passenger _owner;

    private void Awake()
    {
        _owner = GetComponent<Passenger>();
    }

    public void AttachAll()
    {
        if (_owner == null) _owner = GetComponent<Passenger>();
        for (int i = 0; i < _abilities.Count; i++)
        {
            var a = _abilities[i];
            if (a == null) continue;
            a.OnAttach(_owner);
        }
    }

    public void AddAbility(PassengerAbility ability)
    {
        if (ability == null) return;
        _abilities.Add(ability);
    }

    public void InvokeMatched(Passenger partner, ref int points)
    {
        for (int i = 0; i < _abilities.Count; i++)
        {
            var a = _abilities[i];
            if (a == null) continue;
            a.OnMatched(_owner, partner, ref points);
        }
    }

    public void InvokePairBroken(Passenger hitter)
    {
        for (int i = 0; i < _abilities.Count; i++)
        {
            var a = _abilities[i];
            if (a == null) continue;
            a.OnPairBroken(_owner, hitter);
        }
    }

    public bool HasAbility<T>() where T : PassengerAbility
    {
        for (int i = 0; i < _abilities.Count; i++) if (_abilities[i] is T) return true;
        return false;
    }
}
