using LoveMetro.FieldEffects;
using LoveMetro.Input;
using LoveMetro.Pairing;
using LoveMetro.Scoring;
using LoveMetro.Train;

namespace LoveMetro.Core
{
    public sealed class RuntimeServices : IRuntimeServices
    {
        private static readonly RuntimeServices SharedInstance = new RuntimeServices();

        public static RuntimeServices Instance => SharedInstance;

        private RuntimeServices()
        {
            PairingService = new PairingService();
            ScoreService = new ScoreService();
        }

        public IPassengerRegistry PassengerRegistry { get; private set; }
        public IPairingService PairingService { get; private set; }
        public IScoreService ScoreService { get; private set; }
        public IInputIntentProvider InputIntentProvider { get; private set; }
        public IManualPairingService ManualPairingService { get; private set; }
        public ITrainMotionEvents TrainMotionEvents { get; private set; }
        public IStationFlowService StationFlowService { get; private set; }
        public IFieldEffectSystem FieldEffectSystem { get; private set; }

        public void RegisterPassengerRegistry(IPassengerRegistry service)
        {
            PassengerRegistry = service;
        }

        public void UnregisterPassengerRegistry(IPassengerRegistry service)
        {
            if (ReferenceEquals(PassengerRegistry, service))
                PassengerRegistry = null;
        }

        public void RegisterPairingService(IPairingService service)
        {
            PairingService = service ?? new PairingService();
        }

        public void RegisterScoreService(IScoreService service)
        {
            ScoreService = service ?? new ScoreService();
        }

        public void UnregisterScoreService(IScoreService service)
        {
            if (ReferenceEquals(ScoreService, service))
                ScoreService = new ScoreService();
        }

        public void RegisterInputIntentProvider(IInputIntentProvider service)
        {
            InputIntentProvider = service;
        }

        public void UnregisterInputIntentProvider(IInputIntentProvider service)
        {
            if (ReferenceEquals(InputIntentProvider, service))
                InputIntentProvider = null;
        }

        public void RegisterManualPairingService(IManualPairingService service)
        {
            ManualPairingService = service;
        }

        public void UnregisterManualPairingService(IManualPairingService service)
        {
            if (ReferenceEquals(ManualPairingService, service))
                ManualPairingService = null;
        }

        public void RegisterTrainMotionEvents(ITrainMotionEvents service)
        {
            TrainMotionEvents = service;
        }

        public void UnregisterTrainMotionEvents(ITrainMotionEvents service)
        {
            if (ReferenceEquals(TrainMotionEvents, service))
                TrainMotionEvents = null;
        }

        public void RegisterStationFlowService(IStationFlowService service)
        {
            StationFlowService = service;
        }

        public void UnregisterStationFlowService(IStationFlowService service)
        {
            if (ReferenceEquals(StationFlowService, service))
                StationFlowService = null;
        }

        public void RegisterFieldEffectSystem(IFieldEffectSystem service)
        {
            FieldEffectSystem = service;
        }

        public void UnregisterFieldEffectSystem(IFieldEffectSystem service)
        {
            if (ReferenceEquals(FieldEffectSystem, service))
                FieldEffectSystem = null;
        }

        public void ResetForTests()
        {
            PassengerRegistry = null;
            PairingService = new PairingService();
            ScoreService = new ScoreService();
            InputIntentProvider = null;
            ManualPairingService = null;
            TrainMotionEvents = null;
            StationFlowService = null;
            FieldEffectSystem = null;
        }
    }
}
