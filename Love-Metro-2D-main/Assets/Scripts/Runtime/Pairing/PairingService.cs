namespace LoveMetro.Pairing
{
    public sealed class PairingService : IPairingService
    {
        public PairingResult Evaluate(PairingRequest request)
        {
            global::Passenger first = request.First;
            global::Passenger second = request.Second;
            float distance = request.Distance;

            if (first == null || second == null)
                return PairingResult.Failed(PairingFailureReason.MissingPassenger, distance, request.Source);

            if (ReferenceEquals(first, second))
                return PairingResult.Failed(PairingFailureReason.SamePassenger, distance, request.Source);

            if (first.IsFemale == second.IsFemale)
                return PairingResult.Failed(PairingFailureReason.SameGender, distance, request.Source);

            if (first.IsInCouple || second.IsInCouple)
                return PairingResult.Failed(PairingFailureReason.AlreadyInCouple, distance, request.Source);

            if (!first.IsMatchable || !second.IsMatchable)
                return PairingResult.Failed(PairingFailureReason.NotMatchable, distance, request.Source);

            if (request.HasDistanceLimit && distance > request.MaxDistance)
                return PairingResult.Failed(PairingFailureReason.TooFar, distance, request.Source);

            return PairingResult.Succeeded(distance, request.Source);
        }

        public bool TryPair(PairingRequest request, out PairingResult result)
        {
            result = Evaluate(request);
            if (!result.Success)
                return false;

            request.First.ForceToMatchingState(request.Second);
            request.Second.ForceToMatchingState(request.First);
            return true;
        }
    }
}
