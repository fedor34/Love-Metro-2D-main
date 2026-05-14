using LoveMetro.Core;
using UnityEngine;

namespace LoveMetro.Passengers
{
    internal sealed class PassengerPairFormationRuntime
    {
        private readonly IPassengerMatchHost _host;

        public PassengerPairFormationRuntime(IPassengerMatchHost host)
        {
            _host = host ?? throw new System.ArgumentNullException(nameof(host));
        }

        public PassengerPairFormationResult FormPairWith(global::Passenger partner)
        {
            if (partner == null)
            {
                _host.ChangeToMatchingState();
                return PassengerPairFormationResult.Failed(PassengerPairFormationFailureReason.MissingPartner);
            }

            if (_host.Passenger.IsInCouple || partner.IsInCouple)
            {
                _host.ChangeToMatchingState();
                ResolveHost(partner)?.ChangeToMatchingState();
                return PassengerPairFormationResult.Failed(PassengerPairFormationFailureReason.AlreadyInCouple);
            }

            IPassengerMatchHost partnerHost = ResolveHost(partner);
            if (partnerHost == null)
                return PassengerPairFormationResult.Failed(PassengerPairFormationFailureReason.MissingPartnerHost);

            IPassengerMatchHost creator = ResolveCreator(_host, partnerHost);
            IPassengerMatchHost other = ReferenceEquals(creator, _host) ? partnerHost : _host;

            GameObject couplePrefab = creator.CouplePrefab;
            if (couplePrefab == null)
                return PassengerPairFormationResult.Failed(PassengerPairFormationFailureReason.MissingCouplePrefab);

            GameObject coupleObject = Object.Instantiate(couplePrefab);
            global::Couple couple = coupleObject.GetComponent<global::Couple>();
            if (couple == null)
            {
                UnityLifecycle.SafeDestroy(coupleObject);
                return PassengerPairFormationResult.Failed(PassengerPairFormationFailureReason.MissingCoupleComponent);
            }

            couple.Init(creator.Passenger, other.Passenger);
            creator.MatchRuntime.AwardMatchPointsFor(other.Passenger, couple.transform.position);

            _host.ChangeToMatchingState();
            partnerHost.ChangeToMatchingState();

            return PassengerPairFormationResult.Succeeded(couple, ReferenceEquals(creator, _host));
        }

        private static IPassengerMatchHost ResolveCreator(IPassengerMatchHost first, IPassengerMatchHost second)
        {
            return first.Position.x <= second.Position.x ? first : second;
        }

        private static IPassengerMatchHost ResolveHost(global::Passenger passenger)
        {
            return passenger as IPassengerMatchHost;
        }
    }
}
