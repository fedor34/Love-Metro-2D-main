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
    private WandererNew PassangerMain;
    private WandererNew PassangerOther;

    /// <summary>
    /// Инициализирует пару, позиционируя пассажиров относительно друг друга
    /// </summary>
    /// <param name="PassangerMain">Главный пассажир пары</param>
    /// <param name="PassangerOther">Второй пассажир в паре</param>
    public void init(WandererNew PassangerMain, WandererNew PassangerOther)
    {
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
    }
}
