using UnityEngine;

namespace LoveMetro.Passengers.States
{
    public sealed class MatchingPassengerState : IPassengerState
    {
        private readonly PassengerStateContext _context;

        public MatchingPassengerState(PassengerStateContext context)
        {
            _context = context;
        }

        public PassengerStateId Id => PassengerStateId.Matching;

        public void OnCollision(Collision2D collision) { }

        public void Exit()
        {
            _context.IsMatchable = true;
            _context.SetBodyType(RigidbodyType2D.Dynamic);
            _context.SetColliderEnabled(true);
            _context.Animator.ExitMatchingMode();
            _context.SetDefaultLayer();
        }

        public void UpdateState() { }

        public void Enter()
        {
            _context.IsMatchable = false;
            _context.SetBodyType(RigidbodyType2D.Static);
            _context.Animator.EnterMatchingMode();
            _context.SetColliderEnabled(false);
        }

        public void OnTrainSpeedChange(Vector2 force) { }

        public void OnTriggerEnter(Collider2D collision) { }
    }
}
