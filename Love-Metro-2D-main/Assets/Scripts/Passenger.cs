using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(PassangerAnimator), typeof(Collider2D))]
public class Passenger : MonoBehaviour, IFieldEffectTarget
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
    private BeingAbsorbed beingAbsorbedState;

    private bool _isInitiated = false;

    public PassangersContainer container; // Ссылка на контейнер

    public bool IsInCouple = false;
    
    // Эффекты поля
    private Dictionary<FieldEffectType, Vector2> _activeFieldForces = new Dictionary<FieldEffectType, Vector2>();
    private List<IFieldEffect> _currentEffects = new List<IFieldEffect>();

    public void Initiate(Vector3 initialMovingDirection, TrainManager train, ScoreCounter scoreCounter)
    {
        _initialMovingDirection = initialMovingDirection;
        CurrentMovingDirection = _initialMovingDirection.normalized;

        _scoreCounter = scoreCounter;
        
        // Проверяем что компоненты получены в Awake
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (PassangerAnimator == null) PassangerAnimator = GetComponent<PassangerAnimator>();
        if (_collider == null) _collider = GetComponent<Collider2D>();

        wanderingState = new Wandering(this);
        stayingOnHandrailState = new StayingOnHandrail(this);
        fallingState = new Falling(this);
        matchingState = new Matching(this);
        beingAbsorbedState = new BeingAbsorbed(this);

        _currentState = wanderingState;
        if (_currentState != null)
        {
            _currentState.Enter();
        }

        _train = train;
        if (train != null && _currentState != null)
        {
            train.startInertia += _currentState.OnTrainSpeedChange;
        }

        _isInitiated = true;
    }

    private void Update()
    {
        if (_isInitiated == false || _currentState == null)
            return;
        _currentState.UpdateState();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_currentState == null) return;
        _currentState.OnCollision(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_currentState == null) return;
        _currentState.OnTriggerEnter(collision);
    }

    public void ForceToMatchingState(Passenger partner)
    {
        if (transform.position.x <= partner.transform.position.x)
        {
            Couple couple = Instantiate(CouplePref).GetComponent<Couple>();
            couple.init(this, partner);
            _scoreCounter.UpdateScorePointFromMatching(Camera.main.WorldToScreenPoint(couple.transform.position));
        }
        ChangeState(matchingState);
    }

    public void Transport(Vector3 position)
    {
        transform.position = position;
    }
    
    public void ForceToAbsorptionState(Vector3 absorptionCenter, float absorptionForce)
    {
        // ВАЖНО: Пассажиры в парах НЕ МОГУТ быть поглощены - они защищены!
        if (IsInCouple || _currentState is BeingAbsorbed) return;
        
        beingAbsorbedState.SetAbsorptionParameters(absorptionCenter, absorptionForce);
        ChangeState(beingAbsorbedState);
    }

    private void ChangeState(PassangerState newState)
    {
        if (_currentState != null)
        {
            _currentState.Exit();
            if (_train != null)
                _train.startInertia -= _currentState.OnTrainSpeedChange;
        }
        
        _currentState = newState;
        
        if (_currentState != null)
        {
            _currentState.Enter();
            if (_train != null)
                _train.startInertia += _currentState.OnTrainSpeedChange;
        }
    }

    private abstract class PassangerState
    {
        protected Passenger Passanger;
        public PassangerState(Passenger pasanger)
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
        public Wandering(Passenger pasanger) : base(pasanger){}

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
            // Принудительно отключаем анимацию ходьбы при выходе из состояния
            Passanger.PassangerAnimator.ForceWalkingState(false);
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
            // Включаем автоматическое управление анимацией на основе реальной скорости
            Passanger.PassangerAnimator.EnableAutomaticWalkingAnimation();
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            // Определяем, идет ли персонаж против направления торможения
            float directionAlignment = Vector2.Dot(Passanger.CurrentMovingDirection.normalized, force.normalized);
            bool movingAgainstBraking = directionAlignment < -0.3f; // персонаж идет в противоположную сторону
            
            // Базовая случайная компонента
            float baseRandomVertical = Random.Range(-0.15f, 0.15f);
            float baseRandomHorizontal = Random.Range(-0.1f, 0.1f);
            
            // Если персонаж идет против торможения - усиливаем случайность
            if (movingAgainstBraking)
            {
                baseRandomVertical *= 2f; // усиливаем вертикальную случайность
                baseRandomHorizontal *= 3f; // усиливаем горизонтальную случайность
            }
            
            // Уменьшаем влияние текущего направления движения персонажа
            Vector2 currentDirectionInfluence = Passanger.CurrentMovingDirection * 0.15f; // было влияние через скорость, теперь минимальное
            
            // Основная сила - это сила торможения (доминирующая)
            Vector2 modifiedForce = force * 0.85f + // основная сила торможения
                                   currentDirectionInfluence + // минимальное влияние текущего направления
                                   new Vector2(baseRandomHorizontal, baseRandomVertical); // случайность

            // Магнетизм: ищем ближайшего персонажа противоположного пола
            Passenger closestOpposite = null;
            float minDist = float.MaxValue;
            foreach (var other in GameObject.FindObjectsOfType<Passenger>())
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
            if (closestOpposite != null && minDist < 4f) // уменьшили радиус магнетизма для большей предсказуемости
            {
                Vector2 direction = (closestOpposite.transform.position - Passanger.transform.position).normalized;
                float magnetStrength = 0.3f; // немного увеличили силу магнетизма
                modifiedForce += direction * magnetStrength;
            }
            
            // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: автоматический переход в состояние падения
            Passanger.ChangeState(Passanger.fallingState);
            ((Falling)Passanger.fallingState).SetInitialFallingSpeed(modifiedForce);
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
        public StayingOnHandrail(Passenger pasanger) : base(pasanger) {}

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
        public Falling(Passenger pasanger) : base(pasanger)
        {
            resetFallingSpeeds();
        }

        public void SetInitialFallingSpeed(Vector2 initialSpeed)
        {
            currentFallingSpeed = initialSpeed * Passanger._fallingSpeedInitialModifier;
            previousFallingSpeed = currentFallingSpeed;
        }

        public override void OnCollision(Collision2D collision)
        {
            if (collision.transform.TryGetComponent<PlatformEffector2D>(out PlatformEffector2D platform))
            {
                return;
            }
            resetFallingSpeeds();

            Passanger.CurrentMovingDirection = Vector2.Reflect(Passanger.CurrentMovingDirection, collision.contacts[0].normal).normalized;

            if (collision.transform.TryGetComponent<Passenger>(out Passenger passenger)
                && passenger.IsFemale != Passanger.IsFemale
                && passenger.IsMatchable)
            {
                Passanger.ForceToMatchingState(passenger);
                passenger.ForceToMatchingState(Passanger);
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
            // Уменьшили замедление на 20% (0.5 -> 0.4) для более дальнего полета
            currentFallingSpeed -= currentFallingSpeed.normalized * Passanger._fallingDeceleration * Time.deltaTime * 0.4f;
            Passanger._rigidbody.velocity = (Vector3)currentFallingSpeed;
            currentFallingSpeed -= currentFallingSpeed.normalized * Passanger._fallingDeceleration * Time.deltaTime * 0.4f;

            Passanger.PassangerAnimator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(currentFallingSpeed, Vector3.right).normalized, 
                Passanger.IsFemale ? Vector3.right : Vector3.left) != 1);

            // Изменили условие выхода из полета - теперь персонажи дольше остаются в полете
            if (Vector2.Dot(previousFallingSpeed, currentFallingSpeed) <= 0 || currentFallingSpeed.magnitude <= Passanger._minFallingSpeed * 0.5f)
            {
                // Переходим в состояние блуждания вместо держания за поручни
                Passanger.ChangeState(Passanger.wanderingState);
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
            // Увеличили чувствительность к торможению в состоянии полета
            float fallingSensitivity = Passanger._fallingSpeedInitialModifier * 1.5f; // увеличили в 1.5 раза
            
            // Добавляем дополнительную силу в зависимости от текущей скорости полета
            float speedMultiplier = 1f + (currentFallingSpeed.magnitude * 0.1f); // чем быстрее летит, тем сильнее реагирует
            
            Vector2 additionalForce = force * fallingSensitivity * speedMultiplier;
            currentFallingSpeed += additionalForce;
            previousFallingSpeed = currentFallingSpeed;
        }

        private void resetFallingSpeeds()
        {
            currentFallingSpeed = Vector2.zero; previousFallingSpeed = Vector2.zero;
        }

        public override void OnTriggerEnter(Collider2D collision)
        {
            // Убрали автоматическое хватание за поручни при низких скоростях
            // Персонажи в полете больше не цепляются за поручни автоматически
        }
    }

    private class Matching : PassangerState
    {
        public Matching(Passenger pasanger) : base(pasanger){}

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

    private class BeingAbsorbed : PassangerState
    {
        private Vector3 _absorptionCenter;
        private float _absorptionForce;
        private float _timeInAbsorption = 0f;
        private float _maxAbsorptionTime = 3f; // Максимальное время поглощения
        
        public BeingAbsorbed(Passenger passenger) : base(passenger) {}
        
        public void SetAbsorptionParameters(Vector3 center, float force)
        {
            _absorptionCenter = center;
            _absorptionForce = force;
        }
        
        public override void OnCollision(Collision2D collision) 
        {
            // В состоянии поглощения игнорируем большинство столкновений
        }
        
        public override void Exit() 
        {
            Passanger.PassangerAnimator.SetFallingState(false);
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            _timeInAbsorption = 0f;
        }
        
        public override void UpdateState()
        {
            _timeInAbsorption += Time.deltaTime;
            
            Vector3 direction = (_absorptionCenter - Passanger.transform.position).normalized;
            float distance = Vector3.Distance(Passanger.transform.position, _absorptionCenter);
            
            if (distance < 0.5f || _timeInAbsorption > _maxAbsorptionTime)
            {
                // Персонаж поглощен или время истекло
                Passanger.RemoveFromContainerAndDestroy();
                return;
            }
            
            // Применяем силу поглощения
            Vector2 absorptionForce = direction * _absorptionForce;
            absorptionForce *= (1f / Mathf.Max(distance, 0.1f)); // Увеличиваем силу при приближении
            
            Passanger._rigidbody.AddForce(absorptionForce, ForceMode2D.Force);
        }
        
        public override void Enter() 
        {
            Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Passanger.PassangerAnimator.SetFallingState(true);
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._fallingLayer);
            Passanger.IsMatchable = false; // Не может больше создавать пары
            _timeInAbsorption = 0f;
        }
        
        public override void OnTrainSpeedChange(Vector2 force)
        {
            // В состоянии поглощения игнорируем инерцию поезда
        }
        
        public override void OnTriggerEnter(Collider2D collision)
        {
            // В состоянии поглощения не реагируем на поручни
        }
    }

    private void Awake()
    {
        var boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        _rigidbody = GetComponent<Rigidbody2D>();
        PassangerAnimator = GetComponent<PassangerAnimator>();
        _collider = GetComponent<Collider2D>();

        if (_rigidbody == null) gameObject.AddComponent<Rigidbody2D>();
        if (PassangerAnimator == null) gameObject.AddComponent<PassangerAnimator>();
        if (_collider == null) gameObject.AddComponent<Collider2D>();
    }
    
    private void Start()
    {
        // Регистрируем себя в системе эффектов поля
        if (FieldEffectSystem.Instance != null)
        {
            FieldEffectSystem.Instance.RegisterTarget(this);
        }
    }
    
    private void OnDestroy()
    {
        // Отменяем регистрацию в системе эффектов поля
        if (FieldEffectSystem.Instance != null)
        {
            FieldEffectSystem.Instance.UnregisterTarget(this);
        }
    }

    public void RemoveFromContainerAndDestroy()
    {
        if (container != null)
            container.RemovePassanger(this);
        Destroy(gameObject);
    }
    
    #region IFieldEffectTarget Implementation
    
    public void ApplyFieldForce(Vector2 force, FieldEffectType effectType)
    {
        if (!_isInitiated)
        {
            return;
        }

        if (_currentState is Falling)
        {
            CurrentMovingDirection = force.normalized;
            _rigidbody.AddForce(force, ForceMode2D.Force);
        }
        else if (_currentState is Wandering)
        {
            CurrentMovingDirection = force.normalized;
        }
    }
    
    public void ApplyFieldForce(Vector3 force, ForceMode2D forceMode)
    {
        if (!_isInitiated)
        {
            return;
        }

        if (_currentState is Falling)
        {
            CurrentMovingDirection = force.normalized;
            _rigidbody.AddForce(force, forceMode);
        }
        else if (_currentState is Wandering)
        {
            CurrentMovingDirection = force.normalized;
        }
    }
    
    private void CheckForBlackHoleAbsorption()
    {
        var effects = FieldEffectSystem.Instance?.GetEffectsByType(FieldEffectType.Gravity);
        if (effects == null) return;
        
        foreach (var effect in effects)
        {
            if (effect is GravityFieldEffectNew gravityEffect && gravityEffect._createBlackHoleEffect)
            {
                float distance = Vector3.Distance(transform.position, gravityEffect.transform.position);
                if (distance <= gravityEffect._eventHorizonRadius)
                {
                    var data = gravityEffect.GetEffectData();
                    ForceToAbsorptionState(gravityEffect.transform.position, data.strength);
                    break;
                }
            }
        }
    }
    
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public Rigidbody2D GetRigidbody()
    {
        return _rigidbody;
    }
    
    public bool CanBeAffectedBy(FieldEffectType effectType)
    {
        if (!_isInitiated || IsInCouple) return false;
        
        // В состоянии поглощения не подвержены другим эффектам
        if (_currentState is BeingAbsorbed) return false;
        
        // Проверяем, может ли текущее состояние быть подвержено эффекту
        switch (effectType)
        {
            case FieldEffectType.Gravity:
            case FieldEffectType.Repulsion:
            case FieldEffectType.Wind:
            case FieldEffectType.Vortex:
            case FieldEffectType.Magnetic:
                return _currentState is Wandering || _currentState is Falling;
            case FieldEffectType.Slowdown:
            case FieldEffectType.Speedup:
            case FieldEffectType.Friction:
                return _currentState is Wandering;
            default:
                return true;
        }
    }
    
    public void OnEnterFieldEffect(IFieldEffect effect)
    {
        // ВАЖНО: Пассажиры в парах не должны подвергаться воздействию эффектов поля
        if (IsInCouple) return;
        
        if (!_currentEffects.Contains(effect))
        {
            _currentEffects.Add(effect);
        }

        var effectData = effect.GetEffectData();

        // Специальная обработка гравитационных эффектов
        if (effectData.effectType == FieldEffectType.Gravity && effect is GravityFieldEffectNew gravityEffect)
        {
            float distanceToCenter = Vector3.Distance(transform.position, effectData.center);
            
            if (gravityEffect._createBlackHoleEffect && distanceToCenter <= gravityEffect._eventHorizonRadius)
            {
                ForceToAbsorptionState(effectData.center, effectData.strength);
                return;
            }
            
            // Если сила гравитации превышает порог падения и персонаж не держится за поручень
            if (effectData.strength > _handrailMinGrabbingSpeed && 
                !(_currentState is StayingOnHandrail))
            {
                ChangeState(fallingState);
            }
        }
    }
    
    public void OnExitFieldEffect(IFieldEffect effect)
    {
        _currentEffects.Remove(effect);
        
        // Убираем силу эффекта
        var effectData = effect.GetEffectData();
        if (_activeFieldForces.ContainsKey(effectData.effectType))
        {
            _activeFieldForces.Remove(effectData.effectType);
        }
    }
    
    #endregion
}

public class OnEnteringMatchingState : UnityEvent<Vector3> { }
