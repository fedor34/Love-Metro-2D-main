using System.Collections.Generic;
using UnityEngine;

namespace LoveMetro.Spawning
{
    public sealed class VipPairAssigner
    {
        private readonly ISpawnRandom _random;

        public VipPairAssigner(ISpawnRandom random = null)
        {
            _random = random ?? new UnitySpawnRandom();
        }

        public bool TryAssignVipPair(
            IReadOnlyList<global::Passenger> spawned,
            global::VipAbility vipAbility,
            float vipPairChance)
        {
            if (vipAbility == null)
            {
                global::Diagnostics.Log("[Spawner][VIP] ability is null - skip");
                return false;
            }

            if (spawned == null || spawned.Count < 2)
                return false;

            if (_random.Value > Mathf.Clamp01(vipPairChance))
                return false;

            global::Passenger female = null;
            global::Passenger male = null;
            for (int i = 0; i < spawned.Count; i++)
            {
                global::Passenger passenger = spawned[i];
                if (passenger == null || passenger.IsInCouple)
                    continue;

                if (passenger.IsFemale && female == null)
                    female = passenger;
                if (!passenger.IsFemale && male == null)
                    male = passenger;
                if (female != null && male != null)
                    break;
            }

            if (female == null || male == null)
            {
                global::Diagnostics.Log("[Spawner][VIP] not found both genders in wave");
                return false;
            }

            ApplyAbility(female, vipAbility);
            ApplyAbility(male, vipAbility);
            global::Diagnostics.Log($"[Spawner][VIP] Assigned to pair: F={female.name} M={male.name}");
            return true;
        }

        public static void ApplyAbility(global::Passenger passenger, global::PassengerAbility ability)
        {
            if (passenger == null || ability == null)
                return;

            global::PassengerAbilities runner = GetOrAddAbilities(passenger);
            runner.AddAbility(ability);
            runner.AttachAll();
        }

        public static global::PassengerAbilities GetOrAddAbilities(global::Passenger passenger)
        {
            global::PassengerAbilities runner = passenger.GetComponent<global::PassengerAbilities>();
            if (runner == null)
                runner = passenger.gameObject.AddComponent<global::PassengerAbilities>();

            return runner;
        }
    }
}
