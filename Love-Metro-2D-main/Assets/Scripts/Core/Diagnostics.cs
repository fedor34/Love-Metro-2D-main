using UnityEngine;

/// <summary>
/// Centralized runtime diagnostics. Toggle <see cref="Enabled"/> to reduce noise.
/// </summary>
public static class Diagnostics
{
    // Turn on to get verbose logs
    public static bool Enabled = true;

    public static void Log(string message)
    {
        if (Enabled) Debug.Log(message);
    }

    public static void Warn(string message)
    {
        if (Enabled) Debug.LogWarning(message);
    }

    public static void Error(string message)
    {
        Debug.LogError(message);
    }
}

