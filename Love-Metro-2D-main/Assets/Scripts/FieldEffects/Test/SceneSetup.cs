using UnityEngine;

[System.Serializable]
public class SceneSetup : MonoBehaviour
{
    [Header("Автонастройка сцены")]
    [SerializeField] private bool _setupOnAwake = true;
    
    private void Awake()
    {
        if (!_setupOnAwake) return;
        
        SetupScene();
    }
    
    private void SetupScene()
    {
        Debug.Log("[SceneSetup] Настройка сцены...");
        
        // Создаем FieldEffectSystem
        if (FieldEffectSystem.Instance == null)
        {
            var systemObj = new GameObject("FieldEffectSystem");
            systemObj.AddComponent<FieldEffectSystem>();
            Debug.Log("[SceneSetup] FieldEffectSystem создан");
        }
        
        // Создаем BlackHoleTest
        if (FindObjectOfType<BlackHoleTest>() == null)
        {
            var blackHoleObj = new GameObject("BlackHoleTest");
            blackHoleObj.transform.position = new Vector3(0, 0, 0);
            blackHoleObj.AddComponent<BlackHoleTest>();
            Debug.Log("[SceneSetup] BlackHoleTest создан");
        }
        
        Debug.Log("[SceneSetup] Настройка сцены завершена");
    }
} 