using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScoreGiving : MonoBehaviour
{
    [SerializeField] private ScoreCounter _scoreCounter;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            _scoreCounter.UpdateScorePointFromMatching(Input.mousePosition);
    }
}
