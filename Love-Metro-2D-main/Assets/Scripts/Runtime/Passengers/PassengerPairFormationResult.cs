namespace LoveMetro.Passengers
{
    internal enum PassengerPairFormationFailureReason
    {
        None,
        MissingPartner,
        MissingPartnerHost,
        AlreadyInCouple,
        MissingCouplePrefab,
        MissingCoupleComponent
    }

    internal readonly struct PassengerPairFormationResult
    {
        private PassengerPairFormationResult(
            bool success,
            global::Couple couple,
            bool createdByThisPassenger,
            PassengerPairFormationFailureReason failureReason)
        {
            Success = success;
            Couple = couple;
            CreatedByThisPassenger = createdByThisPassenger;
            FailureReason = failureReason;
        }

        public bool Success { get; }
        public global::Couple Couple { get; }
        public bool CreatedByThisPassenger { get; }
        public PassengerPairFormationFailureReason FailureReason { get; }

        public static PassengerPairFormationResult Succeeded(global::Couple couple, bool createdByThisPassenger)
        {
            return new PassengerPairFormationResult(true, couple, createdByThisPassenger, PassengerPairFormationFailureReason.None);
        }

        public static PassengerPairFormationResult Failed(PassengerPairFormationFailureReason failureReason)
        {
            return new PassengerPairFormationResult(false, null, false, failureReason);
        }
    }
}
