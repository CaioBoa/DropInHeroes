using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Combat;

namespace DropInHeroes.UI
{
    /// <summary>
    /// Uma palavra-chave de habilidade: texto exibido, cor (direta ou herdada de um stat) e uma
    /// explicação opcional (tooltip). Keywords só-cor (ex.: "dano físico") deixam a explicação vazia;
    /// keywords com explicação (ex.: "atordoamento") são empilhadas ao lado do tooltip.
    /// </summary>
    [System.Serializable]
    public class KeywordEntry
    {
        public string key;                 // identificador usado no markup ({kw:chave})
        public string displayText;         // texto exibido (vazio => usa a key)

        [Header("Cor")]
        [Tooltip("Se marcado, a cor vem do StatDefinitionCatalog (statColor).")]
        public bool useStatColor;
        public StatType statColor;
        public bool hasColor;
        public Color color = Color.white;

        [TextArea(2, 4)] public string explanation; // vazio => sem tooltip de explicação

        public string Display => string.IsNullOrEmpty(displayText) ? key : displayText;
        public bool HasExplanation => !string.IsNullOrEmpty(explanation);
    }

    /// <summary>
    /// Registro central de palavras-chave usadas nas descrições de habilidades.
    /// </summary>
    [CreateAssetMenu(fileName = "KeywordCatalog", menuName = "Game/System/Keyword Catalog")]
    public class KeywordCatalog : ScriptableObject
    {
        [SerializeField] private List<KeywordEntry> keywords = new List<KeywordEntry>();

        private Dictionary<string, KeywordEntry> lookup;

        public KeywordEntry Get(string key)
        {
            if (lookup == null) Build();
            return key != null && lookup.TryGetValue(key, out var e) ? e : null;
        }

        private void Build()
        {
            lookup = new Dictionary<string, KeywordEntry>();
            foreach (var k in keywords)
                if (!string.IsNullOrEmpty(k.key)) lookup[k.key] = k;
        }
    }
}
