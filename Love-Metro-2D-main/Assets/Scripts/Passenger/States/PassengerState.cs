using UnityEngine;

public partial class Passenger
{
    private abstract class PassangerState : LoveMetro.Passengers.IPassengerState
    {
        protected Passenger Passanger;

        protected PassangerState(Passenger pasanger)
        {
            Passanger = pasanger;
        }

        public abstract LoveMetro.Passengers.PassengerStateId Id { get; }

        public abstract void UpdateState();
        public abstract void Exit();
        public abstract void Enter();
        public abstract void OnCollision(Collision2D collision);
        public abstract void OnTriggerEnter(Collider2D collision);
        public abstract void OnTrainSpeedChange(Vector2 force);
    }
}
