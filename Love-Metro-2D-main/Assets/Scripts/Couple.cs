using UnityEngine;

/// <summary>
/// Represents a passenger couple and manages breakup/despawn behavior.
/// </summary>
public class Couple : MonoBehaviour
{
    [SerializeField] private float _socialDistance;

    [Header("Breakup")]
    [SerializeField] private float _breakupBase = 3f;
    [SerializeField] private float _breakupSpeedScale = 0.6f;
    [SerializeField] private float _breakupMax = 12f;
    [SerializeField] private float _breakMinSpeed = 2.0f;
    [SerializeField] private int _penaltyMin = 20;
    [SerializeField] private int _penaltyMax = 50;

    private Passenger _passengerMain;
    private Passenger _passengerOther;
    private ScoreCounter _score;

    private static ScoreCounter _cachedScoreCounter;

    private readonly struct ImpactInfo
    {
        public ImpactInfo(Vector3 position, Vector2 velocity, int penalty)
        {
            Position = position;
            Velocity = velocity;
            Penalty = penalty;
        }

        public Vector3 Position { get; }
        public Vector2 Velocity { get; }
        public int Penalty { get; }
    }

    public void init(Passenger passengerMain, Passenger passengerOther)
    {
        Init(passengerMain, passengerOther);
    }

    public void Init(Passenger passengerMain, Passenger passengerOther)
    {
        if (passengerMain == null || passengerOther == null || passengerMain == passengerOther)
            return;

        _passengerMain = passengerMain;
        _passengerOther = passengerOther;
        transform.position = passengerMain.transform.position;

        BindPassengers();
        ConfigureTrigger();

        CouplesManager.Instance?.RegisterCouple(this);
        _score = ResolveScoreCounter();
    }

    public void DespawnAtStation()
    {
        CouplesManager.Instance?.UnregisterCouple(this);
        _passengerMain?.RemoveFromContainerAndDestroy();
        _passengerOther?.RemoveFromContainerAndDestroy();
        Destroy(gameObject);
    }

    public void BreakByHit(Passenger hitter)
    {
        ImpactInfo impact = BuildImpactInfo(hitter, transform.position);
        if (impact.Velocity.magnitude < _breakMinSpeed)
            return;

        BreakPairInternal(impact);
    }

    public static void ClearCache()
    {
        _cachedScoreCounter = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || other == null)
            return;

        Passenger passenger = other.GetComponent<Passenger>();
        if (passenger == null || passenger == _passengerMain || passenger == _passengerOther)
            return;

        BreakByHit(passenger);
    }

    private void OnDestroy()
    {
        CouplesManager.Instance?.UnregisterCouple(this);
    }

    private void BindPassengers()
    {
        Vector3 mainPosition = _passengerMain.transform.position;
        Vector3 otherPosition = _passengerOther.transform.position;
        float spacing = ResolveSpacing(mainPosition, otherPosition);
        bool mainIsLeft = mainPosition.x <= otherPosition.x;

        Vector3 otherDirection = mainIsLeft ? Vector3.right : Vector3.left;
        Vector3 alignedOtherPosition = new Vector3(
            mainPosition.x + otherDirection.x * spacing,
            mainPosition.y,
            _passengerOther.transform.position.z);

        _passengerMain.EnterCouple(transform, mainPosition, faceRight: mainIsLeft);
        _passengerOther.EnterCouple(transform, alignedOtherPosition, faceRight: !mainIsLeft);
    }

    private void ConfigureTrigger()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        float radius = Mathf.Max(0.2f, ResolveSpacing(_passengerMain.transform.position, _passengerOther.transform.position) * 0.75f);

        if (trigger == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.isTrigger = true;
            return;
        }

        trigger.isTrigger = true;
        if (trigger is CircleCollider2D existingCircle)
            existingCircle.radius = radius;
    }

    private static ScoreCounter ResolveScoreCounter()
    {
        if (_cachedScoreCounter == null || !_cachedScoreCounter)
            _cachedScoreCounter = Object.FindObjectOfType<ScoreCounter>();

        return _cachedScoreCounter;
    }

    private ImpactInfo BuildImpactInfo(Passenger hitter, Vector3 defaultPosition)
    {
        if (hitter == null)
            return new ImpactInfo(defaultPosition, Vector2.zero, _penaltyMin);

        Rigidbody2D rigidbody = hitter.GetRigidbody();
        Vector2 velocity = rigidbody != null ? rigidbody.velocity : Vector2.zero;
        float speed = velocity.magnitude;
        float penaltyLerp = Mathf.Clamp01(speed / 10f);
        int penalty = Mathf.RoundToInt(Mathf.Lerp(_penaltyMin, _penaltyMax, penaltyLerp));
        return new ImpactInfo(hitter.transform.position, velocity, penalty);
    }

    private void BreakPairInternal(ImpactInfo impact)
    {
        CouplesManager.Instance?.UnregisterCouple(this);
        _score ??= ResolveScoreCounter();
        _score?.ApplyPenalty(impact.Penalty, transform.position);

        float magnitude = Mathf.Clamp(
            _breakupBase + impact.Velocity.magnitude * _breakupSpeedScale,
            _breakupBase,
            _breakupMax);

        ReleasePassenger(_passengerMain, impact.Position, magnitude, Vector2.left);
        ReleasePassenger(_passengerOther, impact.Position, magnitude, Vector2.right);
        Destroy(gameObject);
    }

    private static void ReleasePassenger(Passenger passenger, Vector3 hitterPosition, float magnitude, Vector2 fallbackDirection)
    {
        if (passenger == null)
            return;

        Vector2 direction = (passenger.transform.position - hitterPosition).sqrMagnitude > 0.0001f
            ? (Vector2)(passenger.transform.position - hitterPosition).normalized
            : fallbackDirection;

        passenger.ExitCouple(direction * magnitude);
    }

    private float ResolveSpacing(Vector3 mainPosition, Vector3 otherPosition)
    {
        if (_socialDistance > 0f)
            return _socialDistance;

        float currentSpacing = Mathf.Abs(mainPosition.x - otherPosition.x);
        return currentSpacing > 0.01f ? currentSpacing : 0.75f;
    }
}
