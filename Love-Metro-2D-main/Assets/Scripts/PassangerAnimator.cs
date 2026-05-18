using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PassangerAnimator : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody;

    [SerializeField] private float _movementThreshold = 0.02f;
    [SerializeField] private float _stopHoldSeconds = 0.15f;
    [Header("Walking Animation Speed")]
    [SerializeField] private float _animSpeedMin = 0.8f;
    [SerializeField] private float _animSpeedMax = 1.3f;
    [SerializeField] private float _animSpeedSmoothing = 8f;

    private bool _isWalkingStateForced;
    private float _belowThresholdTimer;
    private float _animSpeedSmoothed = 1f;
    private Vector3 _lastObservedPosition;
    private bool _hasObservedPosition;

    private const string IsWalking = "IsWalking";
    private const string IsFalling = "IsFalling";
    private const string IsHolding = "IsHolding";
    private const string Bumping = "Bumping";

    private void Awake()
    {
        EnsureComponents();
        TrackCurrentPosition();
    }

    private void OnEnable()
    {
        TrackCurrentPosition();
    }

    private Animator GetAnimator()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();
        return _animator;
    }

    private void EnsureComponents()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        EnsureComponents();
        float observedSpeed = CalculateObservedSpeed();
        if (_isWalkingStateForced)
        {
            TrackCurrentPosition();
            return;
        }

        UpdateAutomaticWalking(observedSpeed);
        UpdateAutomaticAnimationSpeed(observedSpeed);
        TrackCurrentPosition();
    }

    public void ChangeFacingDirection(bool isFacingRight)
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = !isFacingRight;
    }

    public void EnterWanderingMode()
    {
        SetHoldingState(false);
        SetFallingState(false);
        EnableAutomaticWalkingAnimation();
    }

    public void ExitWanderingMode()
    {
        ForceWalkingState(false);
    }

    public void EnterHoldingMode()
    {
        SetHoldingState(true);
    }

    public void ExitHoldingMode()
    {
        SetHoldingState(false);
    }

    public void EnterAirborneMode()
    {
        SetHoldingState(false);
        SetFallingState(true);
    }

    public void ExitAirborneMode()
    {
        SetFallingState(false);
    }

    public void EnterMatchingMode()
    {
        SetHoldingState(false);
        SetFallingState(false);
        ForceWalkingState(false);
        ActivateBumping();
    }

    public void ExitMatchingMode()
    {
        EnterWanderingMode();
    }

    public void SetWalkingState(bool isWalking)
    {
        ForceWalkingState(isWalking);
    }

    public void EnableAutomaticWalkingAnimation()
    {
        _isWalkingStateForced = false;
        _belowThresholdTimer = 0f;
        TrackCurrentPosition();
    }

    public void ForceWalkingState(bool isWalking)
    {
        _isWalkingStateForced = true;
        SetWalkingFlag(isWalking);
        TrackCurrentPosition();
    }

    public void SetHoldingState(bool isHolding)
    {
        GetAnimator()?.SetBool(IsHolding, isHolding);
    }

    public void SetFallingState(bool isFalling)
    {
        GetAnimator()?.SetBool(IsFalling, isFalling);
    }

    public void ActivateBumping()
    {
        GetAnimator()?.SetTrigger(Bumping);
    }

    public void ResetAfterPairBreak()
    {
        Animator animator = GetAnimator();
        if (animator == null)
            return;

        animator.ResetTrigger(Bumping);
        SetHoldingState(false);
        SetFallingState(false);
        SetWalkingFlag(true);
        _isWalkingStateForced = false;
        _belowThresholdTimer = 0f;
        _animSpeedSmoothed = 1f;
        TrackCurrentPosition();
        animator.speed = 1f;
        animator.Rebind();
        animator.Update(0f);
    }

    private void UpdateAutomaticWalking(float observedSpeed)
    {
        bool aboveThreshold = observedSpeed > _movementThreshold;

        if (aboveThreshold)
        {
            _belowThresholdTimer = 0f;
            SetWalkingFlag(true);
            return;
        }

        _belowThresholdTimer += Time.deltaTime;
        if (_belowThresholdTimer >= _stopHoldSeconds)
            SetWalkingFlag(false);
    }

    private void UpdateAutomaticAnimationSpeed(float observedSpeed)
    {
        Animator animator = GetAnimator();
        if (animator == null)
            return;

        float t = Mathf.InverseLerp(_movementThreshold, _movementThreshold * 10f + 0.01f, observedSpeed);
        float targetAnimSpeed = Mathf.Lerp(_animSpeedMin, _animSpeedMax, t);
        _animSpeedSmoothed = Mathf.Lerp(_animSpeedSmoothed, targetAnimSpeed, _animSpeedSmoothing * Time.deltaTime);
        animator.speed = _animSpeedSmoothed;
    }

    private float CalculateObservedSpeed()
    {
        float rigidbodySpeed = _rigidbody != null ? _rigidbody.velocity.magnitude : 0f;
        if (!_hasObservedPosition || Time.deltaTime <= 0f)
            return rigidbodySpeed;

        float transformSpeed = CalculateTransformSpeed(transform.position, Time.deltaTime);
        return Mathf.Max(rigidbodySpeed, transformSpeed);
    }

    private float CalculateTransformSpeed(Vector3 currentPosition, float deltaTime)
    {
        if (!_hasObservedPosition || deltaTime <= 0f)
            return 0f;

        return (currentPosition - _lastObservedPosition).magnitude / deltaTime;
    }

    private void TrackCurrentPosition()
    {
        _lastObservedPosition = transform.position;
        _hasObservedPosition = true;
    }

    private void SetWalkingFlag(bool isWalking)
    {
        GetAnimator()?.SetBool(IsWalking, isWalking);
    }
}
