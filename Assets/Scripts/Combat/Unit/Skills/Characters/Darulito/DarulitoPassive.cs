using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva do Darulito — aura: enquanto vivo, concede aos aliados (incl. si) resistência a penetração e a
    /// penetração mágica (por rank), e converte parte do próprio Poder Mágico em Regeneração de Energia.
    /// Composição de módulos persistentes (auto-revertidos ao morrer/fim). O SO só expõe os números por rank.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Darulito/Passive")]
    public class DarulitoPassive : ModularPassive
    {
        [Header("Resistências aos aliados (por rank)")]
        [Tooltip("Resistência a Penetração, por rank (índice 0 = rank 1). Ex.: 30/40/50.")]
        [SerializeField] private float[] penResistanceByRank = { 30f, 40f, 50f };
        [Tooltip("Resistência a Penetração Mágica, por rank. Ex.: 30/40/50.")]
        [SerializeField] private float[] magicPenResistanceByRank = { 30f, 40f, 50f };

        [Header("Regeneração de Energia do dono")]
        [Tooltip("Fração do Poder Mágico convertida em Regeneração de Energia (0.01 = 1%).")]
        [SerializeField] private float energyRegenPercentOfMagic = 0.01f;

        protected override void Compose(SkillContext context)
        {
            int rank = context.owner.Rank;
            var allies = Targets.Allies(context, includeSelf: true);

            SkillModules.StatModifier(Persistent, allies, StatType.PenetrationResistance, RankTiers.ValueFor(penResistanceByRank, rank));
            SkillModules.StatModifier(Persistent, allies, StatType.MagicalPenetrationResistance, RankTiers.ValueFor(magicPenResistanceByRank, rank));
            SkillModules.StatModifierPercentOfStat(Persistent, Targets.Self(context), StatType.EnergyRegeneration, StatType.MagicalPower, energyRegenPercentOfMagic, context.owner);
        }
    }
}
