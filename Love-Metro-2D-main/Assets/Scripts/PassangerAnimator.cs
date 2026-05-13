using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PassangerAnimator : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody;

    [SerializeField] private float _movementThreshold = 0.1f;
    [SerializeField] private float _stopHoldSeconds = 0.9f;
    [Header("Walking Animation Speed")]
    [SerializeField] private float _animSpeedMin = 0.8f;
    [SerializeField] private float _animSpeedMax = 1.3f;
    [SerializeField] private float _animSpeedSmoothing = 8f;

    private bool _isWalkingStateForced;
    private float _belowThresholdTimer;
    private float _animSpeedSmoothed = 1f;

    private const string IsWalking = "IsWalking";
    private const string IsFalling = "IsFalling";
    private const string IsHolding = "IsHolding";
    private const string Bumping = "Bumping";

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (_rigidbody == null || _isWalkingStateForced)
            return;

        UpdateAutomaticWalking();
        UpdateAutomaticAnimationSpeed();
    }

    public void ChangeFacingDirection(bool isFacingRight)
    {
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
    }

    public void ForceWalkingState(bool isWalking)
    {
        _isWalkingStateForced = true;
        SetWalkingFlag(isWalking);
    }

    public void SetHoldingState(bool isHolding)
    {
        _animator.SetBool(IsHolding, isHolding);
    }

    public void SetFallingState(bool isFalling)
    {
        _animator.SetBool(IsFalling, isFalling);
    }

    public void ActivateBumping()
    {
        _animator.SetTrigger(Bumping);
    }

    public void ResetAfterPairBreak()
    {
        _animator.ResetTrigger(Bumping);
        SetHoldingState(false);
        SetFallingState(false);
        SetWalkingFlag(true);
        _isWalkingStateForced = false;
        _belowThresholdTimer = 0f;
        _animSpeedSmoothed = 1f;
        _animator.speed = 1f;
        _animator.Rebind();
        _animator.Update(0f);
    }

    private void UpdateAutomaticWalking()
    {
        float velocityMagnitude = _rigidbody.velocity.magnitude;
        bool aboveThreshold = velocityMagnitude > _movementThreshold;

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

    private void UpdateAutomaticAnimationSpeed()
    {
        float velocityMagnitude = _rigidbody.velocity.magnitude;
        float t = Mathf.InverseLerp(_movementThreshold, _movementThreshold * 10f + 0.01f, velocityMagnitude);
        float targetAnimSpeed = Mathf.Lerp(_animSpeedMin, _animSpeedMax, t);
        _animSpeedSmoothed = Mathf.Lerp(_animSpeedSmoothed, targetAnimSpeed, _animSpeedSmoothing * Time.deltaTime);
        _animator.speed = _animSpeedSmoothed;
    }

    private void SetWalkingFlag(bool isWalking)
    {
        _animator.SetBool(IsWalking, isWalking);
    }
}
