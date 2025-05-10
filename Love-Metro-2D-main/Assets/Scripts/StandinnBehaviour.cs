using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandinnBehaviour : MonoBehaviour
{
    [SerializeField] private AnimationCurve _randomDistribution;
    
    public void SampleDestribution()
    {
        float randomValue = UnityEngine.Random.Range(0f, 1f);
    }
}
