using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{
    /// <summary>
    /// Escalonamento data-driven: o autor escolhe o stat e a porcentagem (100 = 100% do stat).
    /// O texto de habilidade lê o <see cref="stat"/> (para a cor) e o <see cref="percent"/> (o valor).
    /// Converte para <see cref="StatScaling"/> (usado pelo DamageCalculator) via <see cref="Multiplier"/>.
    /// </summary>
    [System.Serializable]
    public struct Scaling
    {
        public StatType stat;
        public float percent; // 100 = 100% do stat

        public float Multiplier => percent * 0.01f;
        public StatScaling ToStatScaling() => new StatScaling(stat, Multiplier);
    }
}
