using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(PassangerAnimator), typeof(Collider2D))]
public class Passenger : MonoBehaviour, IFieldEffectTarget
{
    // Глобальный множитель скорости – можно менять из скриптов или инспектора
    public static float GlobalSpeedMultiplier = 2f;

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

    // Movement strategy selection
    public enum MovementMode
    {
        Legacy = 0,
        SteeringSmooth = 1,
        LaneBased = 2,
        BoidsLite = 3
    }
    [Header("Movement Tuning")]
    [SerializeField] private MovementMode _movementMode = MovementMode.SteeringSmooth;
    private IPassengerMovementStrategy _movementStrategy;
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private ScoreCounter _scoreCounter;
    
    private Wandering wanderingState;
    private StayingOnHandrail stayingOnHandrailState;
    private Falling fallingState;
    private Flying flyingState;
    private Matching matchingState;
    private BeingAbsorbed beingAbsorbedState;

    private bool _isInitiated = false;

    public PassangersContainer container; // Ссылка на контейнер

    public bool IsInCouple = false;
    
    // Эффекты поля
    private Dictionary<FieldEffectType, Vector2> _activeFieldForces = new Dictionary<FieldEffectType, Vector2>();
    private List<IFieldEffect> _currentEffects = new List<IFieldEffect>();

    [Header("Impulse tuning (train inertia)")]
    [SerializeField] private float _launchSensitivity = 1.0f;          // множитель силы инерции поезда
    [SerializeField] private float _minImpulseToLaunch = 3.0f;         // порог для перевода в полёт
    [SerializeField] private float _aimAssistRadius = 5.0f;            // радиус поиска цели для прицела
    [SerializeField] private float _aimAssistMaxStrength = 1.2f;       // макс. добавка к импульсу в сторону цели
    [SerializeField] private float _turbulenceStrength = 0.8f;         // сила управляемого шума (хаос)
    [SerializeField] private float _angleSnapDeg = 10f;                // привязка угла для предсказуемости
    [SerializeField] private float _impulseToVelocityScale = 0.45f;     // во сколько раз импульс конвертируется в стартовую скорость полёта
    [SerializeField] private float _maxFlightSpeed = 18f;               // верхний предел скорости полёта
    [SerializeField] private float _flightSpeedMultiplier = 0.7f;       // глобальный множитель скорости полёта (0.7 = -30%)

    [Header("Billiards-style tuning")]
    [SerializeField] private float _magnetRadius = 3.5f;               // радиус магнитного притяжения к цели
    [SerializeField] private float _magnetForce = 5.0f;                // сила магнитного притяжения
    [SerializeField] private float _repelRadius = 2.0f;                // радиус отталкивания одноимённых
    [SerializeField] private float _repelForce = 4.0f;                 // сила отталкивания одноимённых
    [SerializeField] private float _flightDeceleration = 0.65f;         // базовое замедление полёта (меньше — дальше летят)
    [SerializeField] private float _bounceElasticity = 0.95f;          // упругость при соударениях со стенами
    [SerializeField] private float _wallBounceBoost = 1.0f;            // множитель скорости при ударе о стену (1 = без ускорения)
    [SerializeField] private int _maxBounces = 3;                      // сколько отскоков допускаем до остановки
    [SerializeField] private float _easeOutMinK = 0.985f;               // минимальный коэффициент затухания (при низкой скорости)
    [SerializeField] private float _easeOutMaxK = 0.9985f;              // максимальный коэффициент (при высокой скорости)

