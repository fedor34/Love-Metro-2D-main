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
    [SerializeField] private float _stopHoldSeconds = 0.9f;   // Задержка перед остановкой анимации (0.8–1.0 с)
    [Header("Скорость анимации ходьбы")]
    [SerializeField] private float _animSpeedMin = 0.8f;
    [SerializeField] private float _animSpeedMax = 1.3f;
    [SerializeField] private float _animSpeedSmoothing = 8f;
    private bool _isWalkingStateForced = false; // Флаг принудительного состояния ходьбы
    private float _belowThresholdTimer = 0f;
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
        // Автоматическое управление анимацией ходьбы с задержкой остановки и изменением скорости кадров
        if (_rigidbody != null && !_isWalkingStateForced)
        {
            float v = _rigidbody.velocity.magnitude;
            bool above = v > _movementThreshold;

            if (above)
            {
                _belowThresholdTimer = 0f;
                _animator.SetBool(IsWalking, true);
            }
            else
            {
                _belowThresholdTimer += Time.deltaTime;
                if (_belowThresholdTimer >= _stopHoldSeconds)
                {
                    _animator.SetBool(IsWalking, false);
                }
            }

            // Плавное изменение скорости анимации в зависимости от реальной скорости
            float t = Mathf.InverseLerp(_movementThreshold, _movementThreshold * 10f + 0.01f, v);
            float targetAnimSpeed = Mathf.Lerp(_animSpeedMin, _animSpeedMax, t);
            _animSpeedSmoothed = Mathf.Lerp(_animSpeedSmoothed, targetAnimSpeed, _animSpeedSmoothing * Time.deltaTime);
            _animator.speed = _animSpeedSmoothed;
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

    // Жёсткий сброс визуала после разрыва пары/нестандартных состояний
    public void ResetAfterPairBreak()
    {
        // Сбрасываем все флаги и триггеры
        _animator.ResetTrigger(Bumping);
        _animator.SetBool(IsHolding, false);
        _animator.SetBool(IsFalling, false);
        _animator.SetBool(IsWalking, true); // кратко принудим ходьбу, чтобы уйти из возможной позы
        _isWalkingStateForced = false;      // вернём автоматику
        _animator.speed = 1f;
        // Пересоберём стейт-машину, чтобы не залипала в предыдущем клипе
        _animator.Rebind();
        _animator.Update(0f);
    }
}
