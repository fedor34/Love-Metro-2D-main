using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер толпы управляет поиском свободных точек для перемещения персонажей.
/// Содержит списки слоев с точками и выдаёт следующую цель для wanderer-ов.
/// </summary>
public class CroudManager : MonoBehaviour
{
    // Родительский объект, содержащий слои точек блуждания
    [SerializeField] private Transform _wanderingPointLayersTransforms;

    // Список слоёв с точками блуждания, каждый слой это набор WanderingPoint
    private List<List<WanderingPoint>> _wanderingPointLayers = new List<List<WanderingPoint>>();

    /// <summary>
    /// Заполняем списки точек блуждания из дочерних объектов при инициализации.
    /// Каждая вершина в иерархии представляет слой с набором WanderingPoint.
    /// </summary>
    private void Awake()
    {
        for (int i = 0; i < _wanderingPointLayersTransforms.childCount; i++)
        {
            _wanderingPointLayers.Add(new List<WanderingPoint>());
            for (int j = 0; j < _wanderingPointLayersTransforms.GetChild(i).childCount; j++)
            {
                _wanderingPointLayers[i].Add(_wanderingPointLayersTransforms.GetChild(i).GetChild(j).GetComponent<WanderingPoint>());
            }
        }
    }
    /// <summary>
    /// Определяет новую свободную позицию для wanderer-а, избегая его текущего слоя.
    /// </summary>
    public void SetUnoccupiedPosition(Wanderer wanderer)
    {
        // Текущий слой персонажа, чтобы не выбирать его снова
        int selectedLayer = wanderer.WanderingPointLayer;
        WanderingPoint selectedWanderingPoint = null;

        // Собираем слои, из которых можно выбирать позицию
        List<int> selectableLayers = new List<int>();
        for (int i = 0; i < _wanderingPointLayers.Count; i++)
        {
            if (i == selectedLayer)
                continue;

            selectableLayers.Add(i);
        }

        // Выбираем случайный слой из доступных
        selectedLayer = selectableLayers[Random.Range(0, selectableLayers.Count)];

        // Ищем свободные точки в выбранном слое
        List<WanderingPoint> selectablePoints = new List<WanderingPoint>();
        for (int i = 0; i < _wanderingPointLayers[selectedLayer].Count; i++)
        {
            if (_wanderingPointLayers[selectedLayer][i].IsOccupied || 
                Vector2.Distance(_wanderingPointLayers[selectedLayer][i].transform.position, wanderer.transform.position) > wanderer.maxTargetDistance)
                continue;

            selectablePoints.Add(_wanderingPointLayers[selectedLayer][i]);
        }

        // Если подходящих точек нет, выходим
        if(selectablePoints.Count == 0)
        {
            return;
        }

        // Случайно выбираем конкретную точку
        selectedWanderingPoint = selectablePoints[Random.Range(0, selectablePoints.Count)];

        // Помечаем выбранную точку как занятую
        selectedWanderingPoint.IsOccupied = true;

        bool isUnderHandrail = false;
        if (selectedWanderingPoint.IsUnderHandrail)
            isUnderHandrail = true;

        // Передаём wanderer-у новую цель и информацию о наличии поручня
        wanderer.SetCurrentTargetPositionInfo(selectedWanderingPoint, isUnderHandrail, selectedLayer);
    }
}
