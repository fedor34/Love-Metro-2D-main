#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Меню редактора для создания эффектов поля
/// </summary>
public static class FieldEffectMenus
{
    private const string MENU_ROOT = "GameObject/Field Effects/";
    private const int BASE_PRIORITY = 10;
    
    #region System Management
    
    [MenuItem(MENU_ROOT + "Create Field Effect System", false, BASE_PRIORITY - 1)]
    public static void CreateFieldEffectSystem()
    {
        if (FieldEffectSystem.Instance != null)
        {
            Debug.Log("Field Effect System уже существует");
            Selection.activeGameObject = FieldEffectSystem.Instance.gameObject;
            return;
        }
        
        GameObject systemObj = new GameObject("[FieldEffectSystem]");
        systemObj.AddComponent<FieldEffectSystem>();
        
        Undo.RegisterCreatedObjectUndo(systemObj, "Create Field Effect System");
        Selection.activeGameObject = systemObj;
        
        Debug.Log("Field Effect System создан");
    }
    
    #endregion
    
    #region Movement Effects
    
    [MenuItem(MENU_ROOT + "Movement/Gravity Field Effect", false, BASE_PRIORITY)]
    public static void CreateGravityEffect()
    {
        var effect = CreateEffectAtSceneViewCenter<GravityFieldEffectNew>("Gravity Effect");
        Debug.Log("Создан эффект гравитации");
    }
    
    [MenuItem(MENU_ROOT + "Movement/Gravity Field Effect (Realistic)", false, BASE_PRIORITY)]
    public static void CreateRealisticGravityEffect()
    {
        var effect = CreateEffectAtSceneViewCenter<GravityFieldEffectNew>("Realistic Gravity Effect");
        effect.SetRealisticGravity(true, 9.8f, 100f);
        Debug.Log("Создан реалистичный эффект гравитации");
    }
    
    [MenuItem(MENU_ROOT + "Movement/Gravity Field Effect (Black Hole)", false, BASE_PRIORITY)]
    public static void CreateBlackHoleEffect()
    {
        var effect = CreateEffectAtSceneViewCenter<GravityFieldEffectNew>("Black Hole Effect");
        effect.SetStrength(10f);
        effect.SetRadius(15f);
        effect.SetRealisticGravity(true, 15f, 200f);
        effect.SetBlackHoleEffect(true, 2f);
        Debug.Log("Создан эффект черной дыры");
    }
    
    [MenuItem(MENU_ROOT + "Movement/Wind Field Effect", false, BASE_PRIORITY)]
    public static void CreateWindEffect()
    {
        var effect = CreateEffectAtSceneViewCenter<WindFieldEffect>("Wind Effect");
        effect.SetWindDirection(Vector2.right);
        Debug.Log("Создан эффект ветра");
    }
    
    [MenuItem(MENU_ROOT + "Movement/Wind Field Effect (Turbulent)", false, BASE_PRIORITY)]
    public static void CreateTurbulentWindEffect()
    {
        var effect = CreateEffectAtSceneViewCenter<WindFieldEffect>("Turbulent Wind Effect");
        effect.SetWindDirection(Vector2.right);
        effect.SetTurbulence(true, 0.7f, 2f);
        Debug.Log("Создан турбулентный эффект ветра");
    }
    
    [MenuItem(MENU_ROOT + "Movement/Vortex Field Effect", false, BASE_PRIORITY)]
    public static void CreateVortexEffect()
    {
        var effect = CreateEffectAtSceneViewCenter<VortexFieldEffect>("Vortex Effect");
        Debug.Log("Создан эффект вихря");
    }
    
    [MenuItem(MENU_ROOT + "Movement/Vortex Field Effect (Hurricane)", false, BASE_PRIORITY)]
    public static void CreateHurricaneEffect()
    {
        var effect = CreateEffectAtSceneViewCenter<VortexFieldEffect>("Hurricane Effect");
        effect.SetStrength(8f);
        effect.SetRadius(20f);
        effect.SetVortexParameters(3f, 2f);
        effect.SetEyeOfStorm(true, 3f);
        Debug.Log("Создан эффект урагана");
    }
    
    #endregion
    
    #region Preset Compositions
    
    [MenuItem(MENU_ROOT + "Compositions/Gravity Well", false, BASE_PRIORITY + 20)]
    public static void CreateGravityWell()
    {
        Vector3 center = GetSceneViewCenter();
        
        // Центральная сильная гравитация
        var centralGravity = FieldEffectFactory.CreateEffect<GravityFieldEffectNew>(center);
        centralGravity.name = "Gravity Well - Center";
        centralGravity.SetStrength(8f);
        centralGravity.SetRadius(5f);
        centralGravity.SetPriority(10);
        
        // Окружающий слабый вихрь
        var vortex = FieldEffectFactory.CreateEffect<VortexFieldEffect>(center);
        vortex.name = "Gravity Well - Vortex";
        vortex.SetStrength(3f);
        vortex.SetRadius(12f);
        vortex.SetVortexParameters(1f, 0.5f);
        vortex.SetPriority(5);
        
        // Группируем эффекты
        GameObject parent = new GameObject("Gravity Well Composition");
        parent.transform.position = center;
        centralGravity.transform.SetParent(parent.transform);
        vortex.transform.SetParent(parent.transform);
        
        Undo.RegisterCreatedObjectUndo(parent, "Create Gravity Well");
        Selection.activeGameObject = parent;
        
        Debug.Log("Создана композиция 'Гравитационный колодец'");
    }
    
