using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(PassangerAnimator), typeof(Collider2D))]
public class WandererNew : MonoBehaviour
{
    private delegate void ReleaseHandrail();
    private ReleaseHandrail releaseHandrail;

    [SerializeField] private float _speed;
    [SerializeField] private Vector2 _initialMovingDirection;
    [Range(0,1)]
    [SerializeField] private float _grabingHandrailChance;
    private Vector2 CurrentMovingDirection;
    [SerializeField] private Vector2 HandrailStandingTimeInterval;
    [SerializeField] private float _fallingDeceleration;
    [SerializeField] private float _fallingSpeedInitialModifier;
    [SerializeField] private GameObject CouplePref;
    [SerializeField] private float _handrailMinGrabbingSpeed;
    [SerializeField] private float _minFallingSpeed;
    [SerializeField] private float _handrailCooldown = 0;
    [SerializeField] private float _aditionalCollisionCheckTimePeriod;
    [SerializeField] private string _fallingLayer = "Falling";
    [SerializeField] private string _defaultLayer = "Default";

    public bool IsFemale;
    [HideInInspector]public bool IsMatchable = true;
    [HideInInspector] public PassangerAnimator PassangerAnimator;

    [SerializeField] private TrainManager _train;
    
    private Vector2 _currentAcceleration;
    private float _timeWithoutHolding;

    private PassangerState _currentState;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private ScoreCounter _scoreCounter;
    
    private Wandering wanderingState;
    private StayingOnHandrail stayingOnHandrailState;
    private Falling fallingState;
    private Matching matchingState;

    private bool _isInitiated = false;

    public PassangersContainer container; // Ссылка на контейнер

    public bool IsInCouple = false;

    public void Initiate(Vector3 initialMovingDirection, TrainManager train, ScoreCounter scoreCounter)
    {
        _initialMovingDirection = initialMovingDirection;
        

        CurrentMovingDirection = _initialMovingDirection.normalized;

        _scoreCounter = scoreCounter;
        _rigidbody = GetComponent<Rigidbody2D>();
        PassangerAnimator = GetComponent<PassangerAnimator>();
        _collider = GetComponent<Collider2D>();

        wanderingState = new Wandering(this);
        stayingOnHandrailState = new StayingOnHandrail(this);
        fallingState = new Falling(this);
        matchingState = new Matching(this);

        _currentState = wanderingState;
        _currentState.Enter();

        _train = train;
        train.startInertia += _currentState.OnTrainSpeedChange;

        _isInitiated = true;
    }

    private void Update()
    {
        if (_isInitiated == false)
            return;
        _currentState.UpdateState();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        _currentState.OnCollision(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _currentState.OnTriggerEnter(collision);
    }

    public void ForceToMatchingState(WandererNew partner)
    {
        if (transform.position.x <= partner.transform.position.x)
        {
            Couple couple = Instantiate(CouplePref).GetComponent<Couple>();
            couple.init(this, partner);
            Debug.Log(couple.transform.position);
            _scoreCounter.UpdateScorePointFromMatching(Camera.main.WorldToScreenPoint(couple.transform.position));
        }
        ChangeState(matchingState);
    }

    public void Transport(Vector3 position)
    {
        transform.position = position;
    }

    private void ChangeState(PassangerState newState)
    {
        _currentState.Exit();
        _train.startInertia -= _currentState.OnTrainSpeedChange;
        
        _currentState = newState;
        
        _currentState.Enter();
        _train.startInertia += _currentState.OnTrainSpeedChange;

    }

    private abstract class PassangerState
    {
        protected WandererNew Passanger;
        public PassangerState(WandererNew pasanger)
        {
            Passanger = pasanger;
        }
        public abstract void UpdateState();
        public abstract void Exit();
        public abstract void Enter();
        public abstract void OnCollision(Collision2D collision);
        public abstract void OnTriggerEnter(Collider2D collision);
        public abstract void OnTrainSpeedChange(Vector2 force);
    }

    private class Wandering : PassangerState
    {
        public Wandering(WandererNew pasanger) : base(pasanger){}

        private float _expiredCollisionCheckTime = 0;

        public override void OnCollision(Collision2D collision)
        {
            if(collision.transform.TryGetComponent<PlatformEffector2D>(out PlatformEffector2D platform)
                && Vector3.Dot(platform.transform.up, Passanger.CurrentMovingDirection.normalized) >= 0)
            {
                return;
            }
            ReflectMovmentDirection(collision.contacts[0].normal);
            _expiredCollisionCheckTime = 0;
        }

        public override void Exit() 
        {
            Passanger._rigidbody.velocity = Vector2.zero;
            Passanger.PassangerAnimator.SetWalkingState(false);
        }
         
        public override void UpdateState()
        {
            Passanger._rigidbody.velocity = (Vector3)Passanger.CurrentMovingDirection * Passanger._speed;
            Passanger.PassangerAnimator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(Passanger.CurrentMovingDirection, Vector3.right).normalized, Vector3.right) == 1);

            Passanger._timeWithoutHolding += Time.deltaTime;
            _expiredCollisionCheckTime += Time.deltaTime;
            if(_expiredCollisionCheckTime > Passanger._aditionalCollisionCheckTimePeriod)
            {
                _expiredCollisionCheckTime = 0;
                Vector3? normal = CheckCollisions();
                if (normal != null)
                {
                    ReflectMovmentDirection((Vector3)normal);
                }
            }
        }

