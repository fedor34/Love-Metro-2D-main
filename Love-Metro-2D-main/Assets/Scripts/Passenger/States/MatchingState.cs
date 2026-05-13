using UnityEngine;

public partial class Passenger
{
    private class Matching : PassangerState
    {
        public Matching(Passenger pasanger) : base(pasanger) { }

        public override LoveMetro.Passengers.PassengerStateId Id => LoveMetro.Passengers.PassengerStateId.Matching;

        public override void OnCollision(Collision2D collision) { }

        public override void Exit()
        {
            Passanger.IsMatchable = true;
            if (Passanger._rigidbody != null)
                Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            if (Passanger._collider != null)
                Passanger._collider.enabled = true;
            Passanger.PassangerAnimator.ExitMatchingMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
        }

        public override void UpdateState() { }

        public override void Enter()
        {
            Passanger.IsMatchable = false;
            Passanger._rigidbody.bodyType = RigidbodyType2D.Static;
            Passanger.PassangerAnimator.EnterMatchingMode();
            Passanger._collider.enabled = false;
        }

        public override void OnTrainSpeedChange(Vector2 force) { }

        public override void OnTriggerEnter(Collider2D collision) { }
    }
}
