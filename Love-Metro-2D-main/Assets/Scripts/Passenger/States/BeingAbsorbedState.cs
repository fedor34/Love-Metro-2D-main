using UnityEngine;

public partial class Passenger
{
    private class BeingAbsorbed : PassangerState
    {
        private Vector3 _absorptionCenter;
        private float _absorptionForce;
        private float _timeInAbsorption;
        private readonly float _maxAbsorptionTime = 3f;

        public BeingAbsorbed(Passenger passenger) : base(passenger) { }

        public void SetAbsorptionParameters(Vector3 center, float force)
        {
            _absorptionCenter = center;
            _absorptionForce = force;
        }

        public override void OnCollision(Collision2D collision) { }

        public override void Exit()
        {
            Passanger.PassangerAnimator.ExitAirborneMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            _timeInAbsorption = 0f;
        }

        public override void UpdateState()
        {
            _timeInAbsorption += Time.deltaTime;

            Vector3 direction = (_absorptionCenter - Passanger.transform.position).normalized;
            float distance = Vector3.Distance(Passanger.transform.position, _absorptionCenter);
            if (distance < 0.5f || _timeInAbsorption > _maxAbsorptionTime)
            {
                Passanger.RemoveFromContainerAndDestroy();
                return;
            }

            Vector2 absorptionForce = direction * _absorptionForce;
            absorptionForce *= 1f / Mathf.Max(distance, 0.1f);
            Passanger._rigidbody.AddForce(absorptionForce, ForceMode2D.Force);
        }

        public override void Enter()
        {
            Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Passanger.PassangerAnimator.EnterAirborneMode();
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            Passanger.IsMatchable = false;
            _timeInAbsorption = 0f;
        }

        public override void OnTrainSpeedChange(Vector2 force) { }

        public override void OnTriggerEnter(Collider2D collision) { }
    }
}
