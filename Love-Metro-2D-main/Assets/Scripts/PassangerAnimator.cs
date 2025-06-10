using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PassangerAnimator : MonoBehaviour
{
    // Ссылки на компоненты анимации и спрайта
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    // Кэшируем имена параметров анимации для удобства
    private const string IsWalking = "IsWalking";
    private const string IsFalling = "IsFalling";
    private const string IsHolding = "IsHolding";
    private const string Bumping = "Bumping";

    private void Awake()
    {
        // Находим компоненты при инициализации
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    /// <summary>
    /// Разворот спрайта в нужную сторону.
    /// </summary>
    public void ChangeFacingDirection(bool isFacingRight)
    {
        if (isFacingRight)
            _spriteRenderer.flipX = false;
        else
            _spriteRenderer.flipX = true;
    }

    public void SetWalkingState(bool isWalking)
    {
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

    /// <summary>
    /// Воспроизводит короткую анимацию столкновения (bump).
    /// </summary>
    public void ActivateBumping()
    {
        _animator.SetTrigger(Bumping);
    }
}
