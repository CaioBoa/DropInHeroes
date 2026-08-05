using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Cura os alvos resolvidos, via <see cref="HealCalculator"/>. Cobre tanto cura fixa/escalada
    /// (Guliver: aliados por % da Vida Máx) quanto cura proporcional ao nº de alvos atingidos pelo
    /// efeito anterior na cadeia (Rikurby: cura a si por inimigo atingido) — ver
    /// <see cref="scaleByPreviousHits"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Heal")]
    public class HealEffect : SkillEffect
    {
        [Header("Cura")]
        [Tooltip("Cura fixa somada ao escalonamento.")]
        [SerializeField] private float baseHeal = 0f;
        [Tooltip("Escalonamentos sobre os stats da fonte (o autor escolhe stat + %).")]
        [SerializeField] private Scaling[] scalings;
        [Tooltip("Se marcado, multiplica a cura pelo nº de alvos atingidos pelo efeito ANTERIOR da cadeia (ex.: curar por inimigo atingido).")]
        [SerializeField] private bool scaleByPreviousHits = false;

        private StatScaling[] cachedScalings;
        private bool cacheBuilt;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            if (targets.Count == 0) return;
            BuildCache();

            float raw = HealCalculator.Calculate(context.ownerStats, baseHeal, cachedScalings);
            if (scaleByPreviousHits) raw *= state.previousTargetCount;
            // Cura CAUSADA pela fonte (HealBonus) — calculada uma vez; cada alvo aplica sua HealReceived.
            float caused = context.ownerStats != null ? context.ownerStats.CauseHeal(raw) : raw;
            if (caused <= 0f) return;

            for (int i = 0; i < targets.Count; i++)
            {
                var targetStats = targets[i].GetModule<StatsModule>();
                if (targetStats == null || targetStats.IsDead) continue;
                targetStats.ReceiveHeal(caused);
            }
        }

        private void BuildCache()
        {
            if (cacheBuilt) return;
            int n = scalings != null ? scalings.Length : 0;
            cachedScalings = new StatScaling[n];
            for (int i = 0; i < n; i++) cachedScalings[i] = scalings[i].ToStatScaling();
            cacheBuilt = true;
        }
    }
}
