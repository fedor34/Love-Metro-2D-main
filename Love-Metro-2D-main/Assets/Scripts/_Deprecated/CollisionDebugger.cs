using UnityEngine;

/// <summary>
/// Детальное отслеживание коллизий для выявления проблемы с VIP персонажами.
/// Прикрепите к любому персонажу для мониторинга его коллизий.
/// </summary>
public class CollisionDebugger : MonoBehaviour
{
    private Passenger _passenger;
    private Rigidbody2D _rb;
    private Collider2D _col;
    private bool _isVIP;
    
    private void Start()
    {
        _passenger = GetComponent<Passenger>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        
        _isVIP = IsVIP();
        
        if (_isVIP)
        {
            Debug.Log($"[CollisionDebugger] Started monitoring VIP: {name}");
            Debug.Log($"  Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");
            Debug.Log($"  Collider: Type={_col?.GetType().Name}, IsTrigger={_col?.isTrigger}, Offset={GetColliderOffset()}, Size={GetColliderSize()}");
            Debug.Log($"  RB: CollisionDetection={_rb?.collisionDetectionMode}, IncludeLayers={_rb?.includeLayers.value}, ExcludeLayers={_rb?.excludeLayers.value}");
        }
    }
    
    private Vector2 GetColliderOffset()
    {
        if (_col is BoxCollider2D box) return box.offset;
        if (_col is CircleCollider2D circle) return circle.offset;
        if (_col is CapsuleCollider2D capsule) return capsule.offset;
        return Vector2.zero;
    }
    
    private Vector2 GetColliderSize()
    {
        if (_col is BoxCollider2D box) return box.size;
        if (_col is CircleCollider2D circle) return new Vector2(circle.radius * 2, circle.radius * 2);
        if (_col is CapsuleCollider2D capsule) return capsule.size;
        return Vector2.zero;
    }
    
    private bool IsVIP()
    {
        if (name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        var anim = GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
            return anim.runtimeAnimatorController.name.IndexOf("VIP", System.StringComparison.OrdinalIgnoreCase) >= 0;
        return false;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isVIP) return;
        
        string otherName = collision.gameObject.name;
        bool isBoundary = otherName.ToLower().Contains("wall") || 
                         otherName.ToLower().Contains("border") || 
                         otherName.ToLower().Contains("boundary");
        
        if (isBoundary)
        {
            Debug.LogWarning($"[CollisionDebugger] ✓ VIP {name} COLLIDED with boundary: {otherName}");
            Debug.LogWarning($"  Contact point: {collision.contacts[0].point}");
            Debug.LogWarning($"  Normal: {collision.contacts[0].normal}");
            Debug.LogWarning($"  VIP position: {transform.position}");
            Debug.LogWarning($"  VIP velocity: {_rb.velocity}");
        }
        else
        {
            Debug.Log($"[CollisionDebugger] VIP {name} collided with: {otherName}");
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!_isVIP) return;
        
        string otherName = collision.gameObject.name;
        bool isBoundary = otherName.ToLower().Contains("wall") || 
                         otherName.ToLower().Contains("border") || 
                         otherName.ToLower().Contains("boundary");
        
        if (isBoundary)
        {
            // Логируем каждые 2 секунды чтобы не спамить
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[CollisionDebugger] VIP {name} staying on boundary: {otherName}");
            }
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!_isVIP) return;
        
        string otherName = collision.gameObject.name;
        bool isBoundary = otherName.ToLower().Contains("wall") || 
                         otherName.ToLower().Contains("border") || 
                         otherName.ToLower().Contains("boundary");
        
        if (isBoundary)
        {
            Debug.LogWarning($"[CollisionDebugger] VIP {name} LEFT boundary: {otherName}");
            Debug.LogWarning($"  Position: {transform.position}");
        }
    }
    
    private void Update()
    {
        if (!_isVIP) return;
        
        // Проверяем выход за границы экрана
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        bool outOfBounds = screenPos.x < -100 || screenPos.x > Screen.width + 100 ||
                          screenPos.y < -100 || screenPos.y > Screen.height + 100;
        
        if (outOfBounds)
        {
            Debug.LogError($"[CollisionDebugger] !!! VIP {name} OUT OF BOUNDS !!!");
            Debug.LogError($"  World position: {transform.position}");
            Debug.LogError($"  Screen position: {screenPos}");
            Debug.LogError($"  Velocity: {_rb.velocity}");
            Debug.LogError($"  State: {_passenger?.GetCurrentStateName()}");
            Debug.LogError($"  Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }
    }
}