    [MenuItem(MENU_ROOT + "Compositions/Wind Tunnel", false, BASE_PRIORITY + 20)]
    public static void CreateWindTunnel()
    {
        Vector3 center = GetSceneViewCenter();
        
        // Основной поток ветра
        var mainWind = FieldEffectFactory.CreateEffect<WindFieldEffect>(center);
        mainWind.name = "Wind Tunnel - Main Flow";
        mainWind.SetStrength(6f);
        mainWind.SetRadius(8f);
        mainWind.SetWindDirection(Vector2.right);
        mainWind.SetPriority(10);
        
        // Боковые турбулентные потоки
        var sideWind1 = FieldEffectFactory.CreateEffect<WindFieldEffect>(center + Vector3.up * 4f);
        sideWind1.name = "Wind Tunnel - Side Flow 1";
        sideWind1.SetStrength(3f);
        sideWind1.SetRadius(6f);
        sideWind1.SetWindDirection(Vector2.down + Vector2.right);
        sideWind1.SetTurbulence(true, 0.5f, 3f);
        sideWind1.SetPriority(5);
        
        var sideWind2 = FieldEffectFactory.CreateEffect<WindFieldEffect>(center + Vector3.down * 4f);
        sideWind2.name = "Wind Tunnel - Side Flow 2";
        sideWind2.SetStrength(3f);
        sideWind2.SetRadius(6f);
        sideWind2.SetWindDirection(Vector2.up + Vector2.right);
        sideWind2.SetTurbulence(true, 0.5f, 3f);
        sideWind2.SetPriority(5);
        
        // Группируем эффекты
        GameObject parent = new GameObject("Wind Tunnel Composition");
        parent.transform.position = center;
        mainWind.transform.SetParent(parent.transform);
        sideWind1.transform.SetParent(parent.transform);
        sideWind2.transform.SetParent(parent.transform);
        
        Undo.RegisterCreatedObjectUndo(parent, "Create Wind Tunnel");
        Selection.activeGameObject = parent;
        
        Debug.Log("Создана композиция 'Аэродинамическая труба'");
    }
    
    [MenuItem(MENU_ROOT + "Compositions/Chaotic Zone", false, BASE_PRIORITY + 20)]
    public static void CreateChaoticZone()
    {
        Vector3 center = GetSceneViewCenter();
        
        // Случайные эффекты в разных точках
        var effects = new BaseFieldEffect[5];
        var positions = new Vector3[]
        {
            center,
            center + Vector3.right * 3f,
            center + Vector3.left * 3f,
            center + Vector3.up * 3f,
            center + Vector3.down * 3f
        };
        
        for (int i = 0; i < effects.Length; i++)
        {
            var randomType = (FieldEffectType)Random.Range(0, 3); // Gravity, Wind, Vortex
            
            switch (randomType)
            {
                case FieldEffectType.Gravity:
                    effects[i] = FieldEffectFactory.CreateEffect<GravityFieldEffectNew>(positions[i]);
                    break;
                case FieldEffectType.Wind:
                    var wind = FieldEffectFactory.CreateEffect<WindFieldEffect>(positions[i]);
                    wind.SetWindDirection(Random.insideUnitCircle.normalized);
                    effects[i] = wind;
                    break;
                case FieldEffectType.Vortex:
                    var vortex = FieldEffectFactory.CreateEffect<VortexFieldEffect>(positions[i]);
                    vortex.SetClockwise(Random.value > 0.5f);
                    effects[i] = vortex;
                    break;
            }
            
            effects[i].name = $"Chaotic Zone - Effect {i + 1}";
            effects[i].SetStrength(Random.Range(2f, 6f));
            effects[i].SetRadius(Random.Range(4f, 8f));
            effects[i].SetPriority(Random.Range(1, 10));
        }
        
        // Группируем эффекты
        GameObject parent = new GameObject("Chaotic Zone Composition");
        parent.transform.position = center;
        
        foreach (var effect in effects)
        {
            effect.transform.SetParent(parent.transform);
        }
        
        Undo.RegisterCreatedObjectUndo(parent, "Create Chaotic Zone");
        Selection.activeGameObject = parent;
        
        Debug.Log("Создана композиция 'Хаотичная зона'");
    }
    
    #endregion
    
    #region Utilities
    
