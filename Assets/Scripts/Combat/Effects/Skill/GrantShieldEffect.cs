using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Concede um ESCUDO (ver <see cref="ShieldModule"/>) aos alvos resolvidos, com valor escalado nos
    /// stats do DONO (mesmo modelo do <see cref="HealEffect"/>). Reutilizável por qualquer skill —
    /// ex.: supremo do Darulito (escudo de Defesa + Defesa Mágica em todos os aliados). Recast com o
    /// mesmo <see cref="shieldId"/> renova o escudo em vez de acumular.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Grant Shield")]
    public class GrantShieldEffect : SkillEffect
    {
        [Header("Escudo")]
        [Tooltip("Id do escudo — reaplicar o mesmo id renova (não acumula).")]
        [SerializeField] private string shieldId = "shield";
        [Tooltip("Valor fixo somado ao escalonamento.")]
        [SerializeField] private float baseAmount = 0f;
        [Tooltip("Escalonamentos sobre os stats do DONO (stat + %).")]
        [SerializeField] private Scaling[] scalings;

        private StatScaling[] cachedScalings;
        private bool cacheBuilt;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            if (targets.Count == 0) return;
            BuildCache();

            // Valor bruto (base + escalonamentos nos stats do DONO), depois o escudo CAUSADO por ele
            // (ShieldBonus). Cada alvo aplica o seu ShieldReceived no ShieldModule.
            float raw = baseAmount;
            var owner = context.ownerStats;
            if (owner != null && cachedScalings != null)
                for (int j = 0; j < cachedScalings.Length; j++)
                    raw += owner.GetStat(cachedScalings[j].stat) * cachedScalings[j].multiplier;
            float caused = owner != null ? owner.CauseShield(raw) : raw;
            if (caused <= 0f) return;

            for (int i = 0; i < targets.Count; i++)
            {
                var stats = targets[i].Stats;
                if (stats == null || stats.IsDead) continue;
                targets[i].GetModule<ShieldModule>()?.AddShield(shieldId, caused);
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
