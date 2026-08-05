using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.Sandbox
{

    /// <summary>
    /// Painel esquerdo do Sandbox (roleta): lista TODOS os personagens jogáveis (via catálogo) + o dummy,
    /// cada um como um <see cref="SandboxRosterItem"/> arrastável sob um LayoutGroup. Controller fino — só
    /// popula os itens; o drag e a config de build/rank vivem no item. Substitui o antigo bench + painel de
    /// picks. Cada personagem tem uma entrada de config no <see cref="SandboxController"/> (GetOrCreatePick).
    /// </summary>
    public class SandboxRoster : MonoBehaviour
    {
        [SerializeField] private SandboxController sandbox;
        [SerializeField] private Transform itemContainer;
        [SerializeField] private SandboxRosterItem itemPrefab;
        [Tooltip("Campo de busca que filtra os itens por nome (opcional).")]
        [SerializeField] private TMP_InputField searchInput;

        private readonly List<SandboxRosterItem> items = new List<SandboxRosterItem>();

        private void Awake()
        {
            if (sandbox == null) sandbox = FindFirstObjectByType<SandboxController>();
        }

        private void Start()
        {
            Build();
            if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);
        }

        private void OnDestroy()
        {
            if (searchInput != null) searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        }

        private void OnSearchChanged(string query)
        {
            string q = string.IsNullOrEmpty(query) ? string.Empty : query.Trim().ToLowerInvariant();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                CharacterData c = items[i].Character;
                string name = c != null && !string.IsNullOrEmpty(c.displayName) ? c.displayName.ToLowerInvariant() : string.Empty;
                items[i].gameObject.SetActive(q.Length == 0 || name.Contains(q));
            }
        }

        private void Build()
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null) Destroy(items[i].gameObject);
            items.Clear();

            if (sandbox == null || itemContainer == null || itemPrefab == null) return;

            // Personagens jogáveis; invocações (tag Summon) são excluídas — derivam stats de um dono e
            // não são posicionáveis avulsas (mesma regra do TowerPicks).
            var characters = DataManager.GetAllCharacters();
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData c = characters[i];
                if (c == null || (c.tags & UnitTag.Summon) != 0) continue;
                SandboxRosterItem item = Instantiate(itemPrefab, itemContainer);
                item.Bind(c, sandbox.GetOrCreatePick(c));
                items.Add(item);
            }

            // Dummy por último (sem build/rank; pode ser arrastado várias vezes).
            if (sandbox.DummyCharacter != null)
            {
                SandboxRosterItem dummyItem = Instantiate(itemPrefab, itemContainer);
                dummyItem.Bind(sandbox.DummyCharacter, null);
                items.Add(dummyItem);
            }
        }
    }
}
