using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DropInHeroes.EditorTools
{

    /// <summary>
    /// Varre a arte de personagem e reporta o que fugiu do formato canônico.
    ///
    /// O postprocessor impõe o IMPORT; este validador confere o que está em DISCO — dimensão do
    /// arquivo, e o formato em que a textura acaba na memória. Uma dimensão fora do canônico não
    /// gera erro no Unity: só desperdiça memória em silêncio, que foi exatamente como o projeto
    /// acumulou 54 dimensões e 44% dos sprites sem compressão.
    /// </summary>
    public static class CharacterArtBudgetValidator
    {
        [MenuItem("Tools/DropInHeroes/Validar arte de personagem")]
        public static void Validate()
        {
            var erros = new List<string>();
            var avisos = new List<string>();
            var dims = new SortedDictionary<string, int>();
            var ppus = new SortedDictionary<float, int>();
            var fmts = new SortedDictionary<string, int>();
            int total = 0, fora = 0;

            string raizAbs = Path.Combine(Application.dataPath, "Art/Characters");
            if (!Directory.Exists(raizAbs)) { Debug.LogWarning("Sem Assets/Art/Characters."); return; }

            foreach (var f in Directory.GetFiles(raizAbs, "*.png", SearchOption.AllDirectories))
            {
                string rel = "Assets" + f.Substring(Application.dataPath.Length).Replace('\\', '/');
                var classe = CharacterArtFormat.Classificar(rel);
                if (classe == CharacterArtFormat.Classe.Fora) { fora++; continue; }
                if (CharacterArtFormat.IsentoPorMarcador(rel)) { fora++; continue; }

                total++;
                var ti = AssetImporter.GetAtPath(rel) as TextureImporter;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
                if (ti == null || tex == null) { erros.Add($"não importou como textura: {rel}"); continue; }

                int canvas = CharacterArtFormat.CanvasDe(classe);
                float ppu = CharacterArtFormat.PpuDe(classe);

                dims[$"{tex.width}x{tex.height}"] = dims.TryGetValue($"{tex.width}x{tex.height}", out var d) ? d + 1 : 1;
                ppus[ti.spritePixelsPerUnit] = ppus.TryGetValue(ti.spritePixelsPerUnit, out var q) ? q + 1 : 1;
                fmts[tex.format.ToString()] = fmts.TryGetValue(tex.format.ToString(), out var g) ? g + 1 : 1;

                if (tex.width != canvas || tex.height != canvas)
                    erros.Add($"dimensão {tex.width}x{tex.height}, esperado {canvas}x{canvas}: {rel}");
                if (!Mathf.Approximately(ti.spritePixelsPerUnit, ppu))
                    erros.Add($"PPU {ti.spritePixelsPerUnit}, esperado {ppu}: {rel}");
                if (tex.format == TextureFormat.RGBA32 || tex.format == TextureFormat.ARGB32)
                    erros.Add($"sem compressão ({tex.format}): {rel}");
                if (tex.width % 4 != 0 || tex.height % 4 != 0)
                    erros.Add($"dimensão não múltipla de 4: {rel}");
            }

            // AnimClipTuning só deve sobreviver para efeito artístico declarado, nunca enquadramento.
            foreach (var g in AssetDatabase.FindAssets("t:CharacterData"))
            {
                var cd = AssetDatabase.LoadAssetAtPath<Data.CharacterData>(AssetDatabase.GUIDToAssetPath(g));
                if (cd != null && cd.animClipTunings != null && cd.animClipTunings.Count > 0)
                    avisos.Add($"{cd.name}: {cd.animClipTunings.Count} AnimClipTuning — confirme que é " +
                               "efeito artístico, não correção de enquadramento");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Validação de arte de personagem — {total} sprites no escopo ({fora} fora do escopo).");
            sb.AppendLine($"\nDimensões: {Resumo(dims)}");
            sb.AppendLine($"PPU: {Resumo(ppus)}");
            sb.AppendLine($"Formato em VRAM: {Resumo(fmts)}");

            if (avisos.Count > 0)
            {
                sb.AppendLine($"\nAVISOS ({avisos.Count}):");
                foreach (var a in avisos) sb.AppendLine("  " + a);
            }
            if (erros.Count > 0)
            {
                sb.AppendLine($"\nERROS ({erros.Count}):");
                foreach (var e in erros.GetRange(0, Mathf.Min(40, erros.Count))) sb.AppendLine("  " + e);
                if (erros.Count > 40) sb.AppendLine($"  ... e mais {erros.Count - 40}");
                sb.AppendLine("\nRESULTADO: fora do formato canônico. Reimportar costuma bastar " +
                              "(o postprocessor corrige o import); dimensão errada exige re-normalizar o arquivo.");
                Debug.LogError(sb.ToString());
            }
            else
            {
                sb.AppendLine("\nRESULTADO: formato canônico íntegro.");
                Debug.Log(sb.ToString());
            }
        }

        private static string Resumo<T>(SortedDictionary<T, int> d)
        {
            var partes = new List<string>();
            foreach (var kv in d) partes.Add($"{kv.Key}={kv.Value}");
            return string.Join(", ", partes);
        }
    }
}
