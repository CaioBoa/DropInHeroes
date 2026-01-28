using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Guliver/Passive")]
public class GuliverPassive : PassiveSkill
{
    public override void Initialize(SkillContext context)
    {
        context.skills.Hooks.onDamageTaken += OnDamageTaken;
    }

    private void OnDamageTaken(float amount)
    {
        DebugManager.Log($"[WarriorPassive] Dano recebido: {amount}", DebugCategory.Combat);
    }
}
