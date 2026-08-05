using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Causa dano aos alvos resolvidos, via <see cref="DamageCalculator"/>. Substitui o cálculo de
    /// dano antes copiado em cada ataque básico e em cada dano de área dos supremos — o autor só
    /// escolhe os escalonamentos (<see cref="Scaling"/>) e as flags de dano.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Deal Damage")]
    public class DealDamageEffect : SkillEffect
    {
        [Header("Dano")]
        [Tooltip("Dano fixo somado ao escalonamento.")]
        [SerializeField] private float baseDamage = 0f;
        [Tooltip("Escalonamentos sobre os stats da fonte (o autor escolhe stat + %).")]
        [SerializeField] private Scaling[] scalings;

        [Header("Configuração")]
        [Tooltip("Físico → Defesa; Mágico → Defesa Mágica; Verdadeiro → ignora defesas (redução ainda vale); Puro → ignora toda mitigação do alvo.")]
        [SerializeField] private DamageType damageType = DamageType.Physical;
        [SerializeField] private bool cannotCrit = false;
        [SerializeField] private bool ignoreTypeAdvantage = false;

        // Derivados imutáveis dos campos serializados (iguais para toda unidade) — construídos uma vez.
        private StatScaling[] cachedScalings;
        private DamageConfig config;
        private bool cacheBuilt;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            if (targets.Count == 0) return;
            BuildCache();

            // Crítico só quando o run permite E o efeito não o proíbe. Struct = cópia barata.
            DamageConfig cfg = config;
            cfg.cannotCrit = config.cannotCrit || !context.allowCrit;

            // Aplicação canônica (notificações + lifesteal) — ver DamageApplier.
            DamageApplier.Apply(context, targets, baseDamage, cachedScalings, cfg);
        }

        private void BuildCache()
        {
            if (cacheBuilt) return;
            int n = scalings != null ? scalings.Length : 0;
            cachedScalings = new StatScaling[n];
            for (int i = 0; i < n; i++) cachedScalings[i] = scalings[i].ToStatScaling();
            config = new DamageConfig { damageType = damageType, cannotCrit = cannotCrit, ignoreTypeAdvantage = ignoreTypeAdvantage };
            cacheBuilt = true;
        }
    }
}
