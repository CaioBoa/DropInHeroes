using UnityEngine;

namespace DropInHeroes.EditorTools
{

    /// <summary>
    /// Simetria radial de 5 setores (pentagrama) para o editor da árvore de stats.
    /// Cada nó simétrico pertence a um GRUPO e vive em um SETOR (0..4); IDs no formato "g{grupo}s{setor}".
    /// A cópia do setor s é a cópia canônica (setor 0) rotacionada +72°·s. O setor 0 é centrado no topo (90°).
    /// O nó central ("center") é invariante (sem grupo/setor). Nós soltos (modo livre) usam o prefixo "free".
    /// </summary>
    public static class StatTreeSymmetry
    {
        public const int Sectors = 5;
        public const float SectorDeg = 360f / Sectors; // 72
        public const string CenterId = "center";
        public const string FreePrefix = "free";

        public static string GroupId(int group, int sector) => "g" + group + "s" + sector;

        public static bool TryParse(string id, out int group, out int sector)
        {
            group = 0; sector = 0;
            if (string.IsNullOrEmpty(id) || id[0] != 'g') return false;
            int si = id.IndexOf('s', 1);
            if (si <= 1 || si >= id.Length - 1) return false;
            return int.TryParse(id.Substring(1, si - 1), out group)
                && int.TryParse(id.Substring(si + 1), out sector);
        }

        public static Vector2 Rotate(Vector2 p, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        // Setor (0..4) em que uma posição cai. Setor 0 centrado no topo (ângulo 90°), crescendo no sentido +72°.
        public static int SectorOf(Vector2 p)
        {
            if (p.sqrMagnitude < 1e-6f) return 0;
            float ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;   // 90 = topo
            float rel = Mathf.Repeat((ang - 90f) / SectorDeg, Sectors);
            return ((int)Mathf.Round(rel)) % Sectors;
        }

        /// <summary>Posição canônica (setor 0) de uma cópia que está no setor s.</summary>
        public static Vector2 Canonical(Vector2 posInSector, int sector) => Rotate(posInSector, -SectorDeg * sector);
    }
}
