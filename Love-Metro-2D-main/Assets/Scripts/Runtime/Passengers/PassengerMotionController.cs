using UnityEngine;

namespace LoveMetro.Passengers
{
    public sealed class PassengerMotionController
    {
        private readonly Rigidbody2D _rigidbody;
        private PassengerMotionConfig _config;
        private float _bounceElasticity;

        public PassengerMotionController(Rigidbody2D rigidbody, PassengerMotionConfig config, float bounceElasticity)
        {
            _rigidbody = rigidbody;
            _config = config;
            _bounceElasticity = Mathf.Max(0f, bounceElasticity);
        }

        public Vector2 CurrentVelocity => _rigidbody != null ? _rigidbody.velocity : Vector2.zero;

        public void Configure(PassengerMotionConfig config, float bounceElasticity)
        {
            _config = config;
            _bounceElasticity = Mathf.Max(0f, bounceElasticity);
        }

        public Vector2 ClampFlightVelocity(Vector2 velocity)
        {
            return _config.ClampVelocity(velocity);
        }

        public Vector2 ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            Vector2 reflected = Vector2.Reflect(velocity, normal) * _bounceElasticity;
            reflected *= Mathf.Max(0f, boostMultiplier);
            return ClampFlightVelocity(reflected);
        }

        public Vector2 ScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale)
        {
            return ClampFlightVelocity(velocity * (speedMultiplier * impulseScale));
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (_rigidbody != null)
                _rigidbody.velocity = velocity;
        }

        public void AddForce(Vector2 force, ForceMode2D forceMode)
        {
            if (_rigidbody != null)
                _rigidbody.AddForce(force, forceMode);
        }
    }
}
