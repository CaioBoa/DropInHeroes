using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva do Hami — "Fúria Ardente": a cada golpe básico, causa dano físico bônus no alvo e converte
    /// parte do Poder Mágico em Ataque (permanente no combate, acumula). Aplicada como on-hit (rider — não
    /// crita, o ganho de Ataque acumula por ataque mesmo em esquiva). Definida em código; o SO só expõe os números.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Hami/Passive")]
    public class HamiPassive : ModularOnHitPassive
    {
        [Tooltip("Multiplicador do Ataque no dano físico bônus (0.5 = 50%).")]
        [SerializeField] private float bonusDamageScaling = 0.5f;
        [Tooltip("Percentual do Poder Mágico convertido em Ataque por golpe (5 = 5%).")]
        [SerializeField] private float attackGainPercent = 5f;

        private StatScaling[] bonusScalings;

        protected override void OnHitStrike(SkillContext context, ref EffectRunState state)
        {
            bonusScalings ??= new[] { new StatScaling(StatType.Attack, bonusDamageScaling) };
            SkillModules.Damage(context, Targets.Focus(context), bonusScalings, DamageType.Physical, ref state);
            SkillModules.GainStat(context, Targets.Self(context), StatType.Attack, scaleStat: StatType.MagicalPower, scalePercent: attackGainPercent);
        }
    }
}