        public override void Enter() 
        {
            Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Passanger.PassangerAnimator.SetWalkingState(true);
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            float randomVertical = Random.Range(-0.25f, 0.25f);
            Vector2 modifiedForce = force + new Vector2(0, randomVertical);

            // Магнетизм: ищем ближайшего персонажа противоположного пола и добавляем к силе падения притягивающий вектор
            WandererNew closestOpposite = null;
            float minDist = float.MaxValue;
            foreach (var other in GameObject.FindObjectsOfType<WandererNew>())
            {
                if (other == Passanger) continue;
                if (other.IsFemale == Passanger.IsFemale) continue;
                float dist = Vector2.Distance(Passanger.transform.position, other.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestOpposite = other;
                }
            }
            if (closestOpposite != null && minDist < 5f) // радиус магнетизма можно настроить
            {
                Vector2 direction = (closestOpposite.transform.position - Passanger.transform.position).normalized;
                float magnetStrength = 0.25f; // силу можно настроить (уменьшено вдвое)
                modifiedForce += direction * magnetStrength;
            }

            Passanger.ChangeState(Passanger.fallingState);
            Passanger._currentState.OnTrainSpeedChange(modifiedForce);
        }

        public override void OnTriggerEnter(Collider2D collision)
        {
            if (collision.TryGetComponent<HandRailPosition>(out HandRailPosition Handrail)
                && Passanger._timeWithoutHolding > Passanger._handrailCooldown
                && Handrail.IsOccupied == false
                && UnityEngine.Random.Range(0f, 1f) <= Passanger._grabingHandrailChance)
            {
                Handrail.IsOccupied = true;
                Passanger.transform.position = Handrail.transform.position;
                Passanger.releaseHandrail += Handrail.ReleaseHandrail;
                Passanger.ChangeState(Passanger.stayingOnHandrailState);
            }
        }

        private Vector3? CheckCollisions()
        {
            ContactPoint2D[] contactPoints = new ContactPoint2D[20];
            int contactNumber = Passanger._rigidbody.GetContacts(contactPoints);
            if (contactNumber > 0)
            {
                return contactPoints[0].normal;
            }
            return null;
        }

