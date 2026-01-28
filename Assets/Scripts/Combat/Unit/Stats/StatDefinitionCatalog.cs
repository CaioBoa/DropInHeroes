using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StatDefinitionCatalog", menuName = "Game/System/Stat Definitions")]
public class StatDefinitionCatalog : ScriptableObject
{
    [Header("Stat Definitions")]
    [SerializeField] private List<StatDefinitionEntry> statDefinitions = new List<StatDefinitionEntry>();

    [Header("Resource Definitions")]
    [SerializeField] private List<ResourceDefinitionEntry> resourceDefinitions = new List<ResourceDefinitionEntry>();

    private Dictionary<StatType, StatDefinition> statLookup;
    private Dictionary<ResourceType, StatDefinition> resourceLookup;
    private bool isInitialized;

    public void Initialize()
    {
        if (isInitialized) return;

        statLookup = new Dictionary<StatType, StatDefinition>();
        foreach (var entry in statDefinitions)
        {
            statLookup[entry.type] = entry.definition;
        }

        resourceLookup = new Dictionary<ResourceType, StatDefinition>();
        foreach (var entry in resourceDefinitions)
        {
            resourceLookup[entry.type] = entry.definition;
        }

        isInitialized = true;
    }

    public StatDefinition GetStatDefinition(StatType type)
    {
        if (!isInitialized) Initialize();
        return statLookup.TryGetValue(type, out var def) ? def : null;
    }

    public StatDefinition GetResourceDefinition(ResourceType type)
    {
        if (!isInitialized) Initialize();
        return resourceLookup.TryGetValue(type, out var def) ? def : null;
    }
}

[System.Serializable]
public class StatDefinitionEntry
{
    public StatType type;
    public StatDefinition definition;
}

[System.Serializable]
public class ResourceDefinitionEntry
{
    public ResourceType type;
    public StatDefinition definition;
}
