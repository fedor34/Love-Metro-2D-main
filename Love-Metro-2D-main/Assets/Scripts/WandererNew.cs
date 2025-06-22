using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(PassangerAnimator), typeof(Collider2D))]
public class WandererNew : MonoBehaviour, IFieldEffectTarget
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
        Debug.Log($"[WandererNew] Инициализация {name}");
        
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
        Debug.Log($"[WandererNew] {name} успешно инициализирован, состояние: {_currentState?.GetType().Name}");
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
    
    public void ForceToAbsorptionState(Vector3 absorptionCenter, float absorptionForce)
    {
        if (IsInCouple || _currentState is BeingAbsorbed) return;
        
        Debug.Log($"[WandererNew] {name} переходит в состояние поглощения");
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

    private class BeingAbsorbed : PassangerState
    {
        private Vector3 _absorptionCenter;
        private float _absorptionForce;
        private float _timeInAbsorption = 0f;
        private float _maxAbsorptionTime = 3f; // Максимальное время поглощения
        
        public BeingAbsorbed(WandererNew passenger) : base(passenger) {}
        
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
            
            // Постоянно притягиваем к центру поглощения
            Vector3 directionToCenter = (_absorptionCenter - Passanger.transform.position).normalized;
            float distance = Vector3.Distance(_absorptionCenter, Passanger.transform.position);
            
            Debug.Log($"[BeingAbsorbed] {Passanger.name} поглощается: расстояние={distance:F2}, время={_timeInAbsorption:F1}");
            
            // Очень сильная сила притяжения
            float dynamicForce = _absorptionForce * 5f; // Увеличиваем базовую силу
            
            // Дополнительно увеличиваем силу по мере приближения к центру
            if (distance < 3f)
            {
                dynamicForce *= (3f - distance) * 2f + 1f; // Экспоненциальное увеличение
            }
            
            Vector3 absorptionForce = directionToCenter * dynamicForce;
            
            // Применяем силу как импульс для более быстрого движения
            Passanger._rigidbody.AddForce(absorptionForce, ForceMode2D.Impulse);
            
            // Также устанавливаем скорость напрямую для гарантированного движения к центру
            Vector3 targetVelocity = directionToCenter * Mathf.Min(dynamicForce * 0.1f, 10f);
            Passanger._rigidbody.velocity = Vector2.Lerp(Passanger._rigidbody.velocity, targetVelocity, 0.5f);
            
            // Если персонаж близко к центру или слишком долго поглощается
            if (distance < 1f || _timeInAbsorption > _maxAbsorptionTime)
            {
                // Уничтожаем персонажа (он поглощен черной дырой)
                Debug.Log($"[WandererNew] {Passanger.name} поглощен черной дырой! Расстояние: {distance:F2}");
                Passanger.RemoveFromContainerAndDestroy();
                return;
            }
            
            // Анимация падения/поглощения
            Passanger.PassangerAnimator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(absorptionForce, Vector3.right).normalized, 
                Passanger.IsFemale ? Vector3.right : Vector3.left) != 1);
        }
        
        public override void Enter() 
        {
            Debug.Log($"[WandererNew] {Passanger.name} начинает поглощаться черной дырой");
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
        Debug.Log($"[WandererNew] Awake для {name}");
        
        // Увеличиваем высоту BoxCollider2D в 2 раза для более вытянутой области столкновения
        var boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Vector2 size = boxCollider.size;
            size.y *= 2.0f; // Можно изменить коэффициент для нужной высоты
            boxCollider.size = size;
        }
        
        // Инициализируем базовые компоненты
        _rigidbody = GetComponent<Rigidbody2D>();
        PassangerAnimator = GetComponent<PassangerAnimator>();
        _collider = GetComponent<Collider2D>();
        
        if (_rigidbody == null) Debug.LogError($"[WandererNew] {name} не имеет Rigidbody2D!");
        if (PassangerAnimator == null) Debug.LogError($"[WandererNew] {name} не имеет PassangerAnimator!");
        if (_collider == null) Debug.LogError($"[WandererNew] {name} не имеет Collider2D!");
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
            Debug.LogWarning($"[WandererNew] {name} не инициализирован, игнорирую силу поля");
            return;
        }
        
        Debug.Log($"[WandererNew] {name} получил силу поля {effectType}: {force.magnitude:F2}, состояние: {_currentState?.GetType().Name}");
        
        // Сохраняем силу эффекта
        _activeFieldForces[effectType] = force;
        
        // Применяем силу в зависимости от текущего состояния
        if (_currentState is Wandering wandering)
        {
            // В состоянии блуждания применяем как модификацию направления движения
            Vector2 modifiedDirection = (Vector2)CurrentMovingDirection + force * 0.1f;
            CurrentMovingDirection = modifiedDirection.normalized;
            Debug.Log($"[WandererNew] {name} изменил направление на: {CurrentMovingDirection}");
        }
        else if (_currentState is Falling falling)
        {
            // В состоянии падения применяем напрямую к Rigidbody
            _rigidbody.AddForce(force, ForceMode2D.Force);
            Debug.Log($"[WandererNew] {name} получил силу падения: {force}");
        }
        else
        {
            Debug.Log($"[WandererNew] {name} в состоянии {_currentState?.GetType().Name}, сила поля не применяется");
        }
    }
    
    public void ApplyFieldForce(Vector3 force, ForceMode2D forceMode)
    {
        if (!_isInitiated) 
        {
            Debug.LogWarning($"[WandererNew] {name} не инициализирован, игнорирую силу поля");
            return;
        }
        
        Debug.Log($"[WandererNew] {name} получил силу поля: {force.magnitude:F2}, режим: {forceMode}, состояние: {_currentState?.GetType().Name}");
        
        // Проверяем, не нужно ли перейти в состояние поглощения
        CheckForBlackHoleAbsorption();
        
        // Применяем силу в зависимости от текущего состояния
        if (_currentState is Wandering wandering)
        {
            // В состоянии блуждания применяем как модификацию направления движения
            Vector2 force2D = new Vector2(force.x, force.y);
            Vector2 modifiedDirection = (Vector2)CurrentMovingDirection + force2D * 0.1f;
            CurrentMovingDirection = modifiedDirection.normalized;
            Debug.Log($"[WandererNew] {name} изменил направление на: {CurrentMovingDirection}");
        }
        else if (_currentState is Falling falling)
        {
            // В состоянии падения применяем напрямую к Rigidbody
            _rigidbody.AddForce(force, forceMode);
            Debug.Log($"[WandererNew] {name} получил силу падения: {force}");
        }
        else
        {
            Debug.Log($"[WandererNew] {name} в состоянии {_currentState?.GetType().Name}, сила поля не применяется");
        }
    }
    
    private void CheckForBlackHoleAbsorption()
    {
        // Если уже поглощается, не проверяем
        if (_currentState is BeingAbsorbed) return;
        
        // Проверяем все активные эффекты на предмет черных дыр
        foreach (var effect in _currentEffects)
        {
            if (effect is GravityFieldEffectNew gravityEffect && gravityEffect._createBlackHoleEffect)
            {
                var effectData = gravityEffect.GetEffectData();
                float distance = Vector3.Distance(transform.position, effectData.center);
                
                // Если близко к черной дыре, переводим в состояние поглощения
                if (distance < gravityEffect._eventHorizonRadius * 1.5f)
                {
                    Debug.Log($"[WandererNew] {name} слишком близко к черной дыре (расстояние: {distance:F2}), переводим в поглощение");
                    ForceToAbsorptionState(effectData.center, effectData.strength);
                    return;
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
        if (_currentEffects.Contains(effect)) return;
        
        _currentEffects.Add(effect);
        
        // Логика для реакции на вход в зону эффекта
        var effectData = effect.GetEffectData();
        
        Debug.Log($"[WandererNew] {name} вошел в эффект {effectData.effectType}, сила: {effectData.strength}");
        
        // Проверяем, является ли это эффектом черной дыры
        if (effectData.effectType == FieldEffectType.Gravity && effect is GravityFieldEffectNew gravityEffect)
        {
            float distanceToCenter = Vector3.Distance(transform.position, effectData.center);
            Debug.Log($"[WandererNew] {name} проверяет черную дыру: расстояние={distanceToCenter:F2}, горизонт={gravityEffect._eventHorizonRadius}, черная дыра={gravityEffect._createBlackHoleEffect}");
            
            // Если это черная дыра - сразу переводим в состояние поглощения при любом расстоянии в зоне эффекта
            if (gravityEffect._createBlackHoleEffect)
            {
                Debug.Log($"[WandererNew] {name} попал в черную дыру! Переводим в состояние поглощения");
                ForceToAbsorptionState(effectData.center, effectData.strength);
                return;
            }
            
            // Если это сильная гравитация и пассажир блуждает, переводим в падение
            if (_currentState is Wandering && effectData.strength > 3f)
            {
                Debug.Log($"[WandererNew] {name} попал в сильную гравитацию, переводим в падение");
                Vector2 directionToCenter = (effectData.center - transform.position).normalized;
                Vector2 initialFallingForce = directionToCenter * effectData.strength * 0.5f;
                
                // Переводим в состояние падения с начальной силой
                ChangeState(fallingState);
                _currentState.OnTrainSpeedChange(initialFallingForce);
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
