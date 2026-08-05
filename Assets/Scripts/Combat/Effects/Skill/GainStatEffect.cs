using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Concede um atributo aos alvos (normalmente Self): soma um valor permanente no combate, que
    /// acumula a cada aplicação e reseta ao voltar pro pool (via <see cref="StatsModule.ModifyStat"/>).
    /// Cobre "ganha Ataque sempre que ataca" como efeito de contato. O ganho pode ser fixo (<c>amount</c>)
    /// e/ou ESCALAR num stat-fonte do conjurador (ex.: "ganha 5% do Poder Mágico como Ataque por acerto").
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Gain Stat")]
    public class GainStatEffect : SkillEffect
    {
        [SerializeField] private StatType stat = StatType.Attack;
        [Tooltip("Quantidade FIXA somada (permanente no combate) a cada aplicação.")]
        [SerializeField] private float amount = 0f;

        [Header("Escalonamento opcional (ganho = amount + fonte × scalePercent%)")]
        [Tooltip("Stat-fonte do conjurador que alimenta o escalonamento.")]
        [SerializeField] private StatType scaleStat = StatType.MagicalPower;
        [Tooltip("Percentual do stat-fonte somado por aplicação (100 = 100% do stat). 0 = sem escalonamento.")]
        [SerializeField] private float scalePercent = 0f;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            float gain = amount;
            if (scalePercent != 0f)
                gain += (context.ownerStats?.GetStat(scaleStat) ?? 0f) * scalePercent / 100f;
            if (gain == 0f) return;

            for (int i = 0; i < targets.Count; i++)
                targets[i].GetModule<StatsModule>()?.ModifyStat(stat, gain);
        }
    }
}
