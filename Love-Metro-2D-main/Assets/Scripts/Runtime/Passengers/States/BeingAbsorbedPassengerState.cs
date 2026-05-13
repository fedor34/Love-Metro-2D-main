using UnityEngine;

namespace LoveMetro.Passengers.States
{
    public sealed class BeingAbsorbedPassengerState : IPassengerAbsorptionState
    {
        private readonly PassengerStateContext _context;
        private readonly float _maxAbsorptionTime = 3f;
        private Vector3 _absorptionCenter;
        private float _absorptionForce;
        private float _timeInAbsorption;

        public BeingAbsorbedPassengerState(PassengerStateContext context)
        {
            _context = context;
        }

        public PassengerStateId Id => PassengerStateId.BeingAbsorbed;

        public void SetAbsorptionParameters(Vector3 center, float force)
        {
            _absorptionCenter = center;
            _absorptionForce = force;
        }

        public void OnCollision(Collision2D collision) { }

        public void Exit()
        {
            _context.Animator.ExitAirborneMode();
            _context.SetDefaultLayer();
            _timeInAbsorption = 0f;
        }

        public void UpdateState()
        {
            _timeInAbsorption += Time.deltaTime;

            Vector3 direction = (_absorptionCenter - _context.Position).normalized;
            float distance = Vector3.Distance(_context.Position, _absorptionCenter);
            if (distance < 0.5f || _timeInAbsorption > _maxAbsorptionTime)
            {
                _context.RemovePassengerAndDestroy();
                return;
            }

            Vector2 absorptionForce = (Vector2)direction * _absorptionForce;
            absorptionForce *= 1f / Mathf.Max(distance, 0.1f);
            _context.AddForce(absorptionForce, ForceMode2D.Force);
        }

        public void Enter()
        {
            _context.SetBodyType(RigidbodyType2D.Dynamic);
            _context.Animator.EnterAirborneMode();
            _context.SetDefaultLayer();
            _context.IsMatchable = false;
            _timeInAbsorption = 0f;
        }

        public void OnTrainSpeedChange(Vector2 force) { }

        public void OnTriggerEnter(Collider2D collision) { }
    }
}