    public void Initiate(Vector3 initialMovingDirection, TrainManager train, ScoreCounter scoreCounter)
    {
        _initialMovingDirection = initialMovingDirection;
        CurrentMovingDirection = _initialMovingDirection.normalized;

        // Применяем глобальный множитель скорости – все пассажиры будут двигаться быстрее
        _speed *= GlobalSpeedMultiplier;

        _scoreCounter = scoreCounter;
        
        // Проверяем что компоненты получены в Awake
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (PassangerAnimator == null) PassangerAnimator = GetComponent<PassangerAnimator>();
        if (_collider == null) _collider = GetComponent<Collider2D>();

        // Backward-compatible defaults for newly added serialized fields
        if (_launchSensitivity <= 0f) _launchSensitivity = 1.2f;
        if (_minImpulseToLaunch <= 0f) _minImpulseToLaunch = 0.1f;
        if (_aimAssistRadius <= 0f) _aimAssistRadius = 5.0f;
        if (_aimAssistMaxStrength <= 0f) _aimAssistMaxStrength = 1.0f;
        if (_turbulenceStrength < 0f) _turbulenceStrength = 0.6f;
        if (_angleSnapDeg <= 0f) _angleSnapDeg = 10f;
        if (_impulseToVelocityScale <= 0f) _impulseToVelocityScale = 3.2f; // ещё дальше старт
        if (_maxFlightSpeed <= 0f) _maxFlightSpeed = 56f; // потолок ещё выше
        if (_flightSpeedMultiplier <= 0f || _flightSpeedMultiplier > 2f) _flightSpeedMultiplier = 0.7f;
        // Гарантируем отсутствие накапливающегося ускорения от ударов
        _wallBounceBoost = 1f;

        // Принудительно включаем режим 4 (BoidsLite) для теста
        _movementMode = MovementMode.BoidsLite;

        // Pick movement strategy
        switch (_movementMode)
        {
            case MovementMode.SteeringSmooth:
                _movementStrategy = new SteeringSmoothStrategy();
                break;
            case MovementMode.LaneBased:
                _movementStrategy = new LaneBasedStrategy(1.0f, 3.0f);
                break;
            case MovementMode.BoidsLite:
                _movementStrategy = new BoidsLiteStrategy();
                break;
            default:
                _movementStrategy = new LegacyMovementStrategy();
                break;
        }

        wanderingState = new Wandering(this);
        stayingOnHandrailState = new StayingOnHandrail(this);
        fallingState = new Falling(this);
        flyingState = new Flying(this);
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
            Debug.Log($"[Passenger] Subscribed to train inertia. Sens={_launchSensitivity:F2}, MinImp={_minImpulseToLaunch:F2}");
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

    // Запуск пассажира с заданной начальной скоростью (переводит в состояние падения)
    public void Launch(Vector2 initialVelocity)
    {
        ChangeState(fallingState);
        ((Falling)fallingState).SetInitialFallingSpeed(initialVelocity);
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
            // Previously the side boundaries used one-way PlatformEffector2D
            // components. After switching to thicker BoxCollider2D boundaries,
            // this early-out is no longer needed and would suppress collision
            // response with the new walls.
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
            Vector2 currentVelocity = Passanger._rigidbody.velocity;
            Vector2 naturalVelocity = Passanger.CurrentMovingDirection * Passanger._speed;
            
            // Стоим на месте (без самодвижения) и ждём толчков поезда
            Vector2 desired = Vector2.zero;
            Passanger._rigidbody.velocity = desired;
            
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
            // По умолчанию стоим на месте до толчка поезда
            Passanger._rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Passanger._rigidbody.velocity = Vector2.zero;
            // Включаем автоматическое управление анимацией на основе реальной скорости
            Passanger.PassangerAnimator.EnableAutomaticWalkingAnimation();
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            float sensitivity = Passanger._launchSensitivity;
            Vector2 pos = Passanger.transform.position;
            Vector2 targetWorld = ClickDirectionManager.HasReleasePoint
                ? ClickDirectionManager.LastReleaseWorld
                : pos + ClickDirectionManager.GetCurrentDirection() * 5f;

            const float uniformScale = 1.8f;
            const float uniformGamma = 0.75f;

            float baseMag = Mathf.Max(force.magnitude, 6f) * sensitivity;
            
            float deltaXNorm = 0f;
            float deltaYNorm = 0f;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 passengerScreen = cam.WorldToScreenPoint(pos);
                Vector3 releaseScreen = cam.WorldToScreenPoint(targetWorld);
                deltaXNorm = Mathf.Clamp((releaseScreen.x - passengerScreen.x) / Mathf.Max(1f, (float)Screen.width), -1f, 1f);
                deltaYNorm = Mathf.Clamp((releaseScreen.y - passengerScreen.y) / Mathf.Max(1f, (float)Screen.height), -1f, 1f);
            }
            else
            {
                deltaXNorm = Mathf.Clamp((targetWorld.x - pos.x), -1f, 1f);
                deltaYNorm = Mathf.Clamp((targetWorld.y - pos.y), -1f, 1f);
            }

            float xWeight = Mathf.Pow(Mathf.Abs(deltaXNorm), uniformGamma);
            float yWeight = Mathf.Pow(Mathf.Abs(deltaYNorm), uniformGamma);
            
            // Усиление горизонтальной компоненты на 10%
            float xFromClick = Mathf.Sign(deltaXNorm) * baseMag * uniformScale * xWeight * 1.1f;
            float yFromClick = Mathf.Sign(deltaYNorm) * baseMag * uniformScale * yWeight;

            xFromClick += (Random.value - 0.5f) * 0.1f * Passanger._turbulenceStrength;
            yFromClick += (Random.value - 0.5f) * 0.1f * Passanger._turbulenceStrength;

            Passenger target = FindClosestOpposite(Passanger, Passanger._aimAssistRadius);
            if (target != null)
            {
                Vector2 to = (Vector2)(target.transform.position - Passanger.transform.position);
                float distNormalized = to.magnitude / Passanger._aimAssistRadius;
                if (distNormalized < 1f)
                {
                    Vector2 toNormalized = to.normalized;
                    float assistStrength = Passanger._aimAssistMaxStrength * (1f - distNormalized);
                    xFromClick += toNormalized.x * assistStrength;
                    yFromClick += toNormalized.y * assistStrength;
                }
            }

            Vector2 delta = new Vector2(xFromClick, yFromClick) * Passanger._flightSpeedMultiplier;

            if (delta.magnitude >= Passanger._minImpulseToLaunch)
            {
                Vector2 startV = delta * Passanger._impulseToVelocityScale * Passanger._flightSpeedMultiplier;
                if (startV.magnitude > Passanger._maxFlightSpeed)
                    startV = startV.normalized * Passanger._maxFlightSpeed;

            Passanger.ChangeState(Passanger.fallingState);
                ((Falling)Passanger.fallingState).SetInitialFallingSpeed(startV);
                Passanger._currentState.OnTrainSpeedChange(delta);
                Debug.Log($"[Passenger] Launch (uniform X/Y from click direction) startV={startV} x={xFromClick:F2} y={yFromClick:F2}");
            }
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
        private int _bounceCount;
        public Falling(Passenger pasanger) : base(pasanger)
        {
            resetFallingSpeeds();
        }

        public void SetInitialFallingSpeed(Vector2 initialSpeed)
        {
            // Используем скорость напрямую (без дополнительного множителя), чтобы полёт был длиннее
            currentFallingSpeed = initialSpeed * Passanger._flightSpeedMultiplier;
            previousFallingSpeed = currentFallingSpeed;
        }

        public override void OnCollision(Collision2D collision)
        {
            Vector2 n = collision.contacts[0].normal;

            if (collision.transform.TryGetComponent<Passenger>(out Passenger passenger))
            {
                // Совпадение пар — только если противоположные полы, оба матчабельны и не в паре
                if (passenger.IsFemale != Passanger.IsFemale && !passenger.IsInCouple && !Passanger.IsInCouple)
                {
                    Passanger.ForceToMatchingState(passenger);
                    passenger.ForceToMatchingState(Passanger);
                    return;
                }

                // Иначе — рикошет как от стен (с лёгким бустом)
                Vector2 vA = currentFallingSpeed;
                Vector2 vB = passenger.GetRigidbody() != null ? passenger.GetRigidbody().velocity : Vector2.zero;

                currentFallingSpeed = Vector2.Reflect(vA, n) * Passanger._bounceElasticity;
                currentFallingSpeed *= Mathf.Max(1f, Passanger._wallBounceBoost);
                if (currentFallingSpeed.magnitude > Passanger._maxFlightSpeed)
                    currentFallingSpeed = currentFallingSpeed.normalized * Passanger._maxFlightSpeed;
                Passanger._rigidbody.velocity = currentFallingSpeed;

                if (passenger.GetRigidbody() != null)
                {
                    Vector2 otherReflected = Vector2.Reflect(vB, -n) * passenger._bounceElasticity;
                    otherReflected *= Mathf.Max(1f, passenger._wallBounceBoost);
                    if (otherReflected.magnitude > passenger._maxFlightSpeed)
                        otherReflected = otherReflected.normalized * passenger._maxFlightSpeed;
                    passenger.GetRigidbody().velocity = otherReflected;
                }
            }
            else
            {
                // Рикошет о стену с небольшим ускорением (эффект боулинга)
                currentFallingSpeed = Vector2.Reflect(currentFallingSpeed, n) * Passanger._bounceElasticity;
                currentFallingSpeed *= (Passanger._wallBounceBoost * Passanger._flightSpeedMultiplier);
                if (currentFallingSpeed.magnitude > Passanger._maxFlightSpeed)
                    currentFallingSpeed = currentFallingSpeed.normalized * Passanger._maxFlightSpeed;
                Passanger._rigidbody.velocity = currentFallingSpeed;
            }

            // Учёт количества отскоков: останавливаемся после лимита
            _bounceCount++;
            if (_bounceCount >= Passanger._maxBounces)
            {
                Passanger._rigidbody.velocity = Vector2.zero;
                Passanger.ChangeState(Passanger.wanderingState);
                return;
            }
        }

        public override void Exit() 
        {
            resetFallingSpeeds();
            Passanger._rigidbody.velocity = Vector2.zero;
            Passanger.PassangerAnimator.SetFallingState(false);
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer); 
            // Восстанавливаем стабильность
            Passanger._rigidbody.drag = 0.2f;
            Passanger._rigidbody.angularDrag = 0.2f;
        }

