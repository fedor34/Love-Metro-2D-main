using UnityEngine;

/// <summary>
/// Автоматически прикрепляет CollisionDebugger ко всем VIP персонажам.
/// </summary>
public class AutoAttachCollisionDebugger : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var go = new GameObject("AutoAttachCollisionDebugger");
        go.AddComponent<AutoAttachCollisionDebugger>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        // Проверяем каждую секунду
        InvokeRepeating(nameof(CheckAndAttach), 1f, 1f);
    }

    private void CheckAndAttach()
    {
        var passengers = FindObjectsOfType<Passenger>();
        int attached = 0;
        
        foreach (var p in passengers)
        {
            if (p == null) continue;
            
            // Проверяем, является ли VIP
            bool isVIP = IsVIP(p);
            if (!isVIP) continue;
            
            // Проверяем, есть ли уже CollisionDebugger
            var debugger = p.GetComponent<CollisionDebugger>();
            if (debugger != null) continue;
            
            // Прикрепляем
            p.gameObject.AddComponent<CollisionDebugger>();
            attached++;
            Debug.Log($"[AutoAttachCollisionDebugger] Attached to VIP: {p.name}");
        }
        
        if (attached > 0)
        {
            Debug.Log($"[AutoAttachCollisionDebugger] Total attached: {attached}");
        }
    }
    
    private bool IsVIP(Passenger p)
    {
        if (p.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        var anim = p.GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
            return anim.runtimeAnimatorController.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
        return false;
    }
}



