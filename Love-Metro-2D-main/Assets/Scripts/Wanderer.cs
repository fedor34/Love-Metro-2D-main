using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Wanderer : MonoBehaviour
{

    [SerializeField] private CroudManager croudManager;
    
    [HideInInspector] public int WanderingPointLayer { get; private set; }
    public WandererState _currentState;
    public float maxTargetDistance;

    [Range(0, 1)]
    [SerializeField] private float _holdingOnHandrailChance;
    [SerializeField] private float _speed;
    [SerializeField] private float _distanceToTarget;
    [SerializeField] private float _fallingMultiplier;
    [SerializeField] private float _falingDeceleration;
    [SerializeField] private Collider2D _passangersAwoidingCollider;
    [SerializeField] private TestFlowMovement _flowMovement;
    [SerializeField] private SpringJoint2D _springJoint;
    [SerializeField] private float _fallingThreshold;
    [SerializeField] private Vector2 _handrailStandingTimeInterval;
    [SerializeField] private Vector2 _randomFallingForceModificator;
    [SerializeField] private GameObject _loveParticles;
    [SerializeField] private float _conectionDelta;
    public bool IsFemale;

    [SerializeField] private PassangerAnimator _animator;

    private WanderingPoint _currentTargetWanderingPoint;
    private bool _isNextPointUnderHandrail;
    private Rigidbody2D _rigidbody;
    

    public void SetCurrentTargetPositionInfo(WanderingPoint wanderingPoint, bool isUnderHandrail, int targetPositionLayer)
    {
        if(_currentTargetWanderingPoint != null)
            _currentTargetWanderingPoint.IsOccupied = false;
        _currentTargetWanderingPoint = wanderingPoint;
        _isNextPointUnderHandrail = isUnderHandrail;
        WanderingPointLayer = targetPositionLayer;
    }

    private void Start()
    {
        croudManager.SetUnoccupiedPosition(this);
        _rigidbody = GetComponent<Rigidbody2D>();
        transform.GetComponent<SpriteRenderer>().color = Color.green;
    }

    private float _handrailHoldingElapsedTime = 0;
    private float _handrailHoldingTime;
    private void Update()
    {

        if (_currentState == WandererState.holdingOnHandrail)
        {
            transform.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else
        {
            transform.GetComponent<SpriteRenderer>().color = Color.green;
        }
        _currentTargetWanderingPoint.IsOccupied = true;
        if(_currentState == WandererState.STANDINGHEREIREALISE || _currentState == WandererState.falling)
        {
            return;
        }
        if(_currentState == WandererState.holdingOnHandrail)
        {
            _handrailHoldingElapsedTime += Time.deltaTime;
            if(_handrailHoldingElapsedTime > _handrailHoldingTime)
            {
                _currentState = WandererState.wandering;
                _animator.SetHoldingState(false);
                croudManager.SetUnoccupiedPosition(this);
                _handrailHoldingElapsedTime = 0;
                _currentTargetWanderingPoint.IsOccupied = false;
            }
            return;
        }
        
        transform.position += (_currentTargetWanderingPoint.transform.position - transform.position).normalized * _speed * Time.deltaTime;

        if(Vector2.Distance(transform.position, _currentTargetWanderingPoint.transform.position) > _distanceToTarget)
        {
            return;
        }
        if (_isNextPointUnderHandrail && Random.Range(0f, 1f) < _holdingOnHandrailChance)
        {
            _currentState = WandererState.holdingOnHandrail;
            _animator.SetHoldingState(true);
            _handrailHoldingTime = Random.Range(_handrailStandingTimeInterval.x, _handrailStandingTimeInterval.y);
            return;
        }
        croudManager.SetUnoccupiedPosition(this);
        _currentTargetWanderingPoint.IsOccupied = false;
    }

    public void StartFalling(Vector2 direction)
    {
        if (_currentState != WandererState.holdingOnHandrail && _currentState != WandererState.STANDINGHEREIREALISE)
            StartCoroutine(StartFallingCorutine(direction));
    }

    private IEnumerator StartFallingCorutine(Vector2 direction)
    {
        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _currentState = WandererState.falling;
        _rigidbody.AddForce(direction * _fallingMultiplier * Random.Range(_randomFallingForceModificator.x, _randomFallingForceModificator.y), 
            ForceMode2D.Impulse);
        _flowMovement.enabled = false;
        _passangersAwoidingCollider.enabled = false;
        _springJoint.enabled = false;
        _flowMovement.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        _flowMovement.transform.parent = transform;
        _animator.SetFallingState(true);
        _animator.ChangeFacingDirection((Vector3.Project(direction, Vector3.right).normalized == Vector3.right) != IsFemale);

        while (_rigidbody.velocity.magnitude > _fallingThreshold)
        {
            _rigidbody.AddForce((_rigidbody.velocity.magnitude * -_rigidbody.velocity.normalized) / 2 * _falingDeceleration);
            yield return new WaitForFixedUpdate();
        }

        _rigidbody.velocity = Vector2.zero;
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        if (_currentState != WandererState.STANDINGHEREIREALISE)
        {
            _currentState = WandererState.holdingOnHandrail;
            _flowMovement.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        }
        _handrailHoldingTime = Random.Range(_handrailStandingTimeInterval.x, _handrailStandingTimeInterval.y);
        _passangersAwoidingCollider.enabled = true;
        _flowMovement.enabled = true;
        _springJoint.enabled = true;
        _flowMovement.transform.parent = transform.parent;
        _animator.SetHoldingState(true);
        _animator.SetFallingState(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(_currentState == WandererState.falling && collision.transform.TryGetComponent<Wanderer>(out Wanderer otherWanderer))
        {
            if (otherWanderer._currentState != WandererState.STANDINGHEREIREALISE && IsFemale != otherWanderer.IsFemale)
            {
                Vector2 collisionPoint = collision.GetContact(0).point;
                Vector3 collisionDirection = Vector3.Project((Vector3)collisionPoint - transform.position, Vector3.right).normalized;
                Connect((collisionDirection == Vector3.right) == IsFemale);
                otherWanderer.Connect((-collisionDirection == Vector3.right) == !IsFemale);

                collision.transform.parent.position = collisionPoint;
                collision.transform.localPosition = Vector2.zero;
                collision.transform.parent.position -= -collisionDirection * _conectionDelta;

                transform.parent.position = collisionPoint;
                transform.localPosition = Vector2.zero;
                transform.parent.position -= collisionDirection * _conectionDelta;
            }
        }
    }

    public void Connect(bool isFacingNeeded)
    {
        if (_currentState != WandererState.STANDINGHEREIREALISE)
        {
            Instantiate(_loveParticles, transform.position, Quaternion.identity);
            _flowMovement.transform.GetComponent<SpriteRenderer>().color = Color.red;
        }
        _currentState = WandererState.STANDINGHEREIREALISE;
        _rigidbody.velocity = Vector2.zero;
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        _animator.ActivateBumping();
        _animator.SetFallingState(true);
        _animator.ChangeFacingDirection(isFacingNeeded);
        _flowMovement.DisablePhysics();
    }
}



public enum WandererState
{
    wandering,
    STANDINGHEREIREALISE,
    holdingOnHandrail,
    falling
}