        private void ReflectMovmentDirection(Vector3 normal)
        {
            Passanger.CurrentMovingDirection = Vector2.Reflect(Passanger.CurrentMovingDirection, normal).normalized;
        }
    }

    private class StayingOnHandrail : PassangerState
    {
        private float _expiredTime, _stayingTime;
        public StayingOnHandrail(WandererNew pasanger) : base(pasanger) {}

        public override void OnCollision(Collision2D collision) {}

        public override void Exit() 
        {
            Passanger.releaseHandrail?.Invoke();
            Passanger.releaseHandrail = null;
            Passanger.PassangerAnimator.SetHoldingState(false);
        }

        public override void UpdateState()
        {
            _expiredTime += Time.deltaTime;
            
            if(_expiredTime > _stayingTime)
            {
                Passanger.ChangeState(Passanger.wanderingState);
            }
        }

        public override void Enter() 
        {
            resetTimer();
            Passanger._rigidbody.bodyType = RigidbodyType2D.Static;
            Passanger.PassangerAnimator.SetHoldingState(true);
            Passanger._timeWithoutHolding = 0;
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            resetTimer();
        }

        private void resetTimer()
        {
            _expiredTime = 0;
            _stayingTime = UnityEngine.Random.Range(Passanger.HandrailStandingTimeInterval.x, Passanger.HandrailStandingTimeInterval.y);
        }

        public override void OnTriggerEnter(Collider2D collision){}
    }
    private class Falling : PassangerState
    {
        private Vector2 currentFallingSpeed;
        private Vector2 previousFallingSpeed;
        public Falling(WandererNew pasanger) : base(pasanger)
        {
            resetFallingSpeeds();
        }

        public override void OnCollision(Collision2D collision)
        {
            if (collision.transform.TryGetComponent<PlatformEffector2D>(out PlatformEffector2D platform))
            {
                return;
            }
            resetFallingSpeeds();

            Passanger.CurrentMovingDirection = Vector2.Reflect(Passanger.CurrentMovingDirection, collision.contacts[0].normal).normalized;

            if (collision.transform.TryGetComponent<WandererNew>(out WandererNew wanderer)
                && wanderer.IsFemale != Passanger.IsFemale
                && wanderer.IsMatchable)
            {
                Passanger.ForceToMatchingState(wanderer);
                wanderer.ForceToMatchingState(Passanger);
            }
        }

        public override void Exit() 
        {
            resetFallingSpeeds();
            Passanger._rigidbody.velocity = Vector2.zero;
            Passanger.PassangerAnimator.SetFallingState(false);
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer); 
        }

        public override void UpdateState()
        {
            currentFallingSpeed -= currentFallingSpeed.normalized * Passanger._fallingDeceleration * Time.deltaTime * 0.5f;
            Passanger._rigidbody.velocity = (Vector3)currentFallingSpeed;
            currentFallingSpeed -= currentFallingSpeed.normalized * Passanger._fallingDeceleration * Time.deltaTime * 0.5f;

            Passanger.PassangerAnimator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(currentFallingSpeed, Vector3.right).normalized, 
                Passanger.IsFemale ? Vector3.right : Vector3.left) != 1);

            if (Vector2.Dot(previousFallingSpeed, currentFallingSpeed) <= 0 || currentFallingSpeed.magnitude <= Passanger._minFallingSpeed)
            {
                Passanger.ChangeState(Passanger.stayingOnHandrailState);
                return;
            }

            previousFallingSpeed = currentFallingSpeed;
        }

        public override void Enter() 
        {
            Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Passanger.PassangerAnimator.SetFallingState(true);
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._fallingLayer);
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            currentFallingSpeed += force * Passanger._fallingSpeedInitialModifier;
            previousFallingSpeed = currentFallingSpeed;
        }

        private void resetFallingSpeeds()
        {
            currentFallingSpeed = Vector2.zero; previousFallingSpeed = Vector2.zero;
        }

        public override void OnTriggerEnter(Collider2D collision)
        {
            if (collision.TryGetComponent<HandRailPosition>(out HandRailPosition Handrail)
            && Handrail.IsOccupied == false
            && currentFallingSpeed.magnitude <= Passanger._handrailMinGrabbingSpeed)
            {
                Handrail.IsOccupied = true;
                Passanger.transform.position = Handrail.transform.position;
                Passanger.releaseHandrail += Handrail.ReleaseHandrail;
                Passanger.ChangeState(Passanger.stayingOnHandrailState);
            }
        }
    }

    private class Matching : PassangerState
    {
        public Matching(WandererNew pasanger) : base(pasanger){}

        public override void OnCollision(Collision2D collision){}

        public override void Exit(){}

        public override void UpdateState(){}

        public override void Enter()
        {
            Passanger.IsMatchable = false;
            Passanger._rigidbody.bodyType = RigidbodyType2D.Static;
            Passanger.PassangerAnimator.ActivateBumping();
            Passanger._collider.enabled = false;
        }

        public override void OnTrainSpeedChange(Vector2 force) {}

        public override void OnTriggerEnter(Collider2D collision){}
    }

    private void Awake()
    {
        // Увеличиваем высоту BoxCollider2D в 2 раза для более вытянутой области столкновения
        var boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Vector2 size = boxCollider.size;
            size.y *= 2.0f; // Можно изменить коэффициент для нужной высоты
            boxCollider.size = size;
        }
    }

    public void RemoveFromContainerAndDestroy()
    {
        if (container != null)
            container.RemovePassanger(this);
        Destroy(gameObject);
    }
}

public class OnEnteringMatchingState : UnityEvent<Vector3> { }
