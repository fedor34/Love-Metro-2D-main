using UnityEngine;

namespace LoveMetro.Passengers
{
    public sealed class PassengerStateRuntime
    {
        private readonly PassengerStateMachine _stateMachine;
        private readonly PassengerStateFactory _stateFactory;
        private readonly IPassengerState _wanderingState;
        private readonly IPassengerFallingState _fallingState;
        private readonly IPassengerFlyingState _flyingState;
        private readonly IPassengerState _matchingState;
        private readonly IPassengerState _stayingOnHandrailState;
        private readonly IPassengerAbsorptionState _beingAbsorbedState;

        public PassengerStateRuntime(global::Passenger passenger)
            : this(
                (IPassengerStateHost)passenger,
                passenger is IPassengerInteractionHost interactionHost ? interactionHost.InteractionRuntime : null)
        {
        }

        internal PassengerStateRuntime(IPassengerStateHost host)
            : this(host, host is IPassengerInteractionHost interactionHost ? interactionHost.InteractionRuntime : null)
        {
        }

        internal PassengerStateRuntime(IPassengerStateHost host, PassengerInteractionRuntime interactions)
        {
            PassengerStateContext context = new PassengerStateContext(host, interactions);
            _stateMachine = new PassengerStateMachine(context);
            _stateFactory = new PassengerStateFactory(context);

            _wanderingState = _stateFactory.Create(PassengerStateId.Wandering);
            _fallingState = (IPassengerFallingState)_stateFactory.Create(PassengerStateId.Falling);
            _flyingState = (IPassengerFlyingState)_stateFactory.Create(PassengerStateId.Flying);
            _matchingState = _stateFactory.Create(PassengerStateId.Matching);
            _stayingOnHandrailState = _stateFactory.Create(PassengerStateId.StayingOnHandrail);
            _beingAbsorbedState = (IPassengerAbsorptionState)_stateFactory.Create(PassengerStateId.BeingAbsorbed);
        }

        public PassengerStateId? CurrentStateId => _stateMachine.CurrentStateId;
        public string CurrentStateName => _stateMachine.CurrentStateName;
        public bool HasCurrentState => _stateMachine.CurrentState != null;

        public void ConfigureTrain(global::TrainManager train)
        {
            _stateMachine.ConfigureTrain(train);
        }

        public void ChangeState(PassengerStateId id)
        {
            _stateMachine.ChangeState(ResolveState(id));
        }

        public void EnterFalling(Vector2 initialVelocity)
        {
            _stateMachine.ChangeState((IPassengerState)_fallingState);
            _fallingState.SetInitialFallingSpeed(initialVelocity);
        }

        public void EnterFlying(Vector2 windDirection, float windStrength)
        {
            _stateMachine.ChangeState((IPassengerState)_flyingState);
            _flyingState.SetFlyingParameters(windDirection, windStrength);
        }

        public void UpdateFlyingWind(Vector2 windDirection, float windStrength)
        {
            _flyingState.UpdateWindEffect(windDirection, windStrength);
        }

        public void EnterAbsorption(Vector3 center, float force)
        {
            _beingAbsorbedState.SetAbsorptionParameters(center, force);
            _stateMachine.ChangeState((IPassengerState)_beingAbsorbedState);
        }

        public void UpdateState()
        {
            _stateMachine.UpdateState();
        }

        public void OnCollision(Collision2D collision)
        {
            _stateMachine.OnCollision(collision);
        }

        public void OnTriggerEnter(Collider2D collider)
        {
            _stateMachine.OnTriggerEnter(collider);
        }

        public void ForwardTrainSpeedChangeToCurrentState(Vector2 force)
        {
            _stateMachine.CurrentState?.OnTrainSpeedChange(force);
        }

        public void Clear()
        {
            _stateMachine.Clear();
        }

        private IPassengerState ResolveState(PassengerStateId id)
        {
            switch (id)
            {
                case PassengerStateId.Wandering:
                    return _wanderingState;
                case PassengerStateId.Falling:
                    return (IPassengerState)_fallingState;
                case PassengerStateId.Flying:
                    return (IPassengerState)_flyingState;
                case PassengerStateId.Matching:
                    return _matchingState;
                case PassengerStateId.StayingOnHandrail:
                    return _stayingOnHandrailState;
                case PassengerStateId.BeingAbsorbed:
                    return (IPassengerState)_beingAbsorbedState;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(id), id, "Unknown passenger state.");
            }
        }
    }
}
