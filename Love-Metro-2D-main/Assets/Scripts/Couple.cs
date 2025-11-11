using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Представляет пару пассажиров в игре, управляет их позиционированием и взаимоотношениями
/// </summary>
public class Couple : MonoBehaviour
{
    // Расстояние, которое должно поддерживаться между двумя пассажирами
    [SerializeField] private float _socialDistance;
    
    // Ссылки на двух пассажиров, образующих пару
    private Passenger PassangerMain;
    private Passenger PassangerOther;
    private ScoreCounter _score;
    [Header("Разбивание пары (кик)")]
    [SerializeField] private float _breakupBase = 3f;          // базовая сила
    [SerializeField] private float _breakupSpeedScale = 0.6f;   // добавка от скорости влетевшего
    [SerializeField] private float _breakupMax = 12f;           // потолок силы
    [SerializeField] private float _breakMinSpeed = 2.0f;       // минимальная скорость, чтобы действительно разбить пару
    [SerializeField] private int _penaltyMin = 20;
    [SerializeField] private int _penaltyMax = 50;

    /// <summary>
    /// Инициализирует пару, позиционируя пассажиров относительно друг друга
    /// </summary>
    /// <param name="PassangerMain">Главный пассажир пары</param>
    /// <param name="PassangerOther">Второй пассажир в паре</param>
    public void init(Passenger PassangerMain, Passenger PassangerOther)
    {
        // Присваиваем ссылки в поля, чтобы использовать их позже при разрыве
        this.PassangerMain = PassangerMain;
        this.PassangerOther = PassangerOther;
        // Получаем текущие позиции обоих пассажиров
        Vector3 mainPosition = PassangerMain.transform.position;
        Vector3 otherPosition = PassangerOther.transform.position;

        // Устанавливаем позицию пары на позицию главного пассажира
        transform.position = mainPosition;
        
        // Определяем направление, в котором должен смотреть второй пассажир, основываясь на их относительных позициях
        Vector3 OtherPlayerDirection = mainPosition.x - otherPosition.x <= 0 ? Vector3.right : Vector3.left;

        // Перемещаем второго пассажира для поддержания социальной дистанции от главного пассажира
        PassangerOther.Transport(new Vector3(
            mainPosition.x + OtherPlayerDirection.x * _socialDistance,
            mainPosition.y));

        // Устанавливаем направления взгляда обоих пассажиров
        PassangerMain.PassangerAnimator.ChangeFacingDirection(true);
        PassangerOther.PassangerAnimator.ChangeFacingDirection(false);

        // Делаем обоих пассажиров дочерними объектами этой пары
        PassangerMain.transform.parent = transform;
        PassangerOther.transform.parent = transform;

        // Отмечаем, что оба пассажира теперь в паре
        PassangerMain.IsInCouple = true;
        PassangerOther.IsInCouple = true;
        var mgr = CouplesManager.Instance;
        if (mgr != null)
        {
            mgr.RegisterCouple(this);
        }

        // Подготовим триггер для ловли влетевших пассажиров
        var trigger = GetComponent<Collider2D>();
        if (trigger == null)
        {
            var cc = gameObject.AddComponent<CircleCollider2D>();
            cc.radius = Mathf.Max(0.2f, _socialDistance * 0.75f);
            cc.isTrigger = true;
        }
        else
        {
            trigger.isTrigger = true; // гарантируем режим триггера, если коллайдер уже есть
        }

        _score = FindObjectOfType<ScoreCounter>();
    }

    public void DespawnAtStation()
    {
        if (PassangerMain != null)
        {
            PassangerMain.RemoveFromContainerAndDestroy();
        }
        if (PassangerOther != null)
        {
            PassangerOther.RemoveFromContainerAndDestroy();
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled) return;
        if (other == null) return;
        var p = other.GetComponent<Passenger>();
        if (p == null) return;
        // Игнор если это наши собственные пассажиры
        if (p == PassangerMain || p == PassangerOther) return;

        // Разбиваем пару и начисляем штраф
        BreakByHit(p);
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
                float spd = rb.velocity.magnitude;
                speed = spd;
                float t = Mathf.Clamp01(spd / 10f);
                penalty = Mathf.RoundToInt(Mathf.Lerp(_penaltyMin, _penaltyMax, t));
            }
        }
        // Не разбиваем пару при медленном касании
        if (speed < _breakMinSpeed) return;
        BreakPairInternal(penalty, hitter, hitPos);
    }

    private void BreakPairInternal(int penalty, Passenger hitter, Vector3 hitPos)
    {
        CouplesManager.Instance?.UnregisterCouple(this);
        if (_score != null)
        {
            _score.ApplyPenalty(penalty, hitPos);
        }

        // Определяем направление кика пропорционально скорости и направлению влетевшего
        Vector2 hitterVel = Vector2.zero;
        Vector3 hitterPos = hitPos;
        var hitterRb = hitter != null ? hitter.GetRigidbody() : null;
        if (hitterRb != null)
        {
            hitterVel = hitterRb.velocity;
            hitterPos = hitter.transform.position;
        }

        float speed = hitterVel.magnitude;
        float mag = Mathf.Clamp(_breakupBase + speed * _breakupSpeedScale, _breakupBase, _breakupMax);

        if (PassangerMain != null)
        {
            Vector2 dir = (PassangerMain.transform.position - hitterPos).sqrMagnitude > 0.0001f
                ? (Vector2)(PassangerMain.transform.position - hitterPos).normalized
                : Vector2.left;
            SafeRelease(PassangerMain, dir * mag);
        }
        if (PassangerOther != null)
        {
            Vector2 dir = (PassangerOther.transform.position - hitterPos).sqrMagnitude > 0.0001f
                ? (Vector2)(PassangerOther.transform.position - hitterPos).normalized
                : Vector2.right;
            SafeRelease(PassangerOther, dir * mag);
        }
        Destroy(gameObject);
    }

    private void SafeRelease(Passenger p, Vector2 kickVelocity)
    {
        if (p == null) return;
        p.transform.parent = null;
        p.BreakFromCouple(kickVelocity);
    }
}
