using System.IO;
using UnityEngine;

namespace DropInHeroes.EditorTools
{

    /// <summary>
    /// O formato canônico da arte de personagem — fonte única para o postprocessor e o validador.
    ///
    /// Os números não são arbitrários: saíram da medição do acervo no F0.11
    /// (ver Docs/plans/F0.11-peso-de-personagem.md).
    ///   • PPU 90 — escolhido por comparação visual sob o zoom máximo do combate (1,67×)
    ///   • Canvas 448 do corpo — o conteúdo se estende 208 px acima do pivô (Darulito/Victory)
    ///     e 170 px abaixo (Pedro/DeathIdle); 378 px somados, mais folga
    ///   • Canvas 640 do VFX — largura é o limite: 288 px de cada lado (Rikurby/SupremeEffect)
    ///   • Pivô por classe, posicionado para que a folga sobre e desça igualmente
    ///
    /// Mudar qualquer valor aqui exige re-normalizar o acervo. Não ajuste para acomodar
    /// um asset novo que não cabe — o asset é que deve ser reenquadrado.
    /// </summary>
    public static class CharacterArtFormat
    {
        public const string Raiz = "Assets/Art/Characters/";

        /// <summary>Marcador de exclusão: um arquivo com este nome na pasta isenta a pasta inteira.</summary>
        public const string MarcadorManual = "__manual__";

        public enum Classe { Fora, Corpo, Vfx, Icone }

        public const float PpuMundo = 90f;
        public const float PpuIcone = 100f;

        public const int CanvasCorpo = 448;
        public const int CanvasVfx = 640;
        public const int CanvasIcone = 256;

        public static readonly Vector2 PivoCorpo = new Vector2(0.5f, 205f / 448f);
        public static readonly Vector2 PivoVfx = new Vector2(0.5f, 327f / 640f);
        public static readonly Vector2 PivoIcone = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// Classifica um asset pelo caminho. Arte solta na raiz do personagem (splash art,
        /// design) fica <see cref="Classe.Fora"/>: é autorada à mão e não segue o formato.
        /// </summary>
        public static Classe Classificar(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(Raiz)) return Classe.Fora;

            // Assets/Art/Characters/<Nome>/<Pasta>/<arquivo>  →  6 segmentos
            string[] p = assetPath.Split('/');
            if (p.Length < 6) return Classe.Fora;

            string pasta = p[4].ToLowerInvariant();
            if (pasta == "skills") return Classe.Icone;
            if (pasta.StartsWith("vfx") || pasta.Contains("effect") || pasta.Contains("projectile"))
                return Classe.Vfx;
            return Classe.Corpo;
        }

        /// <summary>A pasta do asset foi marcada para autoria manual?</summary>
        public static bool IsentoPorMarcador(string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath);
            return !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, MarcadorManual));
        }

        public static int CanvasDe(Classe c) =>
            c == Classe.Icone ? CanvasIcone : c == Classe.Vfx ? CanvasVfx : CanvasCorpo;

        public static float PpuDe(Classe c) => c == Classe.Icone ? PpuIcone : PpuMundo;

        public static Vector2 PivoDe(Classe c) =>
            c == Classe.Icone ? PivoIcone : c == Classe.Vfx ? PivoVfx : PivoCorpo;
    }
}
