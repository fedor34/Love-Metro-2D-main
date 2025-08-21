using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

[RequireComponent(typeof(PassangersContainer))]
public class SortingLayerEditor : MonoBehaviour
{
    [SerializeField] private SortingLayer _wandererLayer;
    private PassangersContainer _container;
    private List<SpriteRenderer> _passangerSprites;
    private int _lastPassengerCount = 0;

    private void Start()
    {
        _container = GetComponent<PassangersContainer>();
        UpdatePassangerSprites();
    }

    private void Update()
    {
        // Обновляем список только при изменении количества пассажиров
        if (_container.Passangers.Count != _lastPassengerCount)
        {
            UpdatePassangerSprites();
            _lastPassengerCount = _container.Passangers.Count;
        }
        
        SortDepth();
    }

    public void getPassangerSprites()
    {
        UpdatePassangerSprites();
    }

    private void UpdatePassangerSprites()
    {
        _passangerSprites = new List<SpriteRenderer>();
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
        _passangerSprites.RemoveAll(sprite => sprite == null);

        if (_passangerSprites.Count == 0)
        {
            return;
        }

        _passangerSprites.Sort(new PassangerComparer());

        for (int i = 0; i < _passangerSprites.Count; i++)
        {
            if (_passangerSprites[i] != null)
            {
                _passangerSprites[i].sortingOrder = i;
            }
        }
    }

    class PassangerComparer : IComparer<SpriteRenderer>
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
