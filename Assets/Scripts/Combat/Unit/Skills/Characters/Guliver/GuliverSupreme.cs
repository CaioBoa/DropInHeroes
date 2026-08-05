using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo do Guliver — "Investida Divina": salta para o melhor centro de AoE nos inimigos (onCast),
    /// e no impacto causa dano físico em área (ao redor de si), cura os aliados e concede Defesa a eles.
    /// Definido em código; o SO só expõe os números e o clipe de VFX.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Guliver/Supreme")]
    public class GuliverSupreme : ModularSupreme
    {
        [Header("Salto / área")]
        [SerializeField] private float leapDuration = 0.2f;
        [SerializeField] private float aoeRadius = 4f;

        [Header("Dano em área (multiplicadores)")]
        [SerializeField] private float damageAttackScaling = 6.0f;   // 600% Ataque
        [SerializeField] private float damageHealthScaling = 0.6f;   // 60% Vida Máx.

        [Header("Cura aos aliados (multiplicadores)")]
        [SerializeField] private float healMaxHealthScaling = 0.15f; // 15% Vida Máx.
        [SerializeField] private float healMagicScaling = 5.0f;      // 500% Poder Mágico

        [Header("Buff de Defesa")]
        [SerializeField] private float defenseBuff = 0.30f;          // +30%
        [SerializeField] private float buffDuration = 10f;

        [Header("VFX")]
        [SerializeField] private AnimationClip hitVfxClip;

        private StatScaling[] dmgScalings;
        private StatScaling[] healScalings;
        private StatModification[] defenseMods;

        protected override void OnCast(SkillContext context)
            => SkillModules.LeapToBestAoe(context, aoeRadius, clusterEnemies: true, leapDuration);

        protected override void OnImpact(SkillContext context, ref EffectRunState state)
        {
            dmgScalings ??= new[] { new StatScaling(StatType.Attack, damageAttackScaling), new StatScaling(StatType.MaxHealth, damageHealthScaling) };
            healScalings ??= new[] { new StatScaling(StatType.MaxHealth, healMaxHealthScaling), new StatScaling(StatType.MagicalPower, healMagicScaling) };
            defenseMods ??= new[] { StatModification.Percent(StatType.Defense, defenseBuff) };

            SkillModules.Vfx(context, Targets.Focus(context), hitVfxClip, ref state, fitDiameter: aoeRadius * 2f);
            SkillModules.Damage(context, Targets.AroundSelf(context, aoeRadius, enemies: true), dmgScalings, DamageType.Physical, ref state);
            SkillModules.Heal(context, Targets.Allies(context, includeSelf: true), healScalings, ref state);
            SkillModules.Status(context, Targets.Allies(context, includeSelf: true), "guliver_supreme_def", buffDuration, defenseMods, ref state);
        }
    }
}
