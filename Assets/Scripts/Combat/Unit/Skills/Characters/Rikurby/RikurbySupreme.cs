using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo do Rikurby: salta para o centro do maior aglomerado de inimigos (onCast) e, no impacto, causa
    /// dano FÍSICO em área, se cura por inimigo atingido e atordoa os inimigos ao redor. Definido em código;
    /// o SO só expõe os números e o clipe de VFX.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Rikurby/Supreme")]
    public class RikurbySupreme : ModularSupreme
    {
        [Header("Salto")]
        [SerializeField] private float leapDuration = 0.3f;
        [SerializeField] private float aoeRadius = 2.5f;

        [Header("Dano em área")]
        [Tooltip("Multiplicador do Ataque (12.0 = 1200%).")]
        [SerializeField] private float damageAttackScaling = 12.0f;
        [Tooltip("Multiplicador da Vida Máxima (0.4 = 40%).")]
        [SerializeField] private float damageHealthScaling = 0.4f;

        [Header("Cura por inimigo atingido")]
        [Tooltip("Multiplicador da Vida Máxima por inimigo atingido (0.2 = 20%).")]
        [SerializeField] private float healHealthScaling = 0.2f;

        [Header("Atordoamento (inimigos ao redor)")]
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private float stunChance = 1f;

        [SerializeField] private AnimationClip hitVfxClip;

        private StatScaling[] dmgScalings;
        private StatScaling[] healScalings;

        protected override void OnCast(SkillContext context)
            => SkillModules.LeapToBestAoe(context, aoeRadius, clusterEnemies: true, leapDuration);

        protected override void OnImpact(SkillContext context, ref EffectRunState state)
        {
            dmgScalings ??= new[] { new StatScaling(StatType.Attack, damageAttackScaling), new StatScaling(StatType.MaxHealth, damageHealthScaling) };
            healScalings ??= new[] { new StatScaling(StatType.MaxHealth, healHealthScaling) };

            SkillModules.Vfx(context, Targets.Focus(context), hitVfxClip, ref state, scale: 1f, fitDiameter: 5f, sortingOffset: 1);
            // Dano ANTES da cura: só o Damage grava previousTargetCount (a cura escala por nº de atingidos).
            SkillModules.Damage(context, Targets.AroundSelf(context, aoeRadius, enemies: true), dmgScalings, DamageType.Physical, ref state);
            SkillModules.Heal(context, Targets.Self(context), healScalings, ref state, scaleByPreviousHits: true);
            SkillModules.Status(context, Targets.AroundSelf(context, aoeRadius, enemies: true), "rikurby_supreme_stun", stunDuration, null, ref state, chance: stunChance);
        }
    }
}
