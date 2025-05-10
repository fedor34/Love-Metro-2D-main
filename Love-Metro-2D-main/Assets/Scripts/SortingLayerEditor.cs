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

    private void Start()
    {
        _container = GetComponent<PassangersContainer>();
        getPassangerSprites();
    }

    private void Update()
    {
        // Обновляем список спрайтов каждый кадр, чтобы учесть возможные изменения в контейнере пассажиров
        getPassangerSprites();
        SortDepth();
    }

    public void getPassangerSprites()
    {
        _passangerSprites = new List<SpriteRenderer>();
        if (_container != null && _container.Passangers != null)
        {
            foreach (WandererNew p in _container.Passangers)
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
            return; // Предотвращаем обработку, если списки пусты или равны null
        }

        _passangerSprites.Sort(new PassangerComparer());

        // Используем длину списка спрайтов для цикла, чтобы избежать индексов вне диапазона
        for (int i = 0; i < _passangerSprites.Count; i++)
        {
            _passangerSprites[i].sortingOrder = i;
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
