using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple HUD arrow showing current inertia impulse direction and magnitude.
/// If arrowTransform is not assigned, the script will auto-create a Canvas + Image arrow.
/// </summary>
public class InertiaArrowHUD : MonoBehaviour
{
    [Header("Target UI element")]
    [SerializeField] private RectTransform arrowTransform;

    [Header("Appearance")]
    [SerializeField] private float maxLength = 200f; // pixels
    [SerializeField] private float thickness = 10f;  // pixels (height)
    [SerializeField] private Color color = Color.white;
    [SerializeField] private Vector2 anchoredPos = new Vector2(24, -24); // from top-center

    [Header("Behaviour")]
    [SerializeField] private float smooth = 12f;
    [SerializeField] private bool showOnlyOnBrake = false; // show only left-directed impulses
    [SerializeField] private float showThreshold = 0.05f;  // hide if below

    private Vector2 _displayed;

    private void OnEnable()
    {
        EnsureArrowExists();
    }

    private void EnsureArrowExists()
    {
        if (arrowTransform != null) return;

        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("InertiaArrowCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Create arrow Image
        GameObject arrowGo = new GameObject("InertiaArrow", typeof(RectTransform), typeof(Image));
        arrowGo.transform.SetParent(canvas.transform, false);
        arrowTransform = arrowGo.GetComponent<RectTransform>();

        // Anchor top-center
        arrowTransform.anchorMin = new Vector2(0.5f, 1f);
        arrowTransform.anchorMax = new Vector2(0.5f, 1f);
        arrowTransform.pivot = new Vector2(0f, 0.5f);
        arrowTransform.anchoredPosition = anchoredPos;
        arrowTransform.sizeDelta = new Vector2(maxLength, thickness);

        // Assign default UI sprite
        var img = arrowGo.GetComponent<Image>();
        var builtin = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (builtin == null)
        {
            // На некоторых билдах UISprite.psd отсутствует — создаём простой белый спрайт без спама ошибок
            var tex = Texture2D.whiteTexture;
            builtin = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        img.sprite = builtin;
        img.color = color;
        img.raycastTarget = false;
    }

    private void Update()
    {
        EnsureArrowExists();

        Vector2 target = Vector2.zero;
        
        // Показываем направление клика, если оно задано
        if (ClickDirectionManager.HasClickDirection)
        {
            target = ClickDirectionManager.CurrentClickDirection * 10f; // увеличиваем для видимости
        }
        
        // Или показываем последний импульс инерции
        if (target.magnitude < 0.1f)
        {
            target = TrainManager.LastInertiaImpulse;
            
            if (showOnlyOnBrake && Vector2.Dot(target, Vector2.left) <= 0f)
            {
                if (arrowTransform != null) arrowTransform.gameObject.SetActive(false);
                return;
            }
        }

        _displayed = Vector2.Lerp(_displayed, target, Mathf.Clamp01(Time.deltaTime * smooth));

        if (arrowTransform == null) return;

        float mag = _displayed.magnitude;
        if (mag <= showThreshold)
        {
            arrowTransform.gameObject.SetActive(false);
            return;
        }

        arrowTransform.gameObject.SetActive(true);

        float angle = Mathf.Atan2(_displayed.y, _displayed.x) * Mathf.Rad2Deg;
        arrowTransform.localRotation = Quaternion.Euler(0, 0, angle);

        float scale = Mathf.InverseLerp(0f, 50f, mag); // normalize magnitude
        float width = Mathf.Lerp(0f, maxLength, scale);
        var sd = arrowTransform.sizeDelta;
        sd.x = width;
        sd.y = thickness;
        arrowTransform.sizeDelta = sd;
    }
}
