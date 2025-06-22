using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PassangerAnimator))]
public class TestFlowMovement : MonoBehaviour
{
    [SerializeField] private float _speed;
    [SerializeField] private float _correctingForce;
    [SerializeField] private Transform _targetPositionVisual;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _walkingAnimationSpeed;

    private PassangerAnimator _animator;
    private Rigidbody2D _rigidbody;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<PassangerAnimator>();
    }
    private void FixedUpdate()
    {
        _rigidbody.AddForce((_rigidbody.velocity.sqrMagnitude * -_rigidbody.velocity.normalized) / 2 * _correctingForce);
        _rigidbody.velocity = Vector2.ClampMagnitude(_rigidbody.velocity, _maxSpeed);
        _animator.ChangeFacingDirection(Vector3.Project(_rigidbody.velocity, Vector3.right).normalized == Vector3.right);
        // Используем принудительное управление анимацией для тестового скрипта
        if(_rigidbody.velocity.magnitude > _walkingAnimationSpeed)
            _animator.ForceWalkingState(true);
        else
            _animator.ForceWalkingState(false);
    }

    public void DisablePhysics()
    {
        _rigidbody.bodyType = RigidbodyType2D.Static;
        _rigidbody.velocity = Vector2.zero;
        transform.localPosition = Vector2.zero;
    }
}
