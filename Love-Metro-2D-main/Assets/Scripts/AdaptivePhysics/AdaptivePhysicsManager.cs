using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Менеджер «адаптивной физики». При каждом запуске сцену применяет новый набор базовых физических параметров.
/// Игрок оценивает их клавишами '+' (лайк) / '-' (дизлайк).
/// На основе бинарного отклика алгоритм NES обновляет распределение μ/σ.
/// Логи и state хранятся в JSON (persistentDataPath/adaptive-phys.json)
/// </summary>
public class AdaptivePhysicsManager : MonoBehaviour
{
    [Header("Включить адаптивную физику")]
    public bool enableAdaptivePhysics = true;

    [Header("Настройка распределения")] public float initialSigma = 0.3f;
    public float learningRate = 0.15f; // α

    private Vector<float> _mu;      // центр
    private Vector<float> _sigma;   // diag std
    private Vector<float> _eps;     // последний сэмпл

    private const int PARAM_COUNT = 4; // gravity, fixedDelta, passengerSpeed, baseEffectStrength
    private string _saveFile;

    // ссылки на другие подсистемы
    [SerializeField] private EndlessMode.EndlessModeManager endless; // для baseEffectStrength
    [SerializeField] private MonoBehaviour passengerSpawner;      // любой скрипт, где есть поле speedMultiplier (опц.)

    private void Awake()
    {
        _saveFile = Path.Combine(Application.persistentDataPath, "adaptive-phys.json");
        LoadState();
        if (!enableAdaptivePhysics) return;
        SampleAndApply();
        SetupFeedbackUI();
    }

    #region Sampling / Applying
    private void SampleAndApply()
    {
        _eps = Vector<float>.RandomNormal(PARAM_COUNT);
        Vector<float> p = _mu + _sigma * _eps; // elementwise
        ApplyParameters(p);
    }

    private void ApplyParameters(Vector<float> p)
    {
        // 0: gravity (-1…1 → -20…0 m/s2)
        float g = Mathf.Lerp(-20f, 0f, (p[0] + 1f) * 0.5f);
        Physics2D.gravity = new Vector2(0, g);

        // 1: fixedDelta (0.005…0.02)
        float dt = Mathf.Lerp(0.005f, 0.02f, (p[1] + 1f) * 0.5f);
        Time.fixedDeltaTime = dt;

        // 2: passenger speed multiplier (0.5…2.0)
        float speedMul = Mathf.Lerp(0.5f, 2f, (p[2] + 1f) * 0.5f);
        if (passengerSpawner != null)
        {
            var field = passengerSpawner.GetType().GetField("speedMultiplier");
            if (field != null) field.SetValue(passengerSpawner, speedMul);
        }

        // 3: baseEffectStrength (1…15)
        float strength = Mathf.Lerp(1f, 15f, (p[3] + 1f) * 0.5f);
        if (endless != null)
        {
            var method = endless.GetType().GetMethod("SetBaseStrength");
            if (method != null) method.Invoke(endless, new object[]{ strength });
        }

        Debug.Log($"[AdaptivePhysics] Applied params g={g:F1}, dt={dt:F3}, speedMul={speedMul:F2}, strength={strength:F1}");
    }
    #endregion

    #region Feedback
    private void SetupFeedbackUI()
    {
        GameObject go = new GameObject("AdaptiveFeedbackUI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(go);

        CreateButton(canvas.transform, "+", new Vector2(-60, -60), () => RecordFeedback(+1f));
        CreateButton(canvas.transform, "-", new Vector2(+60, -60), () => RecordFeedback(-1f));
    }

    private void CreateButton(Transform parent, string text, Vector2 anchoredPos, Action onClick)
    {
        var btnObj = new GameObject(text + "Btn");
        btnObj.transform.SetParent(parent);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50, 50);
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.anchoredPosition = anchoredPos;

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);

        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform);
        var txt = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
        txt.text = text;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.fontSize = 36;
        var txtRT = txt.GetComponent<RectTransform>();
        txtRT.anchorMin = txtRT.anchorMax = new Vector2(0.5f, 0.5f);
        txtRT.anchoredPosition = Vector2.zero;
    }

    private void RecordFeedback(float r)
    {
        // логистическая регрессия / NES простой шаг
        _mu = _mu + _eps * (learningRate * r);
        _sigma = Vector<float>.Clamp(_sigma * 0.99f, 0.05f, 1.5f);
        SaveState();
        Debug.Log($"[AdaptivePhysics] Feedback {(r>0?"Like":"Dislike")} → mu now {_mu}");
    }
    #endregion

    #region Persistence
    [Serializable] private class SaveData
    {
        public float[] mu;
        public float[] sigma;
    }

    private void SaveState()
    {
        var data = new SaveData { mu = _mu.ToArray(), sigma = _sigma.ToArray() };
        File.WriteAllText(_saveFile, JsonUtility.ToJson(data));
    }

    private void LoadState()
    {
        _mu = new Vector<float>(PARAM_COUNT);
        _sigma = new Vector<float>(PARAM_COUNT);
        for (int i = 0; i < PARAM_COUNT; i++)
        {
            _mu[i] = 0f; // центр
            _sigma[i] = initialSigma;
        }
        if (File.Exists(_saveFile))
        {
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_saveFile));
                if (data.mu.Length == PARAM_COUNT)
                {
                    _mu = new Vector<float>(data.mu);
                    _sigma = new Vector<float>(data.sigma);
                }
            }
            catch {}
        }
    }
    #endregion
}

#region Simple vector helper
/// Лёгкий вектор float без зависимостей
[Serializable]
public struct Vector<T>
{
    [SerializeField] private float[] v;
    public Vector(int n) { v = new float[n]; }
    public Vector(float[] src) { v = (float[])src.Clone(); }
    public float this[int i] { get => v[i]; set => v[i] = value; }
    public static Vector<T> operator +(Vector<T> a, Vector<T> b)
    {
        var r = new Vector<T>(a.v.Length);
        for (int i = 0; i < a.v.Length; i++) r[i] = a[i] + b[i];
        return r;
    }
    public static Vector<T> operator *(Vector<T> a, Vector<T> b)
    {
        var r = new Vector<T>(a.v.Length);
        for (int i = 0; i < a.v.Length; i++) r[i] = a[i] * b[i];
        return r;
    }

    // умножение на скаляр
    public static Vector<T> operator *(Vector<T> a, float k)
    {
        var r = new Vector<T>(a.v.Length);
        for (int i = 0; i < a.v.Length; i++) r[i] = a[i] * k;
        return r;
    }
    public static Vector<T> operator *(float k, Vector<T> a) => a * k;
    public float[] ToArray() => (float[])v.Clone();
    public override string ToString() => string.Join(",", v);
    public static Vector<T> RandomNormal(int n)
    {
        var r = new Vector<T>(n);
        for (int i = 0; i < n; i++) r[i] = UnityEngine.Random.Range(-1f, 1f);
        return r;
    }
    public static Vector<T> Clamp(Vector<T> a, float min, float max)
    {
        var r = new Vector<T>(a.v.Length);
        for (int i = 0; i < a.v.Length; i++) r[i] = Mathf.Clamp(a[i], min, max);
        return r;
    }
}
#endregion 