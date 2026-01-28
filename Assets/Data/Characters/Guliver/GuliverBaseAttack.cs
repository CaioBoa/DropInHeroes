using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Guliver/Base Attack")]
public class GuliverBaseAttack : ActiveSkill
{
    public override float Execute(SkillContext context)
    {
        float speedMult = (context.ownerStats?.Speed ?? 3f) / 12f;
        return context.ownerVisual?.PlayAttackAnimation(speedMult) ?? 0.5f;
    }

    public override void OnHit(SkillContext context)
    {
        if (context.target == null) return;

        var targetStats = context.target.GetModule<StatsModule>();
        var scalings = new[] { new StatScaling(StatType.Attack, 1f) };
        var result = DamageCalculator.Calculate(context.ownerStats, targetStats, 0f, scalings, DamageConfig.Default);
        targetStats?.ApplyDamage(result);
        context.skills.NotifyDamageDealt(result.finalDamage);
    }
}
