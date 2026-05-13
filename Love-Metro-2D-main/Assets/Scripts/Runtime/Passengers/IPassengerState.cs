using UnityEngine;

namespace LoveMetro.Passengers
{
    public interface IPassengerState
    {
        void UpdateState();
        void Exit();
        void Enter();
        void OnCollision(Collision2D collision);
        void OnTriggerEnter(Collider2D collision);
        void OnTrainSpeedChange(Vector2 force);
    }
}
