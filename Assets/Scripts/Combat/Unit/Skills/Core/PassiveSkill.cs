using UnityEngine;

public abstract class PassiveSkill : ScriptableObject
{
    [Header("Skill Info")]
    public string displayName;

    /// <summary>
    /// Início do combate. Ativa hooks, contadores, etc.
    /// </summary>
    public virtual void Initialize(SkillContext context) { }

    /// <summary>
    /// Final do combate. Reset total ao estado base.
    /// </summary>
    public virtual void Clear() { }

    /// <summary>
    /// Morte ou selamento. Desfaz efeitos sem resetar contadores.
    /// </summary>
    public virtual void Deactivate(SkillContext context) { }

    /// <summary>
    /// Reativação após deactivate (revive, remoção de selo).
    /// </summary>
    public virtual void Reactivate(SkillContext context) { }
}
