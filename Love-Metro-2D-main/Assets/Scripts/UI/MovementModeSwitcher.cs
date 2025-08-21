using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI helper to cycle movement strategies at runtime for comparison.
/// Add to any GameObject in scene; hook to a Button onClick or call via key.
/// </summary>
public class MovementModeSwitcher : MonoBehaviour
{
    [SerializeField] private Text _label;

    private void Update()
    {
        // Quick test: press M to cycle
        if (Input.GetKeyDown(KeyCode.M))
        {
            CycleMode();
        }
    }

    public void CycleMode()
    {
        foreach (var p in FindObjectsOfType<Passenger>())
        {
            // Cycle enum and re-init strategy by re-calling Initiate minimal part
            var field = typeof(Passenger).GetField("_movementMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) continue;
            var mode = (Passenger.MovementMode)field.GetValue(p);
            mode = (Passenger.MovementMode)(((int)mode + 1) % 4);
            field.SetValue(p, mode);

            // Force re-pick of strategy without respawn
            var method = typeof(MovementModeSwitcher).GetMethod("ApplyModeToPassenger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            ApplyModeToPassenger(p, mode);
        }

        UpdateLabel();
    }

    private static void ApplyModeToPassenger(Passenger p, Passenger.MovementMode mode)
    {
        var strategyField = typeof(Passenger).GetField("_movementStrategy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        IPassengerMovementStrategy strategy;
        switch (mode)
        {
            case Passenger.MovementMode.SteeringSmooth:
                strategy = new SteeringSmoothStrategy();
                break;
            case Passenger.MovementMode.LaneBased:
                strategy = new LaneBasedStrategy(1.0f, 3.0f);
                break;
            case Passenger.MovementMode.BoidsLite:
                strategy = new BoidsLiteStrategy();
                break;
            default:
                strategy = new LegacyMovementStrategy();
                break;
        }
        strategyField.SetValue(p, strategy);
    }

    private void UpdateLabel()
    {
        if (_label == null) return;
        var any = FindObjectOfType<Passenger>();
        if (any == null) { _label.text = "No passengers"; return; }
        var field = typeof(Passenger).GetField("_movementMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mode = (Passenger.MovementMode)field.GetValue(any);
        _label.text = $"Movement: {mode}";
    }
}

