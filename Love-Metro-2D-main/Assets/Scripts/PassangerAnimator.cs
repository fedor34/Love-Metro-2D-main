using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PassangerAnimator : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private const string IsWalking = "IsWalking";
    private const string IsFalling = "IsFalling";
    private const string IsHolding = "IsHolding";
    private const string Bumping = "Bumping";

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
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
