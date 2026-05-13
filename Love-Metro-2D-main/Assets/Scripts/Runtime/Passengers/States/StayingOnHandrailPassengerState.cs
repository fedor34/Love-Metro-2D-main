using UnityEngine;

namespace LoveMetro.Passengers.States
{
    public sealed class StayingOnHandrailPassengerState : IPassengerState
    {
        private readonly PassengerStateContext _context;
        private float _expiredTime;
        private float _stayingTime;

        public StayingOnHandrailPassengerState(PassengerStateContext context)
        {
            _context = context;
        }

        public PassengerStateId Id => PassengerStateId.StayingOnHandrail;

        public void OnCollision(Collision2D collision) { }

        public void Exit()
        {
            _context.ReleaseHandrail();
            _context.Animator.ExitHoldingMode();
        }

        public void UpdateState()
        {
            _expiredTime += Time.deltaTime;
            if (_expiredTime > _stayingTime)
                _context.ChangeState(PassengerStateId.Wandering);
        }

        public void Enter()
        {
            ResetTimer();
            _context.SetBodyType(RigidbodyType2D.Static);
            _context.Animator.EnterHoldingMode();
            _context.TimeWithoutHolding = 0f;
        }

        public void OnTrainSpeedChange(Vector2 force)
        {
            ResetTimer();
        }

        public void OnTriggerEnter(Collider2D collision) { }

        private void ResetTimer()
        {
            _expiredTime = 0f;
            _stayingTime = Random.Range(
                _context.HandrailStandingTimeInterval.x,
                _context.HandrailStandingTimeInterval.y);
        }
    }
}
