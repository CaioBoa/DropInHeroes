using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Guliver/Supreme")]
public class GuliverSupreme : ActiveSkill
{
    public override float Execute(SkillContext context)
    {
        float speedMult = (context.ownerStats?.Speed ?? 3f) / 12f;
        return context.ownerVisual?.PlaySupremeAnimation(speedMult) ?? 0.5f;
    }

    public override void OnHit(SkillContext context)
    {
        if (context.target == null) return;

        var targetStats = context.target.GetModule<StatsModule>();
        var scalings = new[] { new StatScaling(StatType.Attack, 3f) };
        var result = DamageCalculator.Calculate(context.ownerStats, targetStats, 0f, scalings, DamageConfig.Default);
        targetStats?.ApplyDamage(result);
        context.skills.NotifyDamageDealt(result.finalDamage);
    }
}
