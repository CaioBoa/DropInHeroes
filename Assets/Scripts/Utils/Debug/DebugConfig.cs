using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Configurações centralizadas para o sistema de debug
/// Permite habilitar/desabilitar categorias e definir cores
/// </summary>
[CreateAssetMenu(menuName = "Debug/Debug Config", fileName = "DebugConfig")]
public class DebugConfig : ScriptableObject
{
    [System.Serializable]
    public class CategorySettings
    {
        public DebugCategory category;
        public bool enabled = true;
        public Color color = Color.white;
    }

    [Header("Global Settings")]
    [Tooltip("Habilita/desabilita todo o sistema de debug")]
    public bool globalEnabled = true;

    [Tooltip("Mostra timestamp no início da mensagem")]
    public bool showTimestamp = false;

    [Tooltip("Mostra prefixo da categoria na mensagem")]
    public bool showCategoryPrefix = true;

    [Header("Categories")]
    public List<CategorySettings> categories = new List<CategorySettings>();

    private Dictionary<DebugCategory, CategorySettings> categoryLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    public void BuildLookup()
    {
        categoryLookup = new Dictionary<DebugCategory, CategorySettings>();
        foreach (var setting in categories)
        {
            categoryLookup[setting.category] = setting;
        }
    }

    public bool IsCategoryEnabled(DebugCategory category)
    {
        if (!globalEnabled) return false;
        if (categoryLookup == null) BuildLookup();
        return categoryLookup.TryGetValue(category, out var settings) && settings.enabled;
    }

    public Color GetCategoryColor(DebugCategory category)
    {
        if (categoryLookup == null) BuildLookup();
        return categoryLookup.TryGetValue(category, out var settings) ? settings.color : Color.white;
    }

    /// <summary>
    /// Inicializa categorias padrão (chamado pelo editor ou em runtime)
    /// </summary>
    public void InitializeDefaultCategories()
    {
        categories = new List<CategorySettings>
        {
            new CategorySettings { category = DebugCategory.Initialization, enabled = true, color = Color.cyan },
            new CategorySettings { category = DebugCategory.State, enabled = true, color = Color.magenta },
            new CategorySettings { category = DebugCategory.Character, enabled = true, color = Color.yellow },
            new CategorySettings { category = DebugCategory.Interaction, enabled = true, color = Color.green },
            new CategorySettings { category = DebugCategory.Drag, enabled = true, color = new Color(1f, 0.5f, 0f) }, // Orange
            new CategorySettings { category = DebugCategory.Combat, enabled = true, color = Color.red },
            new CategorySettings { category = DebugCategory.UI, enabled = true, color = Color.white },
            new CategorySettings { category = DebugCategory.Pool, enabled = true, color = Color.gray }
        };
        BuildLookup();
    }
}