        public override void UpdateState()
        {
            // Нелинейное (ease-out) затухание скорости: быстро стартуем, плавно замедляемся
            float speed = currentFallingSpeed.magnitude;
            float t01 = Mathf.InverseLerp(0f, Passanger._maxFlightSpeed, speed);
            float k = Mathf.Lerp(Passanger._easeOutMinK, Passanger._easeOutMaxK, t01);
            currentFallingSpeed *= Mathf.Pow(k, 60f * Time.deltaTime); // кадр-независимое затухание

            // Магнит к противоположному полу — ограничиваем, чтобы не разворачивало траекторию резко
            Passenger target = FindClosestOpposite(Passanger, Passanger._magnetRadius);
            if (target != null)
            {
                Vector2 to = (Vector2)(target.transform.position - Passanger.transform.position);
                float w = Mathf.InverseLerp(Passanger._magnetRadius, 0f, to.magnitude);
                Vector2 accel = to.normalized * (Passanger._magnetForce * w) * Time.deltaTime;
                // Проецируем магнитную «тягу» только на компоненту вдоль текущего движения, чтобы не было разворотов
                Vector2 forward = currentFallingSpeed.sqrMagnitude > 0.0001f ? currentFallingSpeed.normalized : Vector2.right;
                float along = Vector2.Dot(accel, forward);
                currentFallingSpeed += forward * along;
            }

            // Отталкивание от одноимённых
            foreach (var other in GameObject.FindObjectsOfType<Passenger>())
            {
                if (other == Passanger) continue;
                if (other.IsFemale != Passanger.IsFemale) continue;
                Vector2 toOther = (Vector2)(other.transform.position - Passanger.transform.position);
                float d = toOther.magnitude;
                if (d < 0.001f || d > Passanger._repelRadius) continue;
                float w = Mathf.InverseLerp(Passanger._repelRadius, 0f, d);
                currentFallingSpeed -= toOther.normalized * (Passanger._repelForce * w) * Time.deltaTime;
            }

            // Клэмп скорости
            if (currentFallingSpeed.magnitude > Passanger._maxFlightSpeed)
                currentFallingSpeed = currentFallingSpeed.normalized * Passanger._maxFlightSpeed;

            Passanger._rigidbody.velocity = (Vector3)currentFallingSpeed;

            // Не разворачиваем лица по ходу полёта — сохраняем направление старта (бильярд)
            //Passanger.PassangerAnimator.ChangeFacingDirection(
            //    Vector3.Dot(Vector3.Project(currentFallingSpeed, Vector3.right).normalized, 
            //    Passanger.IsFemale ? Vector3.right : Vector3.left) != 1);

            // Выход из полёта при низкой скорости
            if (currentFallingSpeed.magnitude <= Passanger._minFallingSpeed)
            {
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
            _bounceCount = 0;
            
            // Минимальное сопротивление для длинного полёта
            Passanger._rigidbody.drag = 0.02f;
            Passanger._rigidbody.angularDrag = 0.02f;
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            // Управляемая реакция: X — поезд (ослаблен), Y — указание игрока (усилен)
            float sensitivity = Mathf.Max(0.05f, Passanger._launchSensitivity);

            Vector2 pos = Passanger.transform.position;
            Vector2 targetWorld = ClickDirectionManager.HasReleasePoint
                ? ClickDirectionManager.LastReleaseWorld
                : pos + ClickDirectionManager.GetCurrentDirection() * 5f;

            const float horizontalScale = 0.4f;
            const float verticalScale   = 3.6f;
            const float verticalGamma   = 0.65f;

            float baseMag = Mathf.Max(force.magnitude, 6f) * sensitivity;
            // Усиливаем горизонтальную составляющую на 10%
            float xFromTrain = force.x * sensitivity * horizontalScale * 1.1f;

            float deltaYNorm = 0f;
            var cam = Camera.main;
            if (cam != null)
            {
                float passengerScreenY = cam.WorldToScreenPoint(pos).y;
                float releaseScreenY   = cam.WorldToScreenPoint(targetWorld).y;
                deltaYNorm = Mathf.Clamp((releaseScreenY - passengerScreenY) / Mathf.Max(1f, (float)Screen.height), -1f, 1f);
            }
            else
            {
                deltaYNorm = Mathf.Clamp((targetWorld.y - pos.y), -1f, 1f);
            }

            float yWeight = Mathf.Pow(Mathf.Abs(deltaYNorm), verticalGamma);
            float yFromClick = Mathf.Sign(deltaYNorm) * baseMag * verticalScale * yWeight;

            // Небольшой шум только по Y
            yFromClick += (Random.value - 0.5f) * 0.25f * Passanger._turbulenceStrength;

            // Aim-assist только по Y
            Passenger aimTarget = FindClosestOpposite(Passanger, Passanger._aimAssistRadius);
            if (aimTarget != null)
            {
                Vector2 toAim = (Vector2)(aimTarget.transform.position - Passanger.transform.position);
                float wy = Mathf.Clamp01(Mathf.Abs(toAim.y) / Passanger._aimAssistRadius);
                yFromClick += Mathf.Sign(toAim.y) * Mathf.Min(Passanger._aimAssistMaxStrength, toAim.magnitude * 0.2f) * wy;
            }

            Vector2 delta = new Vector2(xFromTrain, yFromClick);

            currentFallingSpeed += delta;
            if (currentFallingSpeed.magnitude > Passanger._maxFlightSpeed)
                currentFallingSpeed = currentFallingSpeed.normalized * Passanger._maxFlightSpeed;
            previousFallingSpeed = currentFallingSpeed;
        }

        private void resetFallingSpeeds()
        {
            currentFallingSpeed = Vector2.zero; previousFallingSpeed = Vector2.zero;
        }

        public override void OnTriggerEnter(Collider2D collision)
        {
            // При сильном импульсе игнорируем поручни короткое время, чтобы полёт был предсказуем
            // (реализовано отсутствием авто-хвата)
        }
    }

