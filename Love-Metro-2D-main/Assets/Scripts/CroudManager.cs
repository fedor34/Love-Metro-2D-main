using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CroudManager : MonoBehaviour
{
    [SerializeField] private Transform _wanderingPointLayersTransforms;

    private List<List<WanderingPoint>> _wanderingPointLayers = new List<List<WanderingPoint>>();

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
    public void SetUnoccupiedPosition(Wanderer wanderer)
    {
        int selectedLayer = wanderer.WanderingPointLayer;
        WanderingPoint selectedWanderingPoint = null;

        List<int> selectableLayers = new List<int>();
        for (int i = 0; i < _wanderingPointLayers.Count; i++)
        {
            if (i == selectedLayer)
                continue;

            selectableLayers.Add(i);
        }

        selectedLayer = selectableLayers[Random.Range(0, selectableLayers.Count)];

        List<WanderingPoint> selectablePoints = new List<WanderingPoint>();
        for (int i = 0; i < _wanderingPointLayers[selectedLayer].Count; i++)
        {
            if (_wanderingPointLayers[selectedLayer][i].IsOccupied || 
                Vector2.Distance(_wanderingPointLayers[selectedLayer][i].transform.position, wanderer.transform.position) > wanderer.maxTargetDistance)
                continue;

            selectablePoints.Add(_wanderingPointLayers[selectedLayer][i]);
        }

        if(selectablePoints.Count == 0)
        {
            return;
        }

        selectedWanderingPoint = selectablePoints[Random.Range(0, selectablePoints.Count)];

        selectedWanderingPoint.IsOccupied = true;

        bool isUnderHandrail = false;
        if (selectedWanderingPoint.IsUnderHandrail)
            isUnderHandrail = true;

        wanderer.SetCurrentTargetPositionInfo(selectedWanderingPoint, isUnderHandrail, selectedLayer);
    }
}
