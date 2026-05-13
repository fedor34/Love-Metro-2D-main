using UnityEngine;

namespace LoveMetro.Passengers
{
    internal sealed class PassengerPhysicsRuntime
    {
        private readonly global::Passenger _owner;
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private PassangerAnimator _animator;
        private PassengerMotionController _motionController;

        public PassengerPhysicsRuntime(global::Passenger owner)
        {
            _owner = owner;
        }

        public Rigidbody2D Rigidbody => _rigidbody;
        public Collider2D Collider => _collider;
        public PassangerAnimator Animator => _animator;
        public Vector2 CurrentVelocity => EnsureMotionController().CurrentVelocity;

        public int EnsureSolidChildColliders()
        {
            Collider2D[] colliders = _owner.GetComponentsInChildren<Collider2D>(includeInactive: true);
            if (colliders == null)
                return 0;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].isTrigger = false;
            }

            return colliders.Length;
        }

        public void EnsureRequiredComponents()
        {
            if (_rigidbody == null)
                _rigidbody = _owner.GetComponent<Rigidbody2D>() ?? _owner.gameObject.AddComponent<Rigidbody2D>();

            if (_animator == null)
                _animator = _owner.GetComponent<PassangerAnimator>() ?? _owner.gameObject.AddComponent<PassangerAnimator>();

            if (_collider == null)
            {
                _collider = _owner.GetComponent<Collider2D>();
                if (_collider == null)
                {
                    CircleCollider2D collider = _owner.gameObject.AddComponent<CircleCollider2D>();
                    collider.isTrigger = false;
                    _collider = collider;
                }
            }

            ResetCollisionFilters();
        }

        public void ConfigureRigidbody(PassengerSettings settings)
        {
            EnsureRequiredComponents();
            settings = PassengerSettings.Resolve(settings);

            _rigidbody.collisionDetectionMode = settings.collisionDetectionMode;
            _rigidbody.interpolation = settings.interpolation;
            _rigidbody.freezeRotation = settings.freezeRotation;
            _rigidbody.gravityScale = settings.gravityScale;
            _rigidbody.drag = settings.defaultLinearDamping;
            _rigidbody.angularDrag = settings.defaultAngularDamping;
            ResetCollisionFilters();
        }

        public void ConfigureMotion(PassengerMotionConfig config, float bounceElasticity)
        {
            EnsureRequiredComponents();
            if (_motionController == null)
                _motionController = new PassengerMotionController(_rigidbody, config, bounceElasticity);
            else
                _motionController.Configure(config, bounceElasticity);
        }

        public void ResetCollisionFilters()
        {
            if (_rigidbody != null)
            {
                _rigidbody.includeLayers = Physics2D.AllLayers;
                _rigidbody.excludeLayers = 0;
            }

            if (_collider != null)
            {
                _collider.isTrigger = false;
                _collider.includeLayers = Physics2D.AllLayers;
                _collider.excludeLayers = 0;
            }
        }

        public void SetBodyType(RigidbodyType2D bodyType)
        {
            EnsureRequiredComponents();
            _rigidbody.bodyType = bodyType;
        }

        public void SetDefaultLayer(string defaultLayer)
        {
            _owner.gameObject.layer = LayerMask.NameToLayer(defaultLayer);
        }

        public void SetColliderEnabled(bool enabled)
        {
            EnsureRequiredComponents();
            _collider.enabled = enabled;
        }

        public void SetDamping(float linearDamping, float angularDamping)
        {
            EnsureRequiredComponents();
            _rigidbody.drag = linearDamping;
            _rigidbody.angularDrag = angularDamping;
        }

        public void SetLinearDamping(float linearDamping)
        {
            EnsureRequiredComponents();
            _rigidbody.drag = linearDamping;
        }

        public int GetContacts(ContactPoint2D[] contactPoints)
        {
            EnsureRequiredComponents();
            return _rigidbody.GetContacts(contactPoints);
        }

        public void SetVelocity(Vector2 velocity)
        {
            EnsureMotionController().SetVelocity(velocity);
        }

        public void AddForce(Vector2 force, ForceMode2D mode)
        {
            EnsureMotionController().AddForce(force, mode);
        }

        public Vector2 ClampFlightVelocity(Vector2 velocity)
        {
            return EnsureMotionController().ClampFlightVelocity(velocity);
        }

        public Vector2 ReflectVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            return EnsureMotionController().ReflectVelocity(velocity, normal, boostMultiplier);
        }

        public Vector2 ScaleLaunchVelocity(Vector2 velocity, float speedMultiplier, float impulseScale)
        {
            return EnsureMotionController().ScaleLaunchVelocity(velocity, speedMultiplier, impulseScale);
        }

        public void ApplyReflectedVelocity(Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            SetVelocity(ReflectVelocity(velocity, normal, boostMultiplier));
        }

        public string DescribeRigidbody()
        {
            if (_rigidbody == null)
                return "rb(<null>)";

            return $"rb(cdm={_rigidbody.collisionDetectionMode}, interp={_rigidbody.interpolation}, drag={_rigidbody.drag:F2})";
        }

        public void NormalizeVipColliderIfNeeded()
        {
            if (!ShouldNormalizeVipCollider())
                return;

            SpriteRenderer spriteRenderer = _owner.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            BoxCollider2D boxCollider = _owner.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
                boxCollider = _owner.gameObject.AddComponent<BoxCollider2D>();

            if (sprite == null)
            {
                Diagnostics.Warn($"[Passenger][vip-collider] {_owner.name}: no sprite - skip");
                return;
            }

            Vector2 spriteSize = sprite.bounds.size;
            float width = Mathf.Clamp(spriteSize.x * 0.92f, 0.6f, spriteSize.x * 1.05f);
            float height = Mathf.Clamp(spriteSize.y * 0.13f, 0.35f, spriteSize.y * 0.6f);
            float footMargin = Mathf.Clamp(spriteSize.y * 0.02f, 0.02f, 0.2f);
            float offsetY = (-spriteSize.y * 0.5f) + (height * 0.5f) + footMargin;

            boxCollider.size = new Vector2(width, height);
            boxCollider.offset = new Vector2(0f, offsetY);
            boxCollider.isTrigger = false;
            boxCollider.usedByEffector = false;
            _collider = boxCollider;
            ResetCollisionFilters();
            Diagnostics.Log($"[Passenger][vip-collider] {_owner.name}: size={boxCollider.size} offset={boxCollider.offset} spriteSize={spriteSize}");
        }

        private PassengerMotionController EnsureMotionController()
        {
            if (_motionController == null)
            {
                PassengerSettings settings = _owner.Settings;
                ConfigureMotion(PassengerMotionConfig.FromSettings(settings), settings.bounceElasticity);
            }

            return _motionController;
        }

        private bool ShouldNormalizeVipCollider()
        {
            if (_owner.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            Animator animator = _owner.GetComponent<Animator>();
            string controllerName = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : string.Empty;
            return !string.IsNullOrEmpty(controllerName)
                && controllerName.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
