using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Calcula a chance final de aplicar um efeito a partir de uma chance base, ajustando por
    /// efetividade (lançador) × resistência (alvo). Pesos em <see cref="EffectChanceConfig"/>.
    ///
    /// Regras:
    /// - efetividade > resistência: bônus de até +15% (precisa de margem para o teto).
    /// - resistência > efetividade: penalidade sem teto (pode zerar a chance) — pesa mais.
    /// - igualadas: mantém a chance base.
    /// </summary>
    public static class EffectChanceCalculator
    {
        /// <summary>Retorna a chance final no intervalo [0, baseChance + 15%].</summary>
        public static float Calculate(float baseChance, float effectiveness, float resistance)
        {
            // Effectiveness/Resistance em base 100 (100 = 100%): normaliza para fração (a chance base é 0..1).
            float diff = (effectiveness - resistance) / 100f;

            float adjustment = diff >= 0f
                ? Mathf.Min(EffectChanceConfig.MaxEffectivenessBonus, diff * EffectChanceConfig.EffectivenessGain)
                : diff * EffectChanceConfig.ResistanceWeight; // negativo: penalidade

            float chance = baseChance + adjustment;
            return Mathf.Clamp(chance, 0f, baseChance + EffectChanceConfig.MaxEffectivenessBonus);
        }

        /// <summary>Calcula a chance e faz o sorteio. true = efeito aplicado.</summary>
        public static bool Roll(float baseChance, float effectiveness, float resistance)
        {
            return Random.value < Calculate(baseChance, effectiveness, resistance);
        }
    }
}
