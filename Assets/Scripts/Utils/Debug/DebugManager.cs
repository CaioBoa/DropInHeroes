using UnityEngine;

/// <summary>
/// Sistema centralizado de debug com categorias coloridas e filtráveis
/// Uso: DebugManager.Log("mensagem", DebugCategory.Drag);
/// </summary>
public static class DebugManager
{
    private static DebugConfig config;
    private static bool initialized = false;

    private static void EnsureInitialized()
    {
        if (initialized) return;

        config = Resources.Load<DebugConfig>("DebugConfig");
        if (config == null)
        {
            Debug.LogWarning("[DebugManager] DebugConfig não encontrado em Resources! Logs serão exibidos sem filtro.");
        }
        initialized = true;
    }

    /// <summary>
    /// Log normal com categoria
    /// </summary>
    public static void Log(string message, DebugCategory category, Object context = null)
    {
        EnsureInitialized();

        if (config != null && !config.IsCategoryEnabled(category)) return;

        string formattedMessage = FormatMessage(message, category);
        Debug.Log(formattedMessage, context);
    }

    /// <summary>
    /// Warning com categoria
    /// </summary>
    public static void LogWarning(string message, DebugCategory category, Object context = null)
    {
        EnsureInitialized();

        if (config != null && !config.IsCategoryEnabled(category)) return;

        string formattedMessage = FormatMessage(message, category);
        Debug.LogWarning(formattedMessage, context);
    }

    /// <summary>
    /// Error com categoria (sempre exibido, independente do filtro)
    /// </summary>
    public static void LogError(string message, DebugCategory category, Object context = null)
    {
        EnsureInitialized();

        // Erros sempre são mostrados
        string formattedMessage = FormatMessage(message, category);
        Debug.LogError(formattedMessage, context);
    }

    private static string FormatMessage(string message, DebugCategory category)
    {
        if (config == null) return $"[{category}] {message}";

        string prefix = config.showCategoryPrefix ? $"[{category}] " : "";
        string timestamp = config.showTimestamp ? $"[{Time.time:F2}] " : "";

        Color color = config.GetCategoryColor(category);
        string hexColor = ColorUtility.ToHtmlStringRGB(color);

        return $"<color=#{hexColor}>{timestamp}{prefix}{message}</color>";
    }

    /// <summary>
    /// Força reinicialização (útil para recarregar config no editor)
    /// </summary>
    public static void Reinitialize()
    {
        initialized = false;
        config = null;
        EnsureInitialized();
    }
}
