using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FullscreenSpriteFitter : MonoBehaviour
{
    [Tooltip("Камера, относительно которой подгонять фон. Если не задана – берётся Camera.main")] 
    public Camera targetCamera;
    [Tooltip("Дополнительный запас по краям в долях (0.05 = +5%)")] 
    [Range(0f, 0.5f)] public float padding = 0f;
    [Tooltip("Пересчитывать каждый кадр (на случай изменения разрешения / соотношения сторон)")] 
    public bool updateEveryFrame = true;

    private SpriteRenderer _sr;
    private Vector3 _initialLocalScale = Vector3.one;

    private void OnEnable()
    {
        _sr = GetComponent<SpriteRenderer>();
        _initialLocalScale = transform.localScale;
        FitNow();
    }

    private void LateUpdate()
    {
        // Пересчёт при изменении окна/аспекта
        if (!Application.isPlaying || updateEveryFrame)
            FitNow();
    }

    public void FitNow()
    {
        if (_sr == null || _sr.sprite == null) return;
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || !cam.orthographic) return; // для меню используем обычно ортокамеру

        // Требуемый размер мира под камеру
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;
        worldWidth *= (1f + padding);
        worldHeight *= (1f + padding);

        // Размер спрайта в ЛОКАЛЬНЫХ единицах (независимо от текущего масштаба)
        Vector2 spriteLocalSize = _sr.sprite.bounds.size;
        if (spriteLocalSize.x <= 0.0001f || spriteLocalSize.y <= 0.0001f) return;

        // Считаем целевой общий масштаб относительно исходного локального масштаба объекта
        float scaleX = worldWidth / (spriteLocalSize.x * _initialLocalScale.x);
        float scaleY = worldHeight / (spriteLocalSize.y * _initialLocalScale.y);
        float factor = Mathf.Max(scaleX, scaleY);

        // Устанавливаем масштаб стабильно (без накопления ошибок)
        transform.localScale = _initialLocalScale * factor;
    }
}
