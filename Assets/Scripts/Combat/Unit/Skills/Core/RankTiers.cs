using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Helpers para valores por rank em passivas. Rank vai de 1..N; índice 0 = rank 1. Rank fora do
    /// intervalo usa o tier mais próximo (clamp). Centraliza a convenção antes duplicada por passiva.
    /// </summary>
    public static class RankTiers
    {
        /// <summary>Índice do tier (0-based) para um rank 1..N, clampado ao tamanho. Retorna -1 se vazio.</summary>
        public static int IndexFor(int rank, int tierCount)
            => tierCount <= 0 ? -1 : Mathf.Clamp(rank - 1, 0, tierCount - 1);

        /// <summary>Valor por rank a partir de um array (rank 1 = índice 0). Array vazio/nulo = fallback.</summary>
        public static float ValueFor(float[] tiersPerRank, int rank, float fallback = 0f)
        {
            if (tiersPerRank == null || tiersPerRank.Length == 0) return fallback;
            return tiersPerRank[IndexFor(rank, tiersPerRank.Length)];
        }
    }
}
