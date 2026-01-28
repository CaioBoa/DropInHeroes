using UnityEngine;
using System;
using System.Collections.Generic;

public class StatsModule : IUnitModule
{
    private UnitController controller;
    private StatDefinitionCatalog catalog;

    private Dictionary<StatType, Stat> stats = new Dictionary<StatType, Stat>();
    private Dictionary<ResourceType, Resource> resources = new Dictionary<ResourceType, Resource>();

    public event Action OnDeath;
    public bool IsDead { get; private set; }

    // === Acesso rápido ===
    public float Attack => GetStat(StatType.Attack);
    public float Defense => GetStat(StatType.Defense);
    public float Speed => GetStat(StatType.Speed);
    public float Range => GetStat(StatType.Range);
    public float MaxHealth => GetStat(StatType.MaxHealth);
    public float CurrentHealth => GetResource(ResourceType.Health);
    public float MaxEnergy => GetStat(StatType.MaxEnergy);
    public float CurrentEnergy => GetResource(ResourceType.Energy);
    public float EnergyRegeneration => GetStat(StatType.EnergyRegeneration);

    // === IUnitModule ===

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        catalog = Resources.Load<StatDefinitionCatalog>("StatDefinitionCatalog");

        var charData = controller.GetCharacterData();
        InitializeStats(charData);
        InitializeResources();

        resources[ResourceType.Health].OnDepleted += HandleDeath;
    }

    public void OnEnabled() { }

    public void OnDisabled() { }

    public void Cleanup()
    {
        if (resources.ContainsKey(ResourceType.Health))
        {
            resources[ResourceType.Health].OnDepleted -= HandleDeath;
        }
        stats.Clear();
        resources.Clear();
        controller = null;
    }

    // === Inicialização ===

    private void InitializeStats(CharacterData data)
    {
        CreateStat(StatType.Attack, data?.baseAttack ?? 10f);
        CreateStat(StatType.Defense, data?.baseDefense ?? 0f);
        CreateStat(StatType.Speed, data?.baseSpeed ?? 3f);
        CreateStat(StatType.Range, data?.baseRange ?? 1.5f);
        CreateStat(StatType.MaxHealth, data?.baseMaxHealth ?? 100f);
        CreateStat(StatType.MaxEnergy, data?.baseMaxEnergy ?? 50f);
        CreateStat(StatType.EnergyRegeneration, data?.baseEnergyRegeneration ?? 5f);
    }

    private void CreateStat(StatType type, float baseValue)
    {
        var definition = catalog?.GetStatDefinition(type);
        stats[type] = new Stat(type, baseValue, definition);
    }

    private void InitializeResources()
    {
        CreateResource(ResourceType.Health, StatType.MaxHealth);
        CreateResource(ResourceType.Energy, StatType.MaxEnergy);
    }

    private void CreateResource(ResourceType type, StatType maxStatType)
    {
        var definition = catalog?.GetResourceDefinition(type);
        resources[type] = new Resource(type, stats[maxStatType], definition);
    }

    // === API Pública ===

    public float GetStat(StatType type)
    {
        return stats.TryGetValue(type, out var stat) ? stat.CurrentValue : 0f;
    }

    public float GetResource(ResourceType type)
    {
        return resources.TryGetValue(type, out var resource) ? resource.CurrentValue : 0f;
    }

    public Stat GetStatObject(StatType type)
    {
        return stats.TryGetValue(type, out var stat) ? stat : null;
    }

    public Resource GetResourceObject(ResourceType type)
    {
        return resources.TryGetValue(type, out var resource) ? resource : null;
    }

    public void ModifyStat(StatType type, float amount)
    {
        if (stats.TryGetValue(type, out var stat))
        {
            stat.Modify(amount);
        }
    }

    public void ModifyResource(ResourceType type, float amount)
    {
        if (!resources.TryGetValue(type, out var resource)) return;

        if (amount > 0)
            resource.Add(amount);
        else
            resource.Remove(-amount);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        float finalDamage = Mathf.Max(0, damage - Defense);
        resources[ResourceType.Health].Remove(finalDamage);

        // Feedback visual
        controller.GetModule<VisualModule>()?.PlayDamageFlash();

        DebugManager.Log($"TakeDamage({damage}) - Final:{finalDamage} - HP: {CurrentHealth}/{MaxHealth}", DebugCategory.Combat);
    }

    public void Heal(float amount)
    {
        resources[ResourceType.Health].Add(amount);
        DebugManager.Log($"Heal({amount}) - HP: {CurrentHealth}/{MaxHealth}", DebugCategory.Combat);
    }

    private void HandleDeath()
    {
        if (IsDead) return;

        IsDead = true;
        DebugManager.Log("Unidade morreu!", DebugCategory.Combat);

        controller.GetModule<CombatModule>()?.SetState(CombatState.Dead);
        OnDeath?.Invoke();
        CombatController.Instance?.OnUnitDied(controller);
    }

    public void ResetForPool()
    {
        foreach (var stat in stats.Values)
            stat.Reset();

        foreach (var resource in resources.Values)
            resource.SetToMax();

        IsDead = false;
    }

    public void ResetStats()
    {
        foreach (var stat in stats.Values)
            stat.Reset();

        DebugManager.Log("Stats resetados para valores base", DebugCategory.Combat);
    }

    /// <summary>
    /// Reinicializa stats com dados do CharacterData.
    /// Chamado após UnitController.Initialize(CharacterData).
    /// </summary>
    public void ApplyCharacterStats(CharacterData data)
    {
        if (data == null) return;

        stats[StatType.Attack].SetBaseValue(data.baseAttack);
        stats[StatType.Defense].SetBaseValue(data.baseDefense);
        stats[StatType.Speed].SetBaseValue(data.baseSpeed);
        stats[StatType.Range].SetBaseValue(data.baseRange);
        stats[StatType.MaxHealth].SetBaseValue(data.baseMaxHealth);
        stats[StatType.MaxEnergy].SetBaseValue(data.baseMaxEnergy);
        stats[StatType.EnergyRegeneration].SetBaseValue(data.baseEnergyRegeneration);

        // Resetar recursos para novo máximo
        foreach (var resource in resources.Values)
            resource.SetToMax();
    }

    // === Combate ===

    public void PrepareForCombat()
    {
        resources[ResourceType.Energy].SetToMin();
    }

    public void RegenerateEnergy(float deltaTime)
    {
        if (IsDead) return;

        float regenAmount = EnergyRegeneration * deltaTime;
        resources[ResourceType.Energy].Add(regenAmount);
    }
}
