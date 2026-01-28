using UnityEngine;

public class HealthBarModule : IUnitModule
{
    private UnitController controller;
    private StatsModule stats;
    private HealthBar healthBar;

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        stats = controller.GetModule<StatsModule>();

        healthBar = controller.GetComponentInChildren<HealthBar>(true);

        if (healthBar == null)
        {
            DebugManager.LogWarning("HealthBar não encontrado no prefab!", DebugCategory.Combat);
            return;
        }

        bool isPlayer = controller.IsPlayerUnit();
        healthBar.Initialize(isPlayer);

        var healthResource = stats?.GetResourceObject(ResourceType.Health);
        if (healthResource != null)
            healthResource.OnValueChanged += OnHealthChanged;

        healthBar.Hide();
    }

    private void OnHealthChanged(float currentHealth)
    {
        if (healthBar == null || stats == null) return;

        float percent = stats.MaxHealth > 0 ? stats.CurrentHealth / stats.MaxHealth : 0;
        healthBar.SetHealthPercent(percent);
    }

    public void OnEnabled()
    {
        healthBar?.Show();
    }

    public void OnDisabled()
    {
        healthBar?.Hide();
    }

    public void Cleanup()
    {
        var healthResource = stats?.GetResourceObject(ResourceType.Health);
        if (healthResource != null)
            healthResource.OnValueChanged -= OnHealthChanged;

        healthBar = null;
        stats = null;
        controller = null;
    }

    public void ResetForPool()
    {
        healthBar?.ResetToFull();
        healthBar?.Hide();
    }
}
