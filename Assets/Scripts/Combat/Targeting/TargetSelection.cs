using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Primitivas compartilhadas de seleção de alvo: checagens (vivo/focável) e ordenação/seleção
    /// por critério. Fonte única usada por <see cref="TargetQuery"/> e <see cref="FocusModule"/>,
    /// para que "menor vida", "mais próximo" e "focável" signifiquem o mesmo em skills e no foco.
    /// Opera sobre buffers fornecidos pelo chamador — sem alocação por uso.
    /// </summary>
    public static class TargetSelection
    {
        public static bool IsAlive(UnitController unit)
        {
            if (unit == null) return false;
            var stats = unit.Stats; // cacheado no UnitController — evita lookup de dicionário por candidato/frame
            return stats != null && !stats.IsDead;
        }

        public static bool IsTargetableUnit(UnitController unit) => IsAlive(unit) && unit.IsTargetable;

        /// <summary>
        /// Reduz 'candidates' (in place) à seleção final por critério e count
        /// (0 = todos; 1 = único; N = top-N). 'origin' é a posição de referência para distância.
        /// </summary>
        public static void Reduce(List<UnitController> candidates, TargetCriterion criterion, Vector2 origin, int count)
        {
            int n = candidates.Count;
            if (n <= 1) return;

            if (criterion == TargetCriterion.Random)
            {
                int kr = count <= 0 ? n : Mathf.Min(count, n);
                for (int i = 0; i < kr; i++)
                {
                    int r = Random.Range(i, n);
                    (candidates[i], candidates[r]) = (candidates[r], candidates[i]);
                }
                if (count > 0 && kr < n) candidates.RemoveRange(kr, n - kr);
                return;
            }

            if (criterion == TargetCriterion.All)
            {
                if (count > 0 && count < n) candidates.RemoveRange(count, n - count);
                return;
            }

            // Selection sort parcial: traz os 'k' melhores para a frente (listas de combate são pequenas).
            int k = count <= 0 ? n : Mathf.Min(count, n);
            for (int i = 0; i < k; i++)
            {
                int best = i;
                for (int j = i + 1; j < n; j++)
                    if (Better(candidates[j], candidates[best], criterion, origin)) best = j;
                if (best != i)
                    (candidates[i], candidates[best]) = (candidates[best], candidates[i]);
            }
            if (count > 0 && k < n) candidates.RemoveRange(k, n - k);
        }

        private static bool Better(UnitController a, UnitController b, TargetCriterion criterion, Vector2 origin)
        {
            switch (criterion)
            {
                case TargetCriterion.Closest: return SqrDist(a, origin) < SqrDist(b, origin);
                case TargetCriterion.Farthest: return SqrDist(a, origin) > SqrDist(b, origin);
                case TargetCriterion.LowestCurrentHealth: return CurrentHealth(a) < CurrentHealth(b);
                case TargetCriterion.HighestCurrentHealth: return CurrentHealth(a) > CurrentHealth(b);
                case TargetCriterion.LowestMaxHealth: return MaxHealth(a) < MaxHealth(b);
                case TargetCriterion.HighestMaxHealth: return MaxHealth(a) > MaxHealth(b);
                default: return false;
            }
        }

        private static float SqrDist(UnitController u, Vector2 origin)
            => ((Vector2)u.transform.position - origin).sqrMagnitude;

        private static float CurrentHealth(UnitController u)
            => u.Stats?.CurrentHealth ?? 0f;

        private static float MaxHealth(UnitController u)
            => u.Stats?.MaxHealth ?? 0f;
    }
}
