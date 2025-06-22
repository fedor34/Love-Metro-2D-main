using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PassangerAnimator : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody;

    [SerializeField] private float _movementThreshold = 0.1f; // Минимальная скорость для анимации ходьбы
    private bool _isWalkingStateForced = false; // Флаг принудительного состояния ходьбы

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
        // Автоматическое управление анимацией ходьбы на основе реальной скорости
        if (_rigidbody != null && !_isWalkingStateForced)
        {
            bool shouldWalk = _rigidbody.velocity.magnitude > _movementThreshold;
            _animator.SetBool(IsWalking, shouldWalk);
        }
    }
    
    public void ChangeFacingDirection(bool isFacingRight)
    {
        if (isFacingRight)
            _spriteRenderer.flipX = false;
        else
            _spriteRenderer.flipX = true;
    }

    public void SetWalkingState(bool isWalking)
    {
        _isWalkingStateForced = isWalking;
        _animator.SetBool(IsWalking, isWalking);
    }

    /// <summary>
    /// Включает автоматическое управление анимацией ходьбы на основе реальной скорости движения
    /// </summary>
    public void EnableAutomaticWalkingAnimation()
    {
        _isWalkingStateForced = false;
    }

    /// <summary>
    /// Принудительно устанавливает состояние ходьбы (отключает автоматику)
    /// </summary>
    public void ForceWalkingState(bool isWalking)
    {
        _isWalkingStateForced = true;
        _animator.SetBool(IsWalking, isWalking);
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
}
