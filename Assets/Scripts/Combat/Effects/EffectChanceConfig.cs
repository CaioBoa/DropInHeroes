using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Pesos da fórmula de chance de aplicação de efeitos.
    /// Ponto único para ajustar o balanceamento de efetividade × resistência.
    /// (Efetividade e resistência são frações: 0.15 = 15%.)
    /// </summary>
    public static class EffectChanceConfig
    {
        /// <summary>
        /// Ganho de chance por ponto de efetividade líquida (efetividade - resistência),
        /// quando positiva. Ex.: 0.5 → precisa de +30% líquidos para chegar ao teto de +15%.
        /// </summary>
        public const float EffectivenessGain = 0.5f;

        /// <summary>
        /// Penalidade por ponto de resistência líquida (resistência - efetividade), quando positiva.
        /// Maior que EffectivenessGain → resistência tem mais peso que a efetividade.
        /// </summary>
        public const float ResistanceWeight = 1.0f;

        /// <summary>Bônus máximo de chance que a efetividade pode conceder (+15%).</summary>
        public const float MaxEffectivenessBonus = 0.15f;
    }
}
