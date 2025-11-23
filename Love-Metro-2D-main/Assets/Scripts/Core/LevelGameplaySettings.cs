using UnityEngine;

/// <summary>
/// Scene-level gameplay toggles. Drop it onto any GameObject in the scene.
/// Provides a simple checkbox to enable a "slippery floor" feel for the wagon.
/// Other systems (e.g., Passenger) read its static flag at runtime.
/// </summary>
[DefaultExecutionOrder(-500)]
public class LevelGameplaySettings : MonoBehaviour
{
    [Header("Level Toggles")]
    [Tooltip("If enabled, passengers preserve momentum (slippery floor).")]
    public bool slipperyFloor = false;

    [Header("Slippery Floor Tuning")] 
    [Tooltip("Linear drag applied to passengers when slippery floor is on.")]
    public float slipperyLinearDrag = 0.05f;

    public static bool SlipperyFloorEnabled { get; private set; }
    public static float SlipperyLinearDrag { get; private set; } = 0.05f;

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        // Keep static values in sync in editor when tweaking
        SlipperyLinearDrag = Mathf.Max(0f, slipperyLinearDrag);
    }

    public void Apply()
    {
        SlipperyFloorEnabled = slipperyFloor;
        SlipperyLinearDrag = Mathf.Max(0f, slipperyLinearDrag);
    }
}

