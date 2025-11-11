using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FullscreenImageFitter : MonoBehaviour
{
    [Tooltip("Если на объекте есть Image/RawImage – подгонит без чёрных полей, как 'cover'.")]
    public bool envelopeParent = true;

    private RectTransform _rt;
    private AspectRatioFitter _arf;

    private void OnEnable()
    {
        _rt = GetComponent<RectTransform>();
        EnsureStretch();
        EnsureARF();
        FitNow();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            FitNow();
    }

    private void EnsureStretch()
    {
        _rt.anchorMin = Vector2.zero;
        _rt.anchorMax = Vector2.one;
        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }

    private void EnsureARF()
    {
        var img = GetComponent<Image>();
        var raw = GetComponent<RawImage>();
        if (img == null && raw == null)
            return; // нет графики – достаточно растянуть Rect

        _arf = GetComponent<AspectRatioFitter>();
        if (_arf == null)
            _arf = gameObject.AddComponent<AspectRatioFitter>();
        _arf.aspectMode = envelopeParent ? AspectRatioFitter.AspectMode.EnvelopeParent
                                         : AspectRatioFitter.AspectMode.FitInParent;

        // Попробуем выставить актуальное соотношение сторон, если возможно
        float ratio = 1f;
        if (img != null && img.sprite != null)
        {
            var tex = img.sprite.texture;
            if (tex != null) ratio = (float)tex.width / tex.height;
        }
        else if (raw != null && raw.texture != null)
        {
            ratio = (float)raw.texture.width / raw.texture.height;
        }
        _arf.aspectRatio = ratio;
    }

    public void FitNow()
    {
        // Ничего дополнительного – ARF и якоря уже делают «на весь экран»
        if (_arf != null)
            _arf.aspectMode = envelopeParent ? AspectRatioFitter.AspectMode.EnvelopeParent
                                             : AspectRatioFitter.AspectMode.FitInParent;
    }
}

