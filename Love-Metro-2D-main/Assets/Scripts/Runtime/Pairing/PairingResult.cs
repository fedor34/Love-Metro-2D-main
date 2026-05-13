namespace LoveMetro.Pairing
{
    public enum PairingFailureReason
    {
        None,
        MissingPassenger,
        SamePassenger,
        SameGender,
        AlreadyInCouple,
        NotMatchable,
        TooFar
    }

    public readonly struct PairingResult
    {
        private PairingResult(bool success, PairingFailureReason failureReason, float distance, string source)
        {
            Success = success;
            FailureReason = failureReason;
            Distance = distance;
            Source = source;
        }

        public bool Success { get; }
        public PairingFailureReason FailureReason { get; }
        public float Distance { get; }
        public string Source { get; }

        public static PairingResult Succeeded(float distance, string source)
        {
            return new PairingResult(true, PairingFailureReason.None, distance, source);
        }

        public static PairingResult Failed(PairingFailureReason reason, float distance, string source)
        {
            return new PairingResult(false, reason, distance, source);
        }
    }
}
