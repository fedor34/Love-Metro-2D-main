using LoveMetro.FieldEffects;
using LoveMetro.Input;
using LoveMetro.Pairing;
using LoveMetro.Scoring;
using LoveMetro.Train;

namespace LoveMetro.Core
{
    public interface IRuntimeServices
    {
        IPassengerRegistry PassengerRegistry { get; }
        IPairingService PairingService { get; }
        IScoreService ScoreService { get; }
        IInputIntentProvider InputIntentProvider { get; }
        ITrainMotionEvents TrainMotionEvents { get; }
        IFieldEffectSystem FieldEffectSystem { get; }
    }
}