    [MenuItem(MENU_ROOT + "Utilities/Create Demo Scene", false, BASE_PRIORITY + 30)]
    public static void CreateDemoScene()
    {
        Vector3 center = GetSceneViewCenter();
        
        // Создаем демонстрационный объект
        GameObject demo = new GameObject("Field Effects Demo");
        demo.transform.position = center;
        demo.AddComponent<SystemStatusTest>();
        
        Undo.RegisterCreatedObjectUndo(demo, "Create Demo Scene");
        Selection.activeGameObject = demo;
        
        Debug.Log("Создана демонстрационная сцена эффектов поля");
    }
    
    [MenuItem(MENU_ROOT + "Utilities/Clear All Effects", false, BASE_PRIORITY + 30)]
    public static void ClearAllEffects()
    {
        var allEffects = Object.FindObjectsOfType<BaseFieldEffect>();
        
        if (allEffects.Length == 0)
        {
            Debug.Log("Эффекты для удаления не найдены");
            return;
        }
        
        bool confirmed = EditorUtility.DisplayDialog(
            "Удалить все эффекты?",
            $"Найдено {allEffects.Length} эффектов поля. Удалить их все?",
            "Да", "Отмена");
        
        if (confirmed)
        {
            foreach (var effect in allEffects)
            {
                Undo.DestroyObjectImmediate(effect.gameObject);
            }
            
            Debug.Log($"Удалено {allEffects.Length} эффектов поля");
        }
    }
    
    [MenuItem(MENU_ROOT + "Utilities/System Diagnostics", false, BASE_PRIORITY + 30)]
    public static void ShowSystemDiagnostics()
    {
        var diagnostics = new System.Text.StringBuilder();
        diagnostics.AppendLine("=== ДИАГНОСТИКА СИСТЕМЫ ЭФФЕКТОВ ПОЛЯ ===");
        
        // Проверка системы
        if (FieldEffectSystem.Instance == null)
        {
            diagnostics.AppendLine("❌ FieldEffectSystem не инициализирована");
        }
        else
        {
            diagnostics.AppendLine("✅ FieldEffectSystem инициализирована");
            diagnostics.AppendLine($"   Всего эффектов: {FieldEffectSystem.Instance.GetTotalEffectsCount()}");
            
            // Проверка категорий
            foreach (FieldEffectCategory category in System.Enum.GetValues(typeof(FieldEffectCategory)))
            {
                var effects = FieldEffectSystem.Instance.GetEffectsByCategory(category);
                if (effects.Count > 0)
                {
                    diagnostics.AppendLine($"   {category}: {effects.Count}");
                }
            }
        }
        
        // Проверка эффектов на сцене
        var sceneEffects = Object.FindObjectsOfType<BaseFieldEffect>();
        diagnostics.AppendLine($"\nЭффекты на сцене: {sceneEffects.Length}");
        
        foreach (var effect in sceneEffects)
        {
            diagnostics.AppendLine($"   - {effect.name} ({effect.GetType().Name})");
            diagnostics.AppendLine($"     Активен: {effect.IsActive}, Тип: {effect.GetEffectData().effectType}");
        }
        
        // Проверка целей
        var targets = Object.FindObjectsOfType<MonoBehaviour>().Where(mb => mb is IFieldEffectTarget).ToArray();
        diagnostics.AppendLine($"\nЦели на сцене: {targets.Length}");
        
        foreach (var target in targets.Take(5))
        {
            diagnostics.AppendLine($"   - {target.name} ({target.GetType().Name})");
        }
        
        if (targets.Length > 5)
        {
            diagnostics.AppendLine($"   ... и еще {targets.Length - 5}");
        }
        
        Debug.Log(diagnostics.ToString());
        
        // Создаем окно с подробной информацией
        var window = EditorWindow.GetWindow<FieldEffectDiagnosticsWindow>("Field Effects Diagnostics");
        window.SetDiagnosticsText(diagnostics.ToString());
        window.Show();
    }
    
    #endregion
    
    #region Helper Methods
    
    private static T CreateEffectAtSceneViewCenter<T>(string name) where T : BaseFieldEffect
    {
        Vector3 position = GetSceneViewCenter();
        var effect = FieldEffectFactory.CreateEffect<T>(position);
        effect.name = name;
        
        Undo.RegisterCreatedObjectUndo(effect.gameObject, $"Create {name}");
        Selection.activeGameObject = effect.gameObject;
        
        return effect;
    }
    
    private static Vector3 GetSceneViewCenter()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            return SceneView.lastActiveSceneView.camera.transform.position;
        }
        
        return Vector3.zero;
    }
    
    #endregion
}

/// <summary>
/// Окно диагностики системы эффектов поля
/// </summary>
public class FieldEffectDiagnosticsWindow : EditorWindow
{
    private string _diagnosticsText = "";
    private Vector2 _scrollPosition;
    
    public void SetDiagnosticsText(string text)
    {
        _diagnosticsText = text;
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Диагностика системы эффектов поля", EditorStyles.boldLabel);
        
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        EditorGUILayout.TextArea(_diagnosticsText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Обновить"))
        {
            FieldEffectMenus.ShowSystemDiagnostics();
        }
        
        if (GUILayout.Button("Создать FieldEffectSystem"))
        {
            FieldEffectMenus.CreateFieldEffectSystem();
        }
    }
}
#endif