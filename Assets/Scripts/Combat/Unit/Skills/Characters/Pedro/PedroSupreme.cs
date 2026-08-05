using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo do Pedro — "Chute Loiro": avança até o alvo (onCast) e, no impacto, causa dano físico,
    /// reduz a Defesa dele e concede Aumento de Ataque a todos os aliados. Definido em código; SO só números.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Pedro/Supreme")]
    public class PedroSupreme : ModularSupreme
    {
        [Header("Avanço")]
        [SerializeField] private float leapDuration = 0.3f;
        [SerializeField] private float landingGap = 1f;

        [Header("Dano")]
        [SerializeField] private float damageScaling = 4.0f; // 400% Ataque

        [Header("Reduzir Defesa do alvo")]
        [SerializeField] private float defenseDownPercent = -0.4f;
        [SerializeField] private float defenseDownChance = 0.85f;
        [SerializeField] private float defenseDownDuration = 5f;

        [Header("Aumento de Ataque aos aliados")]
        [SerializeField] private float attackUpPercent = 0.3f;
        [SerializeField] private float attackUpDuration = 5f;

        private StatScaling[] dmgScalings;
        private StatModification[] defMods;
        private StatModification[] atkMods;

        protected override void OnCast(SkillContext context)
            => SkillModules.LeapToTarget(context, context.target, leapDuration, landingGap);

        protected override void OnImpact(SkillContext context, ref EffectRunState state)
        {
            dmgScalings ??= new[] { new StatScaling(StatType.Attack, damageScaling) };
            defMods ??= new[] { StatModification.Percent(StatType.Defense, defenseDownPercent) };
            atkMods ??= new[] { StatModification.Percent(StatType.Attack, attackUpPercent) };

            SkillModules.Damage(context, Targets.Focus(context), dmgScalings, DamageType.Physical, ref state);
            SkillModules.Status(context, Targets.Focus(context), "def_down", defenseDownDuration, defMods, ref state, chance: defenseDownChance);
            SkillModules.Status(context, Targets.Allies(context, includeSelf: true), "atk_up", attackUpDuration, atkMods, ref state);
        }
    }
}
