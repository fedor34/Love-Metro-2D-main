using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Emergency runtime guard: if a passenger slips below the playfield floor,
/// forcefully teleports them back into bounds and kicks them upward.
/// </summary>
public class VipBoundaryReturnSystem : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float _floorThresholdY = -3.5f;
    [SerializeField] private float _safeReturnY = -2.95f;
    [SerializeField] private Vector2 _xClamp = new Vector2(-6.5f, 6.5f);
    [SerializeField] private float _scanInterval = 0.2f;

    [Header("Recovery")]
    [SerializeField] private float _returnKickForce = 6f;

    private float _timer;
    private PassangersContainer _cachedContainer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<VipBoundaryReturnSystem>() != null)
            return;

        var go = new GameObject(nameof(VipBoundaryReturnSystem));
        DontDestroyOnLoad(go);
        go.AddComponent<VipBoundaryReturnSystem>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cachedContainer = null; // force re-scan next tick
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = _scanInterval;

        if (_cachedContainer == null || _cachedContainer.Passangers == null)
        {
            _cachedContainer = FindObjectOfType<PassangersContainer>();
        }
        if (_cachedContainer == null || _cachedContainer.Passangers == null)
            return;

        for (int i = 0; i < _cachedContainer.Passangers.Count; i++)
        {
            var passenger = _cachedContainer.Passangers[i];
            if (passenger == null) continue;
            TryReturn(passenger);
        }
    }

    private void TryReturn(Passenger passenger)
    {
        Vector3 currentPos = passenger.transform.position;
        if (currentPos.y >= _floorThresholdY) return;

        Vector3 restoredPos = new Vector3(
            Mathf.Clamp(currentPos.x, _xClamp.x, _xClamp.y),
            _safeReturnY,
            currentPos.z);

        passenger.transform.position = restoredPos;

        var rb = passenger.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.includeLayers = Physics2D.AllLayers;
            rb.excludeLayers = 0;
            rb.AddForce(Vector2.up * _returnKickForce, ForceMode2D.Impulse);
        }

        var col = passenger.GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            col.includeLayers = Physics2D.AllLayers;
            col.excludeLayers = 0;
        }

        Diagnostics.Warn($"[VipBoundaryReturn] Forced {passenger.name} back inside play area from {currentPos} -> {restoredPos}");
    }
}

