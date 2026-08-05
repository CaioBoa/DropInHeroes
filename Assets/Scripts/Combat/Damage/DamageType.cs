using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Tipo de dano — define a mitigação no <see cref="DamageCalculator"/>:
    /// Físico → Defesa (com penetração e resistência à penetração) + redução de dano;
    /// Mágico → Defesa Mágica (com penetração/resistência mágicas) + redução de dano;
    /// Verdadeiro → ignora defesa/def. mágica, mas a redução de dano ainda se aplica;
    /// Puro → ignora defesas E redução de dano (toda mitigação do alvo); só restam aspectos positivos
    /// (crítico, bônus de dano, vantagem de tipo).
    /// É independente do escalonamento (qual stat alimenta o dano) — um dano mágico pode escalar de Ataque.
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        True,
        Pure
    }
}
