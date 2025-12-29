using UnityEngine;

/// <summary>
/// Представляет пару пассажиров в игре, управляет их позиционированием и взаимоотношениями.
/// Оптимизирован: убраны вызовы FindObjectOfType, добавлено кеширование ScoreCounter.
/// </summary>
public class Couple : MonoBehaviour
{
    [SerializeField] private float _socialDistance;

    [Header("Разбивание пары (кик)")]
    [SerializeField] private float _breakupBase = 3f;
    [SerializeField] private float _breakupSpeedScale = 0.6f;
    [SerializeField] private float _breakupMax = 12f;
    [SerializeField] private float _breakMinSpeed = 2.0f;
    [SerializeField] private int _penaltyMin = 20;
    [SerializeField] private int _penaltyMax = 50;

    private Passenger _passengerMain;
    private Passenger _passengerOther;
    private ScoreCounter _score;

    // Статический кеш для ScoreCounter (один на сцену)
    private static ScoreCounter _cachedScoreCounter;

    /// <summary>
    /// Инициализирует пару, позиционируя пассажиров относительно друг друга
    /// </summary>
    public void init(Passenger passengerMain, Passenger passengerOther)
    {
        _passengerMain = passengerMain;
        _passengerOther = passengerOther;

        Vector3 mainPosition = passengerMain.transform.position;
        Vector3 otherPosition = passengerOther.transform.position;

        // Устанавливаем позицию пары
        transform.position = mainPosition;

        // Определяем направление для второго пассажира
        Vector3 otherDirection = mainPosition.x - otherPosition.x <= 0 ? Vector3.right : Vector3.left;

        // Перемещаем второго пассажира
        passengerOther.Transport(new Vector3(
            mainPosition.x + otherDirection.x * _socialDistance,
            mainPosition.y));

        // Устанавливаем направления взгляда
        passengerMain.PassangerAnimator.ChangeFacingDirection(true);
        passengerOther.PassangerAnimator.ChangeFacingDirection(false);

        // Делаем пассажиров дочерними объектами
        passengerMain.transform.parent = transform;
        passengerOther.transform.parent = transform;

        // Отмечаем, что оба в паре
        passengerMain.IsInCouple = true;
        passengerOther.IsInCouple = true;

        // Обновляем статус в реестре
        if (PassengerRegistry.Instance != null)
        {
            PassengerRegistry.Instance.UpdateCoupleStatus(passengerMain);
            PassengerRegistry.Instance.UpdateCoupleStatus(passengerOther);
        }

        // Регистрируем пару
        CouplesManager.Instance?.RegisterCouple(this);

        // Настраиваем триггер
        SetupTrigger();

        // Кешируем ScoreCounter
        _score = GetCachedScoreCounter();
    }

    private void SetupTrigger()
    {
        var trigger = GetComponent<Collider2D>();
        if (trigger == null)
        {
            var cc = gameObject.AddComponent<CircleCollider2D>();
            cc.radius = Mathf.Max(0.2f, _socialDistance * 0.75f);
            cc.isTrigger = true;
        }
        else
        {
            trigger.isTrigger = true;
        }
    }

    /// <summary>
    /// Возвращает закешированный ScoreCounter или ищет его один раз
    /// </summary>
    private static ScoreCounter GetCachedScoreCounter()
    {
        if (_cachedScoreCounter == null)
        {
            _cachedScoreCounter = Object.FindObjectOfType<ScoreCounter>();
        }
        return _cachedScoreCounter;
    }

    public void DespawnAtStation()
    {
        if (_passengerMain != null)
        {
            _passengerMain.RemoveFromContainerAndDestroy();
        }
        if (_passengerOther != null)
        {
            _passengerOther.RemoveFromContainerAndDestroy();
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || other == null) return;

        var passenger = other.GetComponent<Passenger>();
        if (passenger == null) return;

        // Игнорируем собственных пассажиров
        if (passenger == _passengerMain || passenger == _passengerOther) return;

        BreakByHit(passenger);
    }

    public void BreakByHit(Passenger hitter)
    {
        int penalty = _penaltyMin;
        Vector3 hitPos = transform.position;
        float speed = 0f;

        if (hitter != null)
        {
            hitPos = hitter.transform.position;
            var rb = hitter.GetRigidbody();
            if (rb != null)
            {
                speed = rb.velocity.magnitude;
                float t = Mathf.Clamp01(speed / 10f);
                penalty = Mathf.RoundToInt(Mathf.Lerp(_penaltyMin, _penaltyMax, t));
            }
        }

        // Не разбиваем при медленном касании
        if (speed < _breakMinSpeed) return;

        BreakPairInternal(penalty, hitter, hitPos);
    }

    private void BreakPairInternal(int penalty, Passenger hitter, Vector3 hitPos)
    {
        CouplesManager.Instance?.UnregisterCouple(this);

        if (_score != null)
        {
            _score.ApplyPenalty(penalty, transform.position);
        }

        Vector2 hitterVel = Vector2.zero;
        Vector3 hitterPos = hitPos;

        var hitterRb = hitter?.GetRigidbody();
        if (hitterRb != null)
        {
            hitterVel = hitterRb.velocity;
            hitterPos = hitter.transform.position;
        }

        float speed = hitterVel.magnitude;
        float mag = Mathf.Clamp(_breakupBase + speed * _breakupSpeedScale, _breakupBase, _breakupMax);

        ReleasePassenger(_passengerMain, hitterPos, mag, Vector2.left);
        ReleasePassenger(_passengerOther, hitterPos, mag, Vector2.right);

        Destroy(gameObject);
    }

    private void ReleasePassenger(Passenger passenger, Vector3 hitterPos, float magnitude, Vector2 fallbackDir)
    {
        if (passenger == null) return;

        Vector2 dir = (passenger.transform.position - hitterPos).sqrMagnitude > 0.0001f
            ? (Vector2)(passenger.transform.position - hitterPos).normalized
            : fallbackDir;

        passenger.transform.parent = null;
        passenger.BreakFromCouple(dir * magnitude);
    }

    /// <summary>
    /// Очищает статический кеш (вызывать при смене сцены)
    /// </summary>
    public static void ClearCache()
    {
        _cachedScoreCounter = null;
    }
}
