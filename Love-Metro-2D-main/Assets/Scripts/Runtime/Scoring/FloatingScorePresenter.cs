using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace LoveMetro.Scoring
{
    public sealed class FloatingScorePresenter : MonoBehaviour
    {
        private const float FloatingTextMaxTravelTime = 2f;
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private ScoreHudView _hudView;
        [SerializeField] private TMP_Text _floatingScorePrefab;
        [SerializeField] private float _minDisappearingDistance;
        [SerializeField] private float _acceleration;
        [SerializeField] private float _initialSpeed;
        [SerializeField] private float _spawnOffsetY;

        private Animator _counterAnimator;
        private TMP_FontAsset _font;
        private Material _materialTemplate;
        private Material _runtimeMaterial;

        public void Configure(
            ScoreHudView hudView,
            TMP_Text floatingScorePrefab,
            float minDisappearingDistance,
            float acceleration,
            float initialSpeed,
            float spawnOffsetY,
            Animator counterAnimator)
        {
            _hudView = hudView;
            _floatingScorePrefab = floatingScorePrefab;
            _minDisappearingDistance = minDisappearingDistance;
            _acceleration = acceleration;
            _initialSpeed = initialSpeed;
            _spawnOffsetY = spawnOffsetY;
            _counterAnimator = counterAnimator;
            CacheFloatingScoreStyle();
        }

        public bool TryPresent(ScorePresentationRequest request, Action completed = null)
        {
            if (_hudView == null || !_hudView.IsConfigured || _floatingScorePrefab == null)
                return false;

            StartCoroutine(Present(request, completed));
            return true;
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(_runtimeMaterial);
            else
                DestroyImmediate(_runtimeMaterial);

            _runtimeMaterial = null;
        }

        private IEnumerator Present(ScorePresentationRequest request, Action completed)
        {
            Vector2 start = _hudView.WorldToCanvasPoint(request.Change.WorldPosition, _spawnOffsetY);
            TMP_Text floatingText = CreateFloatingText(start);
            if (floatingText == null)
            {
                completed?.Invoke();
                yield break;
            }

            RectTransform floatingRect = floatingText.GetComponent<RectTransform>();
            if (floatingRect == null)
            {
                Destroy(floatingText.gameObject);
                completed?.Invoke();
                yield break;
            }

            floatingText.color = request.Color;
            floatingText.text = request.Change.Delta.ToString();
            floatingText.ForceMeshUpdate(true, true);

            Vector2 targetPosition = _hudView.GetCounterCanvasPosition();
            if (Vector2.Distance(start, targetPosition) < Mathf.Max(0.001f, _minDisappearingDistance))
            {
                Destroy(floatingText.gameObject);
                completed?.Invoke();
                yield break;
            }

            float currentSpeed = Mathf.Max(_initialSpeed, Mathf.Abs(request.Change.Delta) * 0.5f);
            float elapsed = 0f;
            while (Vector2.Distance(floatingRect.anchoredPosition, targetPosition) >= _minDisappearingDistance &&
                   elapsed < FloatingTextMaxTravelTime)
            {
                targetPosition = _hudView.GetCounterCanvasPosition();
                Vector2 direction = (targetPosition - floatingRect.anchoredPosition).normalized;
                currentSpeed += _acceleration * 0.5f * Time.deltaTime;
                floatingRect.anchoredPosition += direction * currentSpeed * Time.deltaTime;
                elapsed += Time.deltaTime;

                if (request.WaitForEndOfFrame)
                    yield return new WaitForEndOfFrame();
                else
                    yield return null;
            }

            Destroy(floatingText.gameObject);
            completed?.Invoke();
        }

        private TMP_Text CreateFloatingText(Vector2 startPosition)
        {
            Transform parent = _hudView.FloatingTextParent;
            TMP_Text floatingText = Instantiate(_floatingScorePrefab, parent, false);
            ConfigureFloatingText(floatingText);

            RectTransform floatingRect = floatingText.GetComponent<RectTransform>();
            if (floatingRect != null)
            {
                floatingRect.anchorMin = new Vector2(0.5f, 0.5f);
                floatingRect.anchorMax = new Vector2(0.5f, 0.5f);
                floatingRect.pivot = new Vector2(0.5f, 0.5f);
                floatingRect.anchoredPosition = _hudView.ClampToCanvasRect(startPosition);
                floatingRect.localScale = Vector3.one;
                floatingRect.localRotation = Quaternion.identity;
            }

            return floatingText;
        }

        private void ConfigureFloatingText(TMP_Text floatingText)
        {
            if (floatingText == null)
                return;

            ApplyScoreFont(floatingText);
            floatingText.raycastTarget = false;
            floatingText.enableVertexGradient = false;
            floatingText.enableWordWrapping = false;
            floatingText.overflowMode = TextOverflowModes.Overflow;
            floatingText.alignment = TextAlignmentOptions.Center;
            floatingText.ForceMeshUpdate();
        }

        private void CacheFloatingScoreStyle()
        {
            if (_floatingScorePrefab == null)
                return;

            _font = _floatingScorePrefab.font;
            _materialTemplate = _floatingScorePrefab.fontSharedMaterial != null
                ? _floatingScorePrefab.fontSharedMaterial
                : _font != null ? _font.material : null;
        }

        private void ApplyScoreFont(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset font = _font != null ? _font : text.font;
            if (font == null)
                return;

            text.font = font;

            Material material = GetFloatingScoreMaterial(font);
            if (material != null)
                text.fontSharedMaterial = material;
        }

        private Material GetFloatingScoreMaterial(TMP_FontAsset font)
        {
            if (_runtimeMaterial != null)
                return _runtimeMaterial;

            Material sourceMaterial = _materialTemplate != null
                ? _materialTemplate
                : font != null ? font.material : null;

            if (sourceMaterial == null)
                return null;

            _runtimeMaterial = new Material(sourceMaterial);

            if (font != null && font.atlasTexture != null && _runtimeMaterial.HasProperty(MainTexId))
                _runtimeMaterial.SetTexture(MainTexId, font.atlasTexture);

            return _runtimeMaterial;
        }

        public void TriggerCounterAnimation()
        {
            if (_counterAnimator != null && _counterAnimator.enabled)
                _counterAnimator.SetTrigger("Jump");
        }
    }
}
