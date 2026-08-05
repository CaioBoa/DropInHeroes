using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva do Guliver — "Vontade do Herói": enquanto vive, reduz o dano RECEBIDO pelos aliados
    /// (stat DamageReduction) e redireciona parte do dano deles a si (damage-share). O dono não recebe a
    /// redução nem partilha consigo. Definida em código (composição de módulos); o SO só expõe as
    /// intensidades por rank.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Guliver/Passive")]
    public class GuliverPassive : ModularPassive
    {
        [Tooltip("Redução do dano recebido pelos aliados (fração), por rank. Ex.: 0.05 / 0.10 / 0.15.")]
        [SerializeField] private float[] damageReductionByRank = { 0.05f, 0.10f, 0.15f };
        [Tooltip("Fração do dano dos aliados redirecionada ao dono, por rank. Ex.: 0.15 / 0.20 / 0.25.")]
        [SerializeField] private float[] damageShareByRank = { 0.15f, 0.20f, 0.25f };

        protected override void Compose(SkillContext context)
        {
            int rank = context.owner.Rank;
            var allies = Targets.Allies(context, includeSelf: false);

            // DamageReduction é base 100 (valor 15 = −15% de dano recebido): a fração vira pontos.
            SkillModules.StatModifier(Persistent, allies, StatType.DamageReduction,
                RankTiers.ValueFor(damageReductionByRank, rank) * 100f);

            SkillModules.ApplyDamageShare(Persistent, allies, context.owner,
                RankTiers.ValueFor(damageShareByRank, rank));
        }
    }
}
