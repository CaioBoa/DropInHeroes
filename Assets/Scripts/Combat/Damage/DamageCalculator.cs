using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public static class DamageCalculator
    {
        public static DamageResult Calculate(
            StatsModule source,
            StatsModule target,
            float baseDamage,
            StatScaling[] scalings,
            DamageConfig config)
        {
            float raw = baseDamage;

            if (source != null && scalings != null)
            {
                for (int i = 0; i < scalings.Length; i++)
                    raw += source.GetStat(scalings[i].stat) * scalings[i].multiplier;
            }

            bool isCrit = false;
            if (!config.cannotCrit && source != null)
            {
                // Stats percentuais em base 100 (100 = 100%): dividir por 100 ao usar como probabilidade/fator.
                isCrit = config.alwaysCrit || Random.value < source.CritRate / 100f;
                if (isCrit)
                {
                    // O alvo mitiga SÓ o bônus do crítico (parte acima do não-crítico), com piso em 0:
                    // um acerto crítico nunca causa menos que um não-crítico, por maior que seja a resistência.
                    float critResist = (target?.GetStat(StatType.CritDamageResistance) ?? 0f) / 100f;
                    float effectiveCritBonus = Mathf.Max(0f, (source.CritDamage / 100f - 1f) * (1f - critResist));
                    raw *= 1f + effectiveCritBonus;
                }
            }

            // Aspecto positivo da fonte — vale para TODOS os tipos, inclusive Puro. (base 100 → fração)
            float damageBonus = (source?.GetStat(StatType.DamageBonus) ?? 0f) / 100f;

            // Mitigação do alvo. Cada tipo ignora uma fatia crescente:
            //   Físico/Mágico → defesa (com penetração e resistência à penetração) + redução de dano;
            //   Verdadeiro    → ignora defesa/def. mágica, mas a redução de dano ainda vale;
            //   Puro          → ignora defesas E redução — só restam os aspectos positivos.
            float defenseFactor = 1f;
            if (config.damageType == DamageType.Physical || config.damageType == DamageType.Magical)
            {
                bool magical = config.damageType == DamageType.Magical;
                float defense = target?.GetStat(magical ? StatType.MagicalDefense : StatType.Defense) ?? 0f;
                float penetration = (source?.GetStat(magical ? StatType.MagicalPenetration : StatType.Penetration) ?? 0f) / 100f;
                float penetrationResist = (target?.GetStat(magical ? StatType.MagicalPenetrationResistance : StatType.PenetrationResistance) ?? 0f) / 100f;

                // Resistência reduz a penetração de forma multiplicativa (nunca a zera por completo).
                float effectivePenetration = penetration * (1f - penetrationResist);
                defense = Mathf.Max(0f, defense * (1f - effectivePenetration));
                // Diminishing returns: defesa=0 → 1.0 (dano integral); defesa=100 → 0.5; defesa→∞ → ~0.
                defenseFactor = 100f / (defense + 100f);
            }

            float damageReduction = config.damageType == DamageType.Pure
                ? 0f
                : (target?.GetStat(StatType.DamageReduction) ?? 0f) / 100f;

            float final_ = Mathf.Max(0f, raw * defenseFactor * (1f + damageBonus - damageReduction));

            if (!config.ignoreTypeAdvantage && source != null && target != null)
            {
                float typeMult = TypeAdvantage.GetMultiplier(source.GetTypes(), target.GetTypes());
                final_ *= typeMult;
            }

            // Regras universais de combate (passivas, itens) — condicionais à fonte/alvo via DamageContext.
            var dmgCtx = new DamageContext(source, target, source?.Tags ?? UnitTag.None, target?.Tags ?? UnitTag.None);
            if (source != null) final_ *= source.GetOutgoingDamageMultiplier(dmgCtx);
            if (target != null) final_ *= target.GetIncomingDamageMultiplier(dmgCtx);

            return new DamageResult
            {
                rawDamage = raw,
                finalDamage = final_,
                isCritical = isCrit,
                isExtinction = config.extinction,
                // Verdadeiro e Puro ignoram a mitigação do alvo — não são redirecionados por damage share.
                ignoreDamageShare = config.ignoreDamageShare
                    || config.damageType == DamageType.True
                    || config.damageType == DamageType.Pure
            };
        }
    }
}
