using UnityEngine;

public partial class Passenger
{
    private void Awake()
    {
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        EnsureSolidColliders(allColliders);
        EnsureRequiredComponents();
        ConfigureRigidbody();
        Diagnostics.Log($"[Passenger][awake] name={name} cols={allColliders?.Length ?? 0} rb(cdm={_rigidbody.collisionDetectionMode}, interp={_rigidbody.interpolation}) layer={gameObject.layer}");

        if (ShouldNormalizeVipCollider())
            NormalizeVipCollider();
    }

    private void Start()
    {
        RegisterInRuntimeSystems();
    }

    private void OnDestroy()
    {
        UnsubscribeCurrentStateFromTrainInertia();
        DetachFromContainer();
        UnregisterFromRuntimeSystems();
    }

    public void RemoveFromContainerAndDestroy()
    {
        DetachFromContainer();
        Destroy(gameObject);
    }

    public void EnterCouple(Transform coupleRoot, Vector3 worldPosition, bool faceRight)
    {
        EnsureRequiredComponents();
        Transport(worldPosition);
        PassangerAnimator.ChangeFacingDirection(faceRight);
        transform.SetParent(coupleRoot);
        IsInCouple = true;
        RefreshCoupleRegistryStatus();
    }

    public void ExitCouple(Vector2 kickVelocity)
    {
        transform.SetParent(null);
        BreakFromCouple(kickVelocity);
    }

    public void BreakFromCouple(Vector2 kickVelocity)
    {
        IsInCouple = false;
        if (_collider != null)
            _collider.enabled = true;

        if (_rigidbody != null)
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;

        if (PassangerAnimator != null)
            PassangerAnimator.ResetAfterPairBreak();

        Launch(kickVelocity);
        IsMatchable = false;
        _rematchEnableTime = Time.time + _rematchCooldown;
        RefreshCoupleRegistryStatus();
        GetAbilities()?.InvokePairBroken(null);
    }

    public string GetCurrentStateName()
    {
        return _currentState != null ? _currentState.GetType().Name : "None";
    }

    private void LogPassengerEvent(string category, string message)
    {
        Diagnostics.Log($"[Passenger][{category}] {message}");
    }

    private void EnsureRequiredComponents()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();

        if (PassangerAnimator == null)
            PassangerAnimator = GetComponent<PassangerAnimator>() ?? gameObject.AddComponent<PassangerAnimator>();

        if (_collider != null)
            return;

        _collider = GetComponent<Collider2D>();
        if (_collider == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = false;
            _collider = collider;
        }
    }

    private static void EnsureSolidColliders(Collider2D[] colliders)
    {
        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].isTrigger = false;
        }
    }

    private void ConfigureRigidbody()
    {
        _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rigidbody.freezeRotation = true;
        _rigidbody.gravityScale = 0f;
    }

    private void RegisterInRuntimeSystems()
    {
        PassengerRegistry.Instance?.Register(this);
        FieldEffectSystem.Instance?.RegisterTarget(this);
    }

    private void UnregisterFromRuntimeSystems()
    {
        PassengerRegistry.Instance?.Unregister(this);
        FieldEffectSystem.Instance?.UnregisterTarget(this);
    }

    private void DetachFromContainer()
    {
        if (container != null)
            container.RemovePassanger(this);
    }

    private void RefreshCoupleRegistryStatus()
    {
        PassengerRegistry.Instance?.UpdateCoupleStatus(this);
    }

    private bool ShouldNormalizeVipCollider()
    {
        if (name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        Animator animator = GetComponent<Animator>();
        string controllerName = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.name
            : string.Empty;
        return !string.IsNullOrEmpty(controllerName)
            && controllerName.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void NormalizeVipCollider()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider2D>();

        if (sprite == null)
        {
            Diagnostics.Warn($"[Passenger][vip-collider] {name}: no sprite - skip");
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
        Diagnostics.Log($"[Passenger][vip-collider] {name}: size={boxCollider.size} offset={boxCollider.offset} spriteSize={spriteSize}");
    }
}
