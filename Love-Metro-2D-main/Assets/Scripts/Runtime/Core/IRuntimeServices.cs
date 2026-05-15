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
        IManualPairingService ManualPairingService { get; }
        ITrainMotionEvents TrainMotionEvents { get; }
        IStationFlowService StationFlowService { get; }
        IFieldEffectSystem FieldEffectSystem { get; }

        void RegisterPassengerRegistry(IPassengerRegistry service);
        void UnregisterPassengerRegistry(IPassengerRegistry service);
        void RegisterPairingService(IPairingService service);
        void RegisterScoreService(IScoreService service);
        void UnregisterScoreService(IScoreService service);
        void RegisterInputIntentProvider(IInputIntentProvider service);
        void UnregisterInputIntentProvider(IInputIntentProvider service);
        void RegisterManualPairingService(IManualPairingService service);
        void UnregisterManualPairingService(IManualPairingService service);
        void RegisterTrainMotionEvents(ITrainMotionEvents service);
        void UnregisterTrainMotionEvents(ITrainMotionEvents service);
        void RegisterStationFlowService(IStationFlowService service);
        void UnregisterStationFlowService(IStationFlowService service);
        void RegisterFieldEffectSystem(IFieldEffectSystem service);
        void UnregisterFieldEffectSystem(IFieldEffectSystem service);
        void ResetForTests();
    }
}
