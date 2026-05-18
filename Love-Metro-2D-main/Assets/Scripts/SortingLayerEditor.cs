using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

[RequireComponent(typeof(PassangersContainer))]
public class SortingLayerEditor : MonoBehaviour
{
    [SerializeField] private SortingLayer _wandererLayer;
    private PassangersContainer _container;
    private readonly List<SpriteRenderer> _passangerSprites = new List<SpriteRenderer>();
    private readonly List<float> _lastSortedY = new List<float>();
    private int _lastPassengerCount = 0;
    private static readonly PassangerComparer Comparer = new PassangerComparer();

    private void Start()
    {
        _container = GetComponent<PassangersContainer>();
        UpdatePassangerSprites();
    }

    private void Update()
    {
        // Обновляем список только при изменении количества пассажиров
        if (_container == null || _container.Passangers == null)
            return;

        if (_container.Passangers.Count != _lastPassengerCount)
        {
            UpdatePassangerSprites();
        }
        
        SortDepth();
    }

    public void getPassangerSprites()
    {
        UpdatePassangerSprites();
    }

    private void UpdatePassangerSprites()
    {
        _passangerSprites.Clear();
        _lastSortedY.Clear();
        if (_container != null && _container.Passangers != null)
        {
            foreach (Passenger p in _container.Passangers)
            {
                if (p != null && p.transform != null)
                {
                    SpriteRenderer sprite = p.transform.GetComponent<SpriteRenderer>();
                    if (sprite != null)
                    {
                        _passangerSprites.Add(sprite);
                    }
                }
            }

            _lastPassengerCount = _container.Passangers.Count;
        }
    }

    private void SortDepth()
    {
        if (_passangerSprites == null || _passangerSprites.Count == 0 || 
            _container == null || _container.Passangers == null)
        {
            return;
        }

        // Удаляем null объекты из списка
        bool needsSort = RemoveMissingSprites();

        if (_passangerSprites.Count == 0)
        {
            _lastSortedY.Clear();
            return;
        }

        needsSort |= HaveSpritePositionsChanged();
        if (!needsSort)
            return;

        _passangerSprites.Sort(Comparer);

        for (int i = 0; i < _passangerSprites.Count; i++)
        {
            if (_passangerSprites[i] != null)
            {
                _passangerSprites[i].sortingOrder = i;
            }
        }

        StoreSortedPositions();
    }

    private bool RemoveMissingSprites()
    {
        bool removed = false;
        for (int i = _passangerSprites.Count - 1; i >= 0; i--)
        {
            SpriteRenderer sprite = _passangerSprites[i];
            if (sprite != null && sprite)
                continue;

            _passangerSprites.RemoveAt(i);
            removed = true;
        }

        return removed;
    }

    private bool HaveSpritePositionsChanged()
    {
        if (_lastSortedY.Count != _passangerSprites.Count)
            return true;

        for (int i = 0; i < _passangerSprites.Count; i++)
        {
            SpriteRenderer sprite = _passangerSprites[i];
            if (sprite == null || !Mathf.Approximately(sprite.transform.position.y, _lastSortedY[i]))
                return true;
        }

        return false;
    }

    private void StoreSortedPositions()
    {
        _lastSortedY.Clear();
        for (int i = 0; i < _passangerSprites.Count; i++)
            _lastSortedY.Add(_passangerSprites[i] != null ? _passangerSprites[i].transform.position.y : float.NaN);
    }

    private class PassangerComparer : IComparer<SpriteRenderer>
    {
        public int Compare(SpriteRenderer x, SpriteRenderer y)
        {
            if (x == null || y == null) return 0;
            
            if(x.transform.position.y == y.transform.position.y)
                return 0;
            return (int)Mathf.Sign(y.transform.position.y - x.transform.position.y);
        }
    }
}
