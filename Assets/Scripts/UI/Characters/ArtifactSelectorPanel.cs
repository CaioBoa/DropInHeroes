using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Modal de seleção de artefato da build: lista vertical (com scroll) dos ArtifactData do catálogo,
    /// cada linha com ícone + nome + texto do efeito, mais um campo de busca por nome. A primeira opção
    /// "Sem artefato" remove o artefato. Ao escolher, dispara <see cref="OnArtifactSelected"/> (id vazio = nenhum).
    /// </summary>
    public class ArtifactSelectorPanel : MonoBehaviour
    {
        [SerializeField] private Transform optionsContent;
        [SerializeField] private ArtifactOptionButton optionPrefab;
        [Tooltip("Busca por nome do artefato (filtra a lista).")]
        [SerializeField] private TMP_InputField searchInput;
        [Tooltip("Inclui uma linha 'Sem artefato' na lista. Deixe FALSO — a remoção fica no botão separado abaixo.")]
        [SerializeField] private bool includeNoneOption = false;
        [Tooltip("Botão SEPARADO de remover artefato (fora da lista, estilo próprio).")]
        [SerializeField] private Button removeButton;
        [Tooltip("Nomes/formato de stat no texto. Se nulo, GameConfig.Active.")]
        [SerializeField] private StatDefinitionCatalog catalog;

        public event Action<string> OnArtifactSelected; // artifactId ("" = nenhum)

        private readonly List<ArtifactOptionButton> options = new List<ArtifactOptionButton>();
        private bool built;
        private KeywordCatalog keywordCatalog;

        private StatDefinitionCatalog Catalog
        {
            get { if (catalog == null) catalog = GameConfig.Active?.StatDefinitions; return catalog; }
        }
        private KeywordCatalog Keywords
        {
            get { if (keywordCatalog == null) keywordCatalog = GameConfig.Active?.Keywords; return keywordCatalog; }
        }

        private void Awake()
        {
            if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearch);
            if (removeButton != null) removeButton.onClick.AddListener(HandleRemove);
        }

        private void OnDestroy()
        {
            if (searchInput != null) searchInput.onValueChanged.RemoveListener(OnSearch);
            if (removeButton != null) removeButton.onClick.RemoveListener(HandleRemove);
        }

        private void HandleRemove() => OnArtifactSelected?.Invoke(string.Empty);

        private void Build()
        {
            if (built) return;
            built = true;
            if (optionPrefab == null || optionsContent == null) return;

            if (includeNoneOption) AddOption(null);
            List<ArtifactData> all = DataManager.GetAllArtifacts();
            for (int i = 0; i < all.Count; i++) AddOption(all[i]);
        }

        private void AddOption(ArtifactData a)
        {
            ArtifactOptionButton opt = Instantiate(optionPrefab, optionsContent);
            string text = a != null ? ArtifactTextBuilder.Summary(a, Catalog, Keywords) : "Remover o artefato desta build.";
            opt.Setup(a, text, HandlePick);
            options.Add(opt);
        }

        /// <summary>Sincroniza o destaque de seleção com o artefato atual e reaplica o filtro de busca.</summary>
        public void Refresh(string currentArtifactId)
        {
            Build();
            for (int i = 0; i < options.Count; i++)
            {
                ArtifactData a = options[i].Artifact;
                bool sel = a != null ? a.ID == currentArtifactId : string.IsNullOrEmpty(currentArtifactId);
                options[i].SetSelected(sel);
            }
            OnSearch(searchInput != null ? searchInput.text : string.Empty);
        }

        private void HandlePick(ArtifactData a) => OnArtifactSelected?.Invoke(a != null ? a.ID : string.Empty);

        private void OnSearch(string query)
        {
            string q = query != null ? query.Trim().ToLowerInvariant() : string.Empty;
            for (int i = 0; i < options.Count; i++)
            {
                ArtifactData a = options[i].Artifact;
                bool show = a == null
                    ? q.Length == 0   // a opção "Sem artefato" só aparece sem busca ativa
                    : q.Length == 0 || (!string.IsNullOrEmpty(a.displayName) && a.displayName.ToLowerInvariant().Contains(q));
                options[i].gameObject.SetActive(show);
            }
        }
    }
}
