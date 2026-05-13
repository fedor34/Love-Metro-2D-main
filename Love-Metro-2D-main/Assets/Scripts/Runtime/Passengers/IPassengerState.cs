using UnityEngine;

namespace LoveMetro.Passengers
{
    public interface IPassengerState
    {
        PassengerStateId Id { get; }

        void UpdateState();
        void Exit();
        void Enter();
        void OnCollision(Collision2D collision);
        void OnTriggerEnter(Collider2D collision);
        void OnTrainSpeedChange(Vector2 force);
    }

    public interface IPassengerFallingState : IPassengerState
    {
        void SetInitialFallingSpeed(Vector2 initialSpeed);
    }

    public interface IPassengerFlyingState : IPassengerState
    {
        void SetFlyingParameters(Vector2 windDirection, float windStrength);
        void UpdateWindEffect(Vector2 windDirection, float windStrength);
    }
}
