using UnityEngine;

public partial class Passenger
{
    private class StayingOnHandrail : PassangerState
    {
        private float _expiredTime;
        private float _stayingTime;

        public StayingOnHandrail(Passenger pasanger) : base(pasanger) { }

        public override LoveMetro.Passengers.PassengerStateId Id => LoveMetro.Passengers.PassengerStateId.StayingOnHandrail;

        public override void OnCollision(Collision2D collision) { }

        public override void Exit()
        {
            Passanger.releaseHandrail?.Invoke();
            Passanger.releaseHandrail = null;
            Passanger.PassangerAnimator.ExitHoldingMode();
        }

        public override void UpdateState()
        {
            _expiredTime += Time.deltaTime;
            if (_expiredTime > _stayingTime)
                Passanger.ChangeState(Passanger.wanderingState);
        }

        public override void Enter()
        {
            ResetTimer();
            Passanger._rigidbody.bodyType = RigidbodyType2D.Static;
            Passanger.PassangerAnimator.EnterHoldingMode();
            Passanger._timeWithoutHolding = 0f;
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            ResetTimer();
        }

        public override void OnTriggerEnter(Collider2D collision) { }

        private void ResetTimer()
        {
            _expiredTime = 0f;
            _stayingTime = Random.Range(Passanger.HandrailStandingTimeInterval.x, Passanger.HandrailStandingTimeInterval.y);
        }
    }
}
