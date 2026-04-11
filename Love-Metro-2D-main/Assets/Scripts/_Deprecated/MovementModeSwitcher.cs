using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deprecated compatibility component kept so old scenes do not get a missing script.
/// Passenger movement strategies were removed from Passenger during the state-machine refactor.
/// </summary>
public class MovementModeSwitcher : MonoBehaviour
{
    [SerializeField] private Text _label;

    private const string RemovedMessage = "Movement mode switching was removed";

    private void Start()
    {
        UpdateLabel();
    }

    public void CycleMode()
    {
        Diagnostics.Warn("[MovementModeSwitcher] Deprecated component is disabled. Passenger movement modes were removed.");
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (_label != null)
            _label.text = RemovedMessage;
    }
}
