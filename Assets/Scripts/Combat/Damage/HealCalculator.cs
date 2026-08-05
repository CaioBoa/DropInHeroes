using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Calcula a cura BRUTA a partir de uma base e de escalonamentos sobre os stats da fonte (mesmo
    /// padrão da parte de escalonamento do DamageCalculator). O bônus de cura CAUSADA (HealBonus) e a
    /// cura RECEBIDA (HealReceived) NÃO entram aqui — são aplicados pelos módulos
    /// <see cref="StatsModule.CauseHeal"/> (origem) e <see cref="StatsModule.ReceiveHeal"/> (destino).
    /// </summary>
    public static class HealCalculator
    {
        public static float Calculate(StatsModule source, float baseHeal, StatScaling[] scalings)
        {
            float heal = baseHeal;

            if (source != null && scalings != null)
            {
                for (int i = 0; i < scalings.Length; i++)
                    heal += source.GetStat(scalings[i].stat) * scalings[i].multiplier;
            }

            return Mathf.Max(0f, heal);
        }
    }
}