    private class Flying : PassangerState
    {
        private Vector2 _flyingVelocity;
        private float _windStrength = 0f;
        private float _flyingTime = 0f;
        private const float MIN_WIND_STRENGTH_FOR_FLYING = 8f;
        private const float MAX_FLYING_TIME = 5f;

        public Flying(Passenger pasanger) : base(pasanger) {}

        public override void OnCollision(Collision2D collision)
        {
            Vector2 n = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector2.up;

            // Столкновение с другим пассажиром
            if (collision.transform.TryGetComponent<Passenger>(out Passenger other))
            {
                bool canMatch = (other.IsFemale != Passanger.IsFemale) && !other.IsInCouple && !Passanger.IsInCouple;
                if (canMatch)
                {
                    Passanger.ForceToMatchingState(other);
                    other.ForceToMatchingState(Passanger);
                    return;
                }

                // Иначе — отражаемся друг от друга как от стен (боулинг)
                Vector2 vA = _flyingVelocity;
                Vector2 vB = other.GetRigidbody() != null ? other.GetRigidbody().velocity : Vector2.zero;

                Vector2 rA = Vector2.Reflect(vA, n) * Passanger._bounceElasticity;
                rA *= (Passanger._wallBounceBoost * Passanger._flightSpeedMultiplier);
                if (rA.magnitude > Passanger._maxFlightSpeed) rA = rA.normalized * Passanger._maxFlightSpeed;

                if (other.GetRigidbody() != null)
                {
                    Vector2 rB = Vector2.Reflect(vB, -n) * other._bounceElasticity;
                    rB *= (other._wallBounceBoost * other._flightSpeedMultiplier);
                    if (rB.magnitude > other._maxFlightSpeed) rB = rB.normalized * other._maxFlightSpeed;
                    other.GetRigidbody().velocity = rB;
                }

                Passanger.ChangeState(Passanger.fallingState);
                ((Falling)Passanger.fallingState).SetInitialFallingSpeed(rA);
                return;
            }

            // Столкновение со стеной — отражаемся и переходим в падение
            Vector2 r = Vector2.Reflect(_flyingVelocity, n) * Passanger._bounceElasticity;
            r *= (Passanger._wallBounceBoost * Passanger._flightSpeedMultiplier);
            if (r.magnitude > Passanger._maxFlightSpeed) r = r.normalized * Passanger._maxFlightSpeed;
            Passanger.ChangeState(Passanger.fallingState);
            ((Falling)Passanger.fallingState).SetInitialFallingSpeed(r);
        }

