using UnityEngine;

public abstract class ActiveSkill : ScriptableObject
{
    [Header("Skill Info")]
    public string displayName;
    public ActiveSkillType skillType;

    public virtual void Initialize(SkillContext context) { }

    public virtual void Clear() { }

    /// <summary>
    /// Executa a skill. Retorna a duração da animação.
    /// </summary>
    public abstract float Execute(SkillContext context);

    /// <summary>
    /// Chamado pelo animation event no frame de impacto.
    /// </summary>
    public abstract void OnHit(SkillContext context);
}
