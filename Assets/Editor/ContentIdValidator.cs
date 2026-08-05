using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using DropInHeroes.Data;

namespace DropInHeroes.EditorTools
{

    /// <summary>
    /// Valida o contrato de ID de conteúdo (ver <see cref="IGameData"/>) em todos os assets do projeto.
    ///
    /// Existe porque o id é gerado do nome do asset em <c>OnValidate</c> enquanto está vazio: é
    /// estável por convenção, não por imposição. Um id vazio ou duplicado não gera erro de
    /// compilação — some silenciosamente na resolução do catálogo.
    /// </summary>
    public static class ContentIdValidator
    {
        private const string MenuPath = "Tools/DropInHeroes/Validar IDs de conteúdo";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            var assets = LoadAll();
            var byId = new Dictionary<string, List<Object>>();
            var vazios = new List<Object>();
            var divergentes = new List<(Object asset, string id, string esperado)>();

            foreach (var (obj, data) in assets)
            {
                string id = data.ID;
                if (string.IsNullOrWhiteSpace(id)) { vazios.Add(obj); continue; }

                string chave = data.Category + "/" + id;
                if (!byId.TryGetValue(chave, out var lista))
                {
                    lista = new List<Object>();
                    byId[chave] = lista;
                }
                lista.Add(obj);

                string esperado = obj.name.ToLower().Replace(" ", "_");
                if (id != esperado) divergentes.Add((obj, id, esperado));
            }

            int erros = 0;
            var sb = new StringBuilder();
            sb.AppendLine($"Validação de IDs — {assets.Count} assets de conteúdo verificados.");

            if (vazios.Count > 0)
            {
                erros += vazios.Count;
                sb.AppendLine($"\nERRO — {vazios.Count} asset(s) com ID vazio:");
                foreach (var o in vazios) sb.AppendLine("  " + AssetDatabase.GetAssetPath(o));
            }

            foreach (var kv in byId)
            {
                if (kv.Value.Count < 2) continue;
                erros++;
                sb.AppendLine($"\nERRO — ID duplicado '{kv.Key}' em {kv.Value.Count} assets:");
                foreach (var o in kv.Value) sb.AppendLine("  " + AssetDatabase.GetAssetPath(o));
            }

            // Divergência NÃO é erro: renomear o asset mantendo o id é o comportamento correto
            // do contrato. É listada para que uma divergência acidental seja percebida.
            if (divergentes.Count > 0)
            {
                sb.AppendLine($"\nINFO — {divergentes.Count} asset(s) com ID diferente do nome (esperado após rename):");
                foreach (var (o, id, esperado) in divergentes)
                    sb.AppendLine($"  '{id}' vs '{esperado}'  —  {AssetDatabase.GetAssetPath(o)}");
            }

            sb.AppendLine(erros == 0
                ? "\nRESULTADO: contrato íntegro, nenhum erro."
                : $"\nRESULTADO: {erros} problema(s) — corrigir antes de commitar.");

            if (erros > 0) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());
        }

        private static List<(Object, IGameData)> LoadAll()
        {
            var result = new List<(Object, IGameData)>();
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj is IGameData data) result.Add((obj, data));
            }
            return result;
        }
    }
}
