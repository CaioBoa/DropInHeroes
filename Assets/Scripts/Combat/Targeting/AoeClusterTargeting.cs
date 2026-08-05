using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Escolhe a POSIÇÃO de um disco (AoE circular) de raio fixo que cobre o MAIOR número de pontos
    /// (ex.: saltar para o centro que atinge mais inimigos). Análogo circular do
    /// <see cref="LineTargeting.KeepBestLine"/> (que faz o mesmo para faixas retas).
    ///
    /// O centro ótimo de um disco de raio r que cobre o máximo de pontos ou está SOBRE um ponto, ou tem
    /// dois pontos exatamente na borda — então basta testar cada ponto e os dois centros de círculo-r
    /// que passam por cada par próximo, contando a cobertura de cada candidato. Exato e barato para os
    /// poucos alvos por time (O(n³), n pequeno).
    /// </summary>
    public static class AoeClusterTargeting
    {
        private const float Epsilon = 1e-3f; // tolerância de borda p/ casar com o teste do dano em área

        public static Vector2 BestCoveragePoint(List<Vector2> points, float radius, Vector2 fallback)
        {
            int n = points.Count;
            if (n == 0) return fallback;
            if (n == 1) return points[0];
            if (radius <= 0f) return points[0];

            float r2 = radius * radius;
            float coverR2 = r2 + Epsilon;

            Vector2 best = points[0];
            int bestCount = 0;

            // Candidatos: cada ponto como centro.
            for (int i = 0; i < n; i++)
            {
                int c = CountWithin(points, points[i], coverR2);
                if (c > bestCount) { bestCount = c; best = points[i]; }
            }

            // Candidatos: os dois centros de círculo-r por cada par de pontos a até 2r de distância.
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    Vector2 d = points[j] - points[i];
                    float dist2 = d.sqrMagnitude;
                    if (dist2 < 1e-6f || dist2 > 4f * r2) continue;

                    float dist = Mathf.Sqrt(dist2);
                    Vector2 mid = (points[i] + points[j]) * 0.5f;
                    float h = Mathf.Sqrt(Mathf.Max(0f, r2 - dist2 * 0.25f));
                    Vector2 perp = new Vector2(-d.y, d.x) / dist;

                    Vector2 c1 = mid + perp * h;
                    int n1 = CountWithin(points, c1, coverR2);
                    if (n1 > bestCount) { bestCount = n1; best = c1; }

                    Vector2 c2 = mid - perp * h;
                    int n2 = CountWithin(points, c2, coverR2);
                    if (n2 > bestCount) { bestCount = n2; best = c2; }
                }

            return best;
        }

        private static int CountWithin(List<Vector2> points, Vector2 center, float coverR2)
        {
            int count = 0;
            for (int k = 0; k < points.Count; k++)
                if ((points[k] - center).sqrMagnitude <= coverR2) count++;
            return count;
        }
    }
}
