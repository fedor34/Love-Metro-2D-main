using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Emergency runtime guard: if a passenger slips below the playfield floor,
/// forcefully teleports them back into bounds and kicks them upward.
/// </summary>
public class VipBoundaryReturnSystem : MonoBehaviour
{
    private static VipBoundaryReturnSystem _instance;

    [Header("Detection")]
    [SerializeField] private float _floorThresholdY = -3.5f;
    [SerializeField] private float _safeReturnY = -2.95f;
    [SerializeField] private Vector2 _xClamp = new Vector2(-6.5f, 6.5f);
    [SerializeField] private float _scanInterval = 0.2f;

    [Header("Recovery")]
    [SerializeField] private float _returnKickForce = 6f;

    private float _timer;
    private LoveMetro.Core.IPassengerRegistry _passengerRegistry;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
            return;

        var go = new GameObject(nameof(VipBoundaryReturnSystem));
        DontDestroyOnLoad(go);
        go.AddComponent<VipBoundaryReturnSystem>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _passengerRegistry = null;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = _scanInterval;

        _passengerRegistry ??= LoveMetro.Core.RuntimeServices.Instance.PassengerRegistry ?? PassengerRegistry.Instance;
        if (_passengerRegistry == null)
            return;

        var passengers = _passengerRegistry.AllPassengers;
        for (int i = 0; i < passengers.Count; i++)
        {
            var passenger = passengers[i];
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
            rb.collisionDetectionMode = passenger.Settings.collisionDetectionMode;
            rb.AddForce(Vector2.up * _returnKickForce, ForceMode2D.Impulse);
        }

        passenger.ResetPhysicsCollisionFilters();

        Diagnostics.Warn($"[VipBoundaryReturn] Forced {passenger.name} back inside play area from {currentPos} -> {restoredPos}");
    }
}

