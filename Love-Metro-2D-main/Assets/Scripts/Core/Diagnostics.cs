using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// Централизованная диагностика.
/// В Development Build логи работают, в Release Build - отключены через Conditional атрибут.
/// Для принудительного включения в редакторе установите Enabled = true.
/// </summary>
public static class Diagnostics
{
    // Включить для verbose логов в Editor режиме
    public static bool Enabled = true;

    /// <summary>
    /// Логирует сообщение. В Release сборках этот метод полностью вырезается компилятором.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("DIAGNOSTICS_ENABLED")]
    public static void Log(string message)
    {
        if (Enabled) Debug.Log(message);
    }

    /// <summary>
    /// Логирует предупреждение. В Release сборках вырезается.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("DIAGNOSTICS_ENABLED")]
    public static void Warn(string message)
    {
        if (Enabled) Debug.LogWarning(message);
    }

    /// <summary>
    /// Логирует ошибку. Работает всегда (ошибки важны даже в Release).
    /// </summary>
    public static void Error(string message)
    {
        Debug.LogError(message);
    }

    /// <summary>
    /// Логирует с указанием категории. В Release сборках вырезается.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("DIAGNOSTICS_ENABLED")]
    public static void LogCategory(string category, string message)
    {
        if (Enabled) Debug.Log($"[{category}] {message}");
    }
}

