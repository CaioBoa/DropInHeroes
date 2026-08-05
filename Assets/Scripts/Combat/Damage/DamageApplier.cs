using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Caminho CANÔNICO de aplicação de dano direto de skills: Calculate → ApplyDamage → notificações
    /// (dano causado; ataque recebido — exceto riders on-hit, ver <see cref="SkillContext.isOnHit"/>)
    /// → roubo de vida. Único lugar onde essas regras vivem — usado pelo módulo de dano das skills
    /// (<see cref="SkillModules"/>) e pelo <see cref="DealDamageEffect"/>.
    /// </summary>
    public static class DamageApplier
    {
        public static void Apply(in SkillContext context, List<UnitController> targets,
                                 float baseDamage, StatScaling[] scalings, in DamageConfig config)
        {
            float lifesteal = context.ownerStats?.Lifesteal ?? 0f;
            float healBack = 0f;

            for (int i = 0; i < targets.Count; i++)
            {
                var targetStats = targets[i].Stats;
                if (targetStats == null || targetStats.IsDead) continue;

                var result = DamageCalculator.Calculate(context.ownerStats, targetStats, baseDamage, scalings, config);
                targetStats.ApplyDamage(result);
                context.skills?.NotifyDamageDealt(result.finalDamage);

                // Crítico da FONTE (só golpes que podem critar produzem isCritical) — hook p/ passivas de crit.
                if (result.isCritical)
                    context.skills?.NotifyCriticalHit(targets[i]);

                // Um golpe = um "ataque recebido". Riders on-hit são caroneiros do golpe — não contam.
                if (!context.isOnHit)
                    targets[i].GetModule<SkillsModule>()?.NotifyAttackReceived(context.owner);

                // Roubo de vida: cura a fonte por % do dano causado a inimigos (dano direto; DoT não conta).
                if (lifesteal > 0f && context.owner != null && targets[i].GetTeam() != context.owner.GetTeam())
                    healBack += result.finalDamage * (lifesteal / 100f); // Lifesteal em base 100 (100 = 100%)
            }

            // Lifesteal é uma cura CAUSADA pela fonte nela mesma: passa pelos módulos de cura, então
            // aplica HealBonus (causa) e HealReceived (recebe) da própria unidade.
            if (healBack > 0f && context.ownerStats != null)
                context.ownerStats.ReceiveHeal(context.ownerStats.CauseHeal(healBack));
        }
    }
}
