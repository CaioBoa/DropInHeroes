using UnityEngine;

public class EnergyBarModule : IUnitModule
{
    private UnitController controller;
    private StatsModule stats;
    private EnergyBar energyBar;

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        stats = controller.GetModule<StatsModule>();

        energyBar = controller.GetComponentInChildren<EnergyBar>(true);

        if (energyBar == null)
        {
            DebugManager.LogWarning("EnergyBar não encontrado no prefab!", DebugCategory.Combat);
            return;
        }

        energyBar.Initialize();

        var energyResource = stats?.GetResourceObject(ResourceType.Energy);
        if (energyResource != null)
            energyResource.OnValueChanged += OnEnergyChanged;

        energyBar.Hide();
    }

    private void OnEnergyChanged(float currentEnergy)
    {
        if (energyBar == null || stats == null) return;

        float percent = stats.MaxEnergy > 0 ? stats.CurrentEnergy / stats.MaxEnergy : 0;
        energyBar.SetEnergyPercent(percent);
    }

    public void OnEnabled()
    {
        energyBar?.Show();
    }

    public void OnDisabled()
    {
        energyBar?.Hide();
    }

    public void Cleanup()
    {
        var energyResource = stats?.GetResourceObject(ResourceType.Energy);
        if (energyResource != null)
            energyResource.OnValueChanged -= OnEnergyChanged;

        energyBar = null;
        stats = null;
        controller = null;
    }

    public void ResetForPool()
    {
        energyBar?.ResetToEmpty();
        energyBar?.Hide();
    }
}
