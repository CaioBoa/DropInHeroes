using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Seleção em FAIXA RETA (onda direcional): dada uma origem e uma lista de candidatos, escolhe a
    /// direção — entre as direções origem→candidato — que cobre o MAIOR número de candidatos dentro de
    /// uma faixa de largura/comprimento dados, e descarta os de fora. Usado pelo <see cref="TargetQuery"/>
    /// com <see cref="RadiusMode.Line"/> (ex.: a onda do supremo do Hami).
    /// </summary>
    public static class LineTargeting
    {
        public static void KeepBestLine(Vector2 origin, List<UnitController> candidates, float width, float length)
        {
            int n = candidates.Count;
            if (n <= 1 || width <= 0f || length <= 0f) return;

            float halfWidth = width * 0.5f;

            Vector2 bestDir = default;
            int bestCount = -1;
            for (int i = 0; i < n; i++)
            {
                Vector2 to = (Vector2)candidates[i].transform.position - origin;
                if (to.sqrMagnitude < 0.0001f) continue;
                Vector2 dir = to.normalized;

                int count = 0;
                for (int j = 0; j < n; j++)
                    if (InBand(origin, dir, candidates[j].transform.position, halfWidth, length)) count++;

                if (count > bestCount) { bestCount = count; bestDir = dir; }
            }

            if (bestCount <= 0) { candidates.Clear(); return; }

            for (int i = candidates.Count - 1; i >= 0; i--)
                if (!InBand(origin, bestDir, candidates[i].transform.position, halfWidth, length))
                    candidates.RemoveAt(i);
        }

        private static bool InBand(Vector2 origin, Vector2 dir, Vector2 point, float halfWidth, float length)
        {
            Vector2 rel = (Vector2)point - origin;
            float along = Vector2.Dot(rel, dir);
            if (along < 0f || along > length) return false;
            Vector2 onAxis = origin + dir * along;
            return (((Vector2)point) - onAxis).sqrMagnitude <= halfWidth * halfWidth;
        }
    }
}
