using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Módulo responsável por habilidades/skills da unidade
/// PLACEHOLDER - será implementado na fase de combate
/// </summary>
public class SkillsModule : IUnitModule
{
    private UnitController controller;
    private List<Skill> availableSkills = new List<Skill>();

    // === INTERFACE IMPLEMENTATION ===

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        availableSkills.Clear();

        // TODO: Carregar skills do CharacterData
        // CharacterData data = controller.GetCharacterData();
        // foreach (var skillData in data.skills)
        // {
        //     availableSkills.Add(new Skill(skillData));
        // }

        DebugManager.Log("SkillsModule inicializado (placeholder)", DebugCategory.Combat);
    }

    public void OnEnabled()
    {
        // Futuro: ativar indicadores de skill
    }

    public void OnDisabled()
    {
        // Futuro: desativar indicadores
    }

    public void Cleanup()
    {
        availableSkills.Clear();
        controller = null;
    }

    // === PUBLIC API ===

    /// <summary>
    /// Usa uma habilidade em um alvo
    /// </summary>
    public void UseSkill(string skillId, UnitController target)
    {
        // TODO: Implementar uso de skill
        var skill = availableSkills.Find(s => s.Id == skillId);
        if (skill == null)
        {
            DebugManager.LogWarning($"Skill não encontrada: {skillId}", DebugCategory.Combat);
            return;
        }

        if (!CanUseSkill(skillId))
        {
            DebugManager.LogWarning($"Não pode usar skill: {skillId}", DebugCategory.Combat);
            return;
        }

        // TODO: Executar efeito da skill
        DebugManager.Log($"Usando skill: {skill.DisplayName} em {target?.GetCharacterData()?.displayName}", DebugCategory.Combat);

        // TODO: Iniciar cooldown
        skill.CurrentCooldown = skill.Cooldown;
    }

    /// <summary>
    /// Verifica se pode usar uma skill
    /// </summary>
    public bool CanUseSkill(string skillId)
    {
        var skill = availableSkills.Find(s => s.Id == skillId);
        if (skill == null) return false;

        // TODO: Verificar cooldown, mana/energy, silenciado, etc
        return skill.CurrentCooldown <= 0;
    }

    /// <summary>
    /// Reduz cooldown de todas as skills (chamado a cada turno)
    /// </summary>
    public void ReduceCooldowns()
    {
        foreach (var skill in availableSkills)
        {
            if (skill.CurrentCooldown > 0)
            {
                skill.CurrentCooldown--;
                DebugManager.Log($"Cooldown de {skill.DisplayName}: {skill.CurrentCooldown}", DebugCategory.Combat);
            }
        }
    }

    /// <summary>
    /// Adiciona uma nova skill à unidade
    /// </summary>
    public void LearnSkill(Skill skill)
    {
        // TODO: Implementar aprendizado de skill
        if (skill == null) return;

        if (!availableSkills.Exists(s => s.Id == skill.Id))
        {
            availableSkills.Add(skill);
            DebugManager.Log($"Skill aprendida: {skill.DisplayName}", DebugCategory.Combat);
        }
    }

    // === PROPERTIES ===

    public List<Skill> AvailableSkills => availableSkills;
}

/// <summary>
/// Representa uma habilidade/skill
/// </summary>
[System.Serializable]
public class Skill
{
    public string Id;
    public string DisplayName;
    public string Description;
    public SkillType Type;
    public int Cooldown; // Em turnos
    public int CurrentCooldown;
    public int Damage;
    public int Healing;
    public TargetType TargetType;
    public int Range; // Em tiles
    public int ManaCost; // Ou energy cost

    public Skill(string id, string name, SkillType type)
    {
        Id = id;
        DisplayName = name;
        Type = type;
        CurrentCooldown = 0;
    }
}

/// <summary>
/// Tipos de habilidades
/// </summary>
public enum SkillType
{
    Attack,     // Dano direto
    Heal,       // Cura
    Buff,       // Buff em aliado
    Debuff,     // Debuff em inimigo
    AoE,        // Área de efeito
    Utility     // Utilidade (teleporte, escudo, etc)
}

/// <summary>
/// Tipos de alvo
/// </summary>
public enum TargetType
{
    Self,           // Em si mesmo
    SingleAlly,     // Um aliado
    SingleEnemy,    // Um inimigo
    AllAllies,      // Todos aliados
    AllEnemies,     // Todos inimigos
    Area            // Área (requer posição)
}
