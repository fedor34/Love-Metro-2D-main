using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScoreGiving : MonoBehaviour
{
    [SerializeField] private ScoreCounter _scoreCounter;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            _scoreCounter.UpdateScorePointFromMatching(GetMouseWorldPosition());
    }

    private static Vector3 GetMouseWorldPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return Vector3.zero;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePosition);
    }
}
