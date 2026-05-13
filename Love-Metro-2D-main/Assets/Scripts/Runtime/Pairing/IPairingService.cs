namespace LoveMetro.Pairing
{
    public interface IPairingService
    {
        PairingResult Evaluate(PairingRequest request);
        bool TryPair(PairingRequest request, out PairingResult result);
    }
}
