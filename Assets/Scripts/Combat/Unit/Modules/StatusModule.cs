using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Módulo responsável por efeitos de status (buffs, debuffs, condições)
/// PLACEHOLDER - será implementado na fase de combate
/// </summary>
public class StatusModule : IUnitModule
{
    private UnitController controller;
    private List<StatusEffect> activeStatuses = new List<StatusEffect>();

    // === INTERFACE IMPLEMENTATION ===

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        activeStatuses.Clear();

        DebugManager.Log("StatusModule inicializado (placeholder)", DebugCategory.Combat);
    }

    public void OnEnabled()
    {
        // Futuro: ativar efeitos visuais de status
    }

    public void OnDisabled()
    {
        // Futuro: desativar efeitos visuais
    }

    public void Cleanup()
    {
        activeStatuses.Clear();
        controller = null;
    }

    // === PUBLIC API ===

    /// <summary>
    /// Aplica um efeito de status à unidade
    /// </summary>
    public void ApplyStatus(StatusEffect effect)
    {
        // TODO: Implementar aplicação de status
        if (effect == null) return;

        activeStatuses.Add(effect);
        DebugManager.Log($"Status aplicado: {effect.Id}", DebugCategory.Combat);

        // TODO: Aplicar efeito imediato se houver
        // TODO: Atualizar visual
    }

    /// <summary>
    /// Remove um efeito de status específico
    /// </summary>
    public void RemoveStatus(string statusId)
    {
        // TODO: Implementar remoção
        var status = activeStatuses.Find(s => s.Id == statusId);
        if (status != null)
        {
            activeStatuses.Remove(status);
            DebugManager.Log($"Status removido: {statusId}", DebugCategory.Combat);
        }
    }

    /// <summary>
    /// Verifica se possui um status específico
    /// </summary>
    public bool HasStatus(string statusId)
    {
        return activeStatuses.Exists(s => s.Id == statusId);
    }

    /// <summary>
    /// Processa duração de status (chamado a cada turno)
    /// </summary>
    public void TickStatuses()
    {
        // TODO: Implementar tick de status
        for (int i = activeStatuses.Count - 1; i >= 0; i--)
        {
            var status = activeStatuses[i];

            // Aplicar efeito por turno
            // TODO: Aplicar dano/cura/etc

            // Reduzir duração
            status.Duration--;

            if (status.Duration <= 0)
            {
                activeStatuses.RemoveAt(i);
                DebugManager.Log($"Status expirou: {status.Id}", DebugCategory.Combat);
            }
        }
    }

    // === PROPERTIES ===

    public List<StatusEffect> ActiveStatuses => activeStatuses;
}

/// <summary>
/// Representa um efeito de status (buff, debuff, condição)
/// </summary>
[System.Serializable]
public class StatusEffect
{
    public string Id;
    public string DisplayName;
    public StatusEffectType Type;
    public int Duration; // Em turnos
    public int DamagePerTurn; // Para veneno, queimadura, etc
    public int HealPerTurn; // Para regeneração
    public float StatModifier; // Multiplicador de stat (1.5 = +50%, 0.5 = -50%)
    public StatType AffectedStat;

    public StatusEffect(string id, StatusEffectType type, int duration)
    {
        Id = id;
        Type = type;
        Duration = duration;
    }
}

/// <summary>
/// Tipos de efeitos de status
/// </summary>
public enum StatusEffectType
{
    Buff,       // Efeito positivo
    Debuff,     // Efeito negativo
    DoT,        // Damage over time (veneno, queimadura)
    HoT,        // Heal over time (regeneração)
    Stun,       // Impede ações
    Root,       // Impede movimento
    Silence     // Impede habilidades
}