        public override void Exit()
        {
            Passanger.IsMatchable = true;
            Passanger.PassangerAnimator.SetFallingState(false);
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._defaultLayer);
            _flyingTime = 0f;
        }

        public override void UpdateState()
        {
            _flyingTime += Time.deltaTime;

            // Постепенно снижаем скорость, чтобы контролировать длительность полёта
            if (_flyingVelocity.sqrMagnitude > 0.0001f && Passanger._flightDeceleration > 0f)
            {
                float speed = Mathf.Max(0f, _flyingVelocity.magnitude - Passanger._flightDeceleration * Time.deltaTime);
                _flyingVelocity = _flyingVelocity.normalized * speed;
            }
            
            // Применяем движение от ветра
            Passanger._rigidbody.velocity = _flyingVelocity;
            
            // Если ветер ослаб или время полета превышено, переходим к падению
            if (_windStrength < MIN_WIND_STRENGTH_FOR_FLYING || _flyingTime > MAX_FLYING_TIME)
            {
                Passanger.ChangeState(Passanger.fallingState);
                ((Falling)Passanger.fallingState).SetInitialFallingSpeed(_flyingVelocity);
                return;
            }

            // Обновляем направление анимации
            Passanger.PassangerAnimator.ChangeFacingDirection(
                Vector3.Dot(Vector3.Project(_flyingVelocity, Vector3.right).normalized, Vector3.right) == 1);
        }

