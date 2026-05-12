using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class GameplayCameraSettings : MonoBehaviour
{
    [SerializeField] private float _orthographicSize = 6.65f;
    [SerializeField] private bool _enforceAtRuntime = true;
    [SerializeField] private bool _logRuntimeCorrections = true;

    private Camera _camera;

    private void Reset()
    {
        CacheCamera();
        if (_camera != null)
            _orthographicSize = _camera.orthographicSize;
    }

    private void OnValidate()
    {
        _orthographicSize = Mathf.Max(0.01f, _orthographicSize);
        ApplyCameraSettings(logCorrection: false);
    }

    private void Awake()
    {
        ApplyCameraSettings(logCorrection: false);
    }

    private void LateUpdate()
    {
        if (Application.isPlaying && _enforceAtRuntime)
            ApplyCameraSettings(logCorrection: _logRuntimeCorrections);
    }

    private void CacheCamera()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
    }

    private void ApplyCameraSettings(bool logCorrection)
    {
        CacheCamera();
        if (_camera == null || !_camera.orthographic)
            return;

        if (Mathf.Approximately(_camera.orthographicSize, _orthographicSize))
            return;

        float previousSize = _camera.orthographicSize;
        _camera.orthographicSize = _orthographicSize;

        if (logCorrection && Application.isPlaying)
        {
            Debug.LogWarning(
                $"[GameplayCameraSettings] Corrected {name} orthographicSize {previousSize:F4} -> {_orthographicSize:F4}. " +
                $"scene={gameObject.scene.name} aspect={_camera.aspect:F4} pixelRect={_camera.pixelRect}");
        }
    }
}
