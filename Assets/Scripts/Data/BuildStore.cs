using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DropInHeroes.Data
{

    /// <summary>
    /// Persistência das builds custom por personagem em JSON no persistentDataPath. Cada personagem tem
    /// ATÉ 3 slots custom, criados sob demanda (não pré-alocados) — só existem builds que o jogador
    /// realmente criou. A build default vive no <see cref="CharacterData"/> (asset). Cache com write-through.
    /// </summary>
    public static class BuildStore
    {
        public const int MaxCustom = 3;

        private static readonly Dictionary<string, CharacterBuildSet> cache = new Dictionary<string, CharacterBuildSet>();

        public static IReadOnlyList<CharacterBuild> GetCustomBuilds(string characterId) => Load(characterId).slots;

        public static bool CanCreate(string characterId) => Load(characterId).slots.Count < MaxCustom;

        /// <summary>Cria e persiste uma nova build custom vazia (null se já houver 3).</summary>
        public static CharacterBuild CreateCustomBuild(string characterId)
        {
            CharacterBuildSet set = Load(characterId);
            if (set.slots.Count >= MaxCustom) return null;
            var b = new CharacterBuild { label = "Nova Build " + (set.slots.Count + 1) };
            set.slots.Add(b);
            Save(characterId, set);
            return b;
        }

        public static void DeleteCustomBuild(string characterId, CharacterBuild build)
        {
            CharacterBuildSet set = Load(characterId);
            if (set.slots.Remove(build)) Save(characterId, set);
        }

        /// <summary>Persiste o estado atual das builds custom (chame após editar uma build em memória).</summary>
        public static void Persist(string characterId)
        {
            if (cache.TryGetValue(characterId, out CharacterBuildSet set)) Save(characterId, set);
        }

        private static CharacterBuildSet Load(string characterId)
        {
            if (cache.TryGetValue(characterId, out CharacterBuildSet cached)) return cached;

            var set = new CharacterBuildSet();
            try
            {
                string path = PathFor(characterId);
                if (File.Exists(path)) JsonUtility.FromJsonOverwrite(File.ReadAllText(path), set);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BuildStore] Falha ao ler builds de '{characterId}': {e.Message}");
            }

            if (set.slots == null) set.slots = new List<CharacterBuild>();
            if (set.slots.Count > MaxCustom) set.slots.RemoveRange(MaxCustom, set.slots.Count - MaxCustom);
            cache[characterId] = set;
            return set;
        }

        private static void Save(string characterId, CharacterBuildSet set)
        {
            cache[characterId] = set;
            try
            {
                string dir = BuildsDir();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(PathFor(characterId), JsonUtility.ToJson(set, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildStore] Falha ao salvar builds de '{characterId}': {e.Message}");
            }
        }

        private static string BuildsDir() => Path.Combine(Application.persistentDataPath, "builds");
        private static string PathFor(string characterId) => Path.Combine(BuildsDir(), characterId + ".json");

        // Estado estático sobrevive a domain reload desabilitado — limpa o cache entre sessões de Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => cache.Clear();

        [Serializable]
        private class CharacterBuildSet
        {
            public List<CharacterBuild> slots = new List<CharacterBuild>();
        }
    }
}