        public override void Enter()
        {
            Passanger.IsMatchable = false;
            Passanger.PassangerAnimator.SetFallingState(true); // Используем анимацию падения для полета
            Passanger.gameObject.layer = LayerMask.NameToLayer(Passanger._fallingLayer);
            _flyingTime = 0f;
        }

        public override void OnTrainSpeedChange(Vector2 force)
        {
            // В полете добавляем инерцию поезда к скорости
            _flyingVelocity += force * 0.3f; // Меньший коэффициент чем при падении
        }

        public override void OnTriggerEnter(Collider2D collision)
        {
            // В полете не цепляемся за поручни
        }

        public void SetFlyingParameters(Vector2 windVelocity, float windStrength)
        {
            _flyingVelocity = windVelocity * Passanger._flightSpeedMultiplier;
            _windStrength = windStrength;
        }

        public void UpdateWindEffect(Vector2 windVelocity, float windStrength)
        {
            _flyingVelocity = windVelocity * Passanger._flightSpeedMultiplier;
            _windStrength = windStrength;
        }
    }

    // Вспомогательно: ближайший пассажир противоположного пола
    private static Passenger FindClosestOpposite(Passenger self, float radius)
    {
        Passenger best = null;
        float bestDist = radius;
        foreach (var p in GameObject.FindObjectsOfType<Passenger>())
        {
            if (p == self) continue;
            if (p.IsFemale == self.IsFemale) continue;
            float d = Vector2.Distance(self.transform.position, p.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
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

    public string GetCurrentStateName()
    {
        return _currentState != null ? _currentState.GetType().Name : "None";
    }
    
    #region IFieldEffectTarget Implementation
    
    public void ApplyFieldForce(Vector2 force, FieldEffectType effectType)
    {
        if (!_isInitiated)
        {
            return;
        }

        // Добавляем отладочную информацию
        if (effectType == FieldEffectType.Wind && force.magnitude > 0.1f)
        {
            Debug.Log($"[Passenger] {name} получил ветер: сила={force.magnitude:F1}, состояние={_currentState?.GetType().Name}, InCouple={IsInCouple}, IsInitiated={_isInitiated}");
        }

        // Специальная обработка для ветра
        if (effectType == FieldEffectType.Wind)
        {
            float windStrength = force.magnitude;
            
            // Если ветер достаточно сильный, переводим в полет
            if (windStrength >= 8f && !(_currentState is Flying) && !(_currentState is BeingAbsorbed) && !IsInCouple)
            {
                Debug.Log($"[Passenger] {name} переходит в полет! Сила ветра: {windStrength:F1}");
                ChangeState(flyingState);
                ((Flying)flyingState).SetFlyingParameters(force, windStrength);
                return;
            }
            
            // Если уже в полете, обновляем параметры ветра
            if (_currentState is Flying)
            {
                ((Flying)flyingState).UpdateWindEffect(force, windStrength);
                return;
            }
            
            // Если ветер слабее 8, но все же есть - применяем как обычную силу
            if (_currentState is Wandering)
            {
                // Слабый ветер просто толкает пассажира
                CurrentMovingDirection = force.normalized;
                float forceMultiplier = windStrength < 8f ? 0.5f : 2f; // Сильный ветер - больше силы
                _rigidbody.AddForce(force * forceMultiplier, ForceMode2D.Force);
                Debug.Log($"[Passenger] {name} толкается ветром: сила={windStrength:F1}, множитель={forceMultiplier}");
                return;
            }
            
            // Даже если не в Wandering, но ветер очень сильный - применяем принудительно
            if (windStrength > 5f && _currentState is not Flying && _currentState is not BeingAbsorbed)
            {
                _rigidbody.AddForce(force, ForceMode2D.Force);
                Debug.Log($"[Passenger] {name} принудительно сдувается ветром: {windStrength:F1}");
                return;
            }
        }

        if (_currentState is Falling)
        {
            CurrentMovingDirection = force.normalized;
            _rigidbody.AddForce(force, ForceMode2D.Force);
        }
        else if (_currentState is Wandering)
        {
            // Применяем силу для не-ветровых эффектов
            CurrentMovingDirection = force.normalized;
            _rigidbody.AddForce(force, ForceMode2D.Force);
        }
        else if (_currentState is Flying)
        {
            // В полете обновляем скорость через состояние Flying
            ((Flying)flyingState).UpdateWindEffect(force, force.magnitude);
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
                return _currentState is Wandering || _currentState is Falling || _currentState is Flying;
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
