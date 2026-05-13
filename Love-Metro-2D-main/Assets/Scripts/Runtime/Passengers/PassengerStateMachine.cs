using UnityEngine;

namespace LoveMetro.Passengers
{
    public sealed class PassengerStateMachine
    {
        private global::TrainManager _train;

        public PassengerStateMachine(PassengerStateContext context)
        {
            Context = context;
        }

        public PassengerStateContext Context { get; }
        public IPassengerState CurrentState { get; private set; }
        public string CurrentStateName => CurrentState != null ? CurrentState.GetType().Name : "None";

        public void ConfigureTrain(global::TrainManager train)
        {
            if (_train == train)
                return;

            UnsubscribeFromTrainInertia();
            _train = train;
            SubscribeToTrainInertia();
        }

        public void ChangeState(IPassengerState newState)
        {
            if (CurrentState != null)
            {
                CurrentState.Exit();
                UnsubscribeFromTrainInertia();
            }

            CurrentState = newState;

            if (CurrentState != null)
            {
                CurrentState.Enter();
                SubscribeToTrainInertia();
            }
        }

        public void UpdateState()
        {
            CurrentState?.UpdateState();
        }

        public void OnCollision(Collision2D collision)
        {
            CurrentState?.OnCollision(collision);
        }

        public void OnTriggerEnter(Collider2D collider)
        {
            CurrentState?.OnTriggerEnter(collider);
        }

        public void Clear()
        {
            UnsubscribeFromTrainInertia();
            CurrentState = null;
            _train = null;
        }

        private void SubscribeToTrainInertia()
        {
            if (_train == null || CurrentState == null)
                return;

            _train.startInertia -= CurrentState.OnTrainSpeedChange;
            _train.startInertia += CurrentState.OnTrainSpeedChange;
        }

        private void UnsubscribeFromTrainInertia()
        {
            if (_train != null && CurrentState != null)
                _train.startInertia -= CurrentState.OnTrainSpeedChange;
        }
    }
}
