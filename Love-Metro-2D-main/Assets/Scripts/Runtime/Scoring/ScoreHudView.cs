using TMPro;
using UnityEngine;

namespace LoveMetro.Scoring
{
    public sealed class ScoreHudView : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _badgeRect;
        [SerializeField] private TextMeshProUGUI _scoreText;

        private RectTransform _canvasRect;

        public bool IsConfigured => _canvas != null && _badgeRect != null && _scoreText != null;
        public Transform FloatingTextParent => CanvasRect != null ? CanvasRect.transform : transform;

        private RectTransform CanvasRect
        {
            get
            {
                if (_canvasRect == null && _canvas != null)
                    _canvasRect = _canvas.GetComponent<RectTransform>();

                return _canvasRect;
            }
        }

        public void Configure(Canvas canvas, RectTransform badgeRect, TextMeshProUGUI scoreText)
        {
            _canvas = canvas;
            _canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            _badgeRect = badgeRect;
            _scoreText = scoreText;
        }

        public void SetScore(int score)
        {
            if (_scoreText != null)
                _scoreText.text = score.ToString();
        }

        public Vector2 GetCounterCanvasPosition()
        {
            RectTransform canvasRect = CanvasRect;
            if (_badgeRect != null && canvasRect != null)
                return canvasRect.InverseTransformPoint(_badgeRect.position);

            return Vector2.zero;
        }

        public Vector2 WorldToCanvasPoint(Vector3 worldPosition, float offsetY)
        {
            RectTransform canvasRect = CanvasRect;
            if (canvasRect == null)
                return Vector2.zero;

            Camera mainCamera = Camera.main;
            Vector3 screenPosition = mainCamera != null ? mainCamera.WorldToScreenPoint(worldPosition) : worldPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out Vector2 localPoint);

            return ClampToCanvasRect(localPoint + Vector2.up * offsetY);
        }

        public Vector2 ClampToCanvasRect(Vector2 localPoint)
        {
            RectTransform canvasRect = CanvasRect;
            if (canvasRect == null)
                return localPoint;

            Rect rect = canvasRect.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return localPoint;

            const float margin = 32f;
            float minX = rect.xMin + margin;
            float maxX = rect.xMax - margin;
            float minY = rect.yMin + margin;
            float maxY = rect.yMax - margin;

            if (minX > maxX)
            {
                minX = rect.xMin;
                maxX = rect.xMax;
            }

            if (minY > maxY)
            {
                minY = rect.yMin;
                maxY = rect.yMax;
            }

            return new Vector2(
                Mathf.Clamp(localPoint.x, minX, maxX),
                Mathf.Clamp(localPoint.y, minY, maxY));
        }
    }
}
