using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Tower;
using DropInHeroes.Utils;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Raiz da tela de Personagens (viewer-first). Esquerda: roll de heróis com pesquisa + seletor de
    /// build (Default + builds custom criadas sob demanda, com "+") + voltar. Direita: abas Overview ↔
    /// Stats Tree do herói. Builds custom são editadas em memória e persistidas por <see cref="BuildStore"/>;
    /// a Default é somente-leitura. Nome da build editável no topo (ícone de lápis). Entrar via cena Loading.
    /// </summary>
    public class CharactersScreenController : MonoBehaviour
    {
        [Header("Roster")]
        [SerializeField] private Transform listContent;
        [SerializeField] private CharacterListItem listItemPrefab;
        [SerializeField] private TMP_InputField searchInput;

        [Header("Abas")]
        [SerializeField] private Button overviewTabButton;
        [SerializeField] private Button treeTabButton;
        [SerializeField] private GameObject overviewRoot;
        [SerializeField] private GameObject treeRoot;
        [SerializeField] private GameObject overviewTabSelected;
        [SerializeField] private GameObject treeTabSelected;

        [Header("Painéis")]
        [SerializeField] private CharacterMenuPanel menuPanel;
        [SerializeField] private BuildSelectorPanel buildSelector;
        [SerializeField] private ArtifactSelectorPanel artifactSelector;
        [SerializeField] private GameObject artifactSelectorRoot;
        [SerializeField] private Button artifactCloseButton;      // fecha o modal de artefato
        [SerializeField] private StatTreeView treeView;
        [SerializeField] private StatsComparePanel comparePanel;
        [SerializeField] private GameObject comparePanelRoot;     // comparação: visível segurando SHIFT na árvore
        [SerializeField] private GameObject shiftHint;            // dica "Segure SHIFT"; some ao comparar
        [SerializeField] private Button saveButton;
        [SerializeField] private GameObject savedFeedback;

        [Header("Nome da build")]
        [SerializeField] private TMP_InputField buildNameInput;
        [SerializeField] private Button buildNameEditButton;      // lápis (só em build custom)
        [SerializeField] private Button buildDeleteButton;        // lixeira à esquerda do lápis (só em build custom)
        [SerializeField] private ConfirmDialog confirmDialog;     // confirmação temática de exclusão

        [Header("Navegação")]
        [SerializeField] private Button backButton;

        [Header("Preview")]
        [Tooltip("Rank usado no preview da build no Overview (tiers 0..rank-1). Default = rank máximo.")]
        [SerializeField] private int previewRank = 3;

        private readonly List<CharacterListItem> spawnedItems = new List<CharacterListItem>();
        private StatTreeData tree;
        private CharacterData selected;
        private CharacterBuild currentBuild;
        private Coroutine fade;

        // Editável = é uma build custom (não a Default, que vive no asset).
        private bool IsEditable => selected != null && currentBuild != null && !ReferenceEquals(currentBuild, selected.defaultBuild);

        private void Start()
        {
            tree = DataManager.GetSharedStatTree();
            if (buildSelector != null) buildSelector.SetTree(tree);

            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (overviewTabButton != null) overviewTabButton.onClick.AddListener(() => SetTab(true));
            if (treeTabButton != null) treeTabButton.onClick.AddListener(() => SetTab(false));
            if (buildSelector != null) { buildSelector.OnBuildSelected += OnBuildSelected; buildSelector.OnCreateBuild += OnCreateBuild; }
            if (artifactSelector != null) artifactSelector.OnArtifactSelected += OnArtifactSelected;
            if (treeView != null) treeView.OnAllocationChanged += OnAllocationChanged;
            if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
            if (menuPanel != null) menuPanel.OnArtifactClicked += ToggleArtifactPopup;
            if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);
            if (buildNameEditButton != null) buildNameEditButton.onClick.AddListener(BeginRenameBuild);
            if (buildDeleteButton != null) buildDeleteButton.onClick.AddListener(ConfirmDeleteBuild);
            if (artifactCloseButton != null) artifactCloseButton.onClick.AddListener(() => SetArtifactPopup(false));
            if (buildNameInput != null) buildNameInput.onEndEdit.AddListener(EndRenameBuild);

            SetArtifactPopup(false);
            SetTab(true);
            BuildRoster();
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            if (overviewTabButton != null) overviewTabButton.onClick.RemoveAllListeners();
            if (treeTabButton != null) treeTabButton.onClick.RemoveAllListeners();
            if (buildSelector != null) { buildSelector.OnBuildSelected -= OnBuildSelected; buildSelector.OnCreateBuild -= OnCreateBuild; }
            if (artifactSelector != null) artifactSelector.OnArtifactSelected -= OnArtifactSelected;
            if (treeView != null) treeView.OnAllocationChanged -= OnAllocationChanged;
            if (saveButton != null) saveButton.onClick.RemoveListener(OnSaveClicked);
            if (menuPanel != null) menuPanel.OnArtifactClicked -= ToggleArtifactPopup;
            if (searchInput != null) searchInput.onValueChanged.RemoveListener(OnSearchChanged);
            if (buildNameEditButton != null) buildNameEditButton.onClick.RemoveListener(BeginRenameBuild);
            if (buildDeleteButton != null) buildDeleteButton.onClick.RemoveListener(ConfirmDeleteBuild);
            if (artifactCloseButton != null) artifactCloseButton.onClick.RemoveAllListeners();
            if (buildNameInput != null) buildNameInput.onEndEdit.RemoveListener(EndRenameBuild);
        }

        private void BuildRoster()
        {
            if (listItemPrefab == null || listContent == null)
            {
                DebugManager.LogError("Roster não configurado (listItemPrefab/listContent).", DebugCategory.UI);
                return;
            }

            var characters = DataManager.GetAllCharacters();
            CharacterData first = null;
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData c = characters[i];
                if (c == null) continue;
                if ((c.tags & UnitTag.Summon) != 0) continue; // invocações não são buildáveis
                CharacterListItem item = Instantiate(listItemPrefab, listContent);
                item.Setup(c, OnCharacterSelected);
                spawnedItems.Add(item);
                if (first == null) first = c;
            }

            if (first != null) OnCharacterSelected(first);
            else if (menuPanel != null) menuPanel.Clear();
        }

        private void OnSearchChanged(string query)
        {
            string q = query != null ? query.Trim().ToLowerInvariant() : string.Empty;
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                CharacterData d = spawnedItems[i].CharacterData;
                string name = d != null && !string.IsNullOrEmpty(d.displayName) ? d.displayName.ToLowerInvariant() : string.Empty;
                spawnedItems[i].gameObject.SetActive(q.Length == 0 || name.Contains(q));
            }
        }

        private void SetTab(bool overview)
        {
            if (overviewTabSelected != null) overviewTabSelected.SetActive(overview);
            if (treeTabSelected != null) treeTabSelected.SetActive(!overview);
            if (!overview) SetArtifactPopup(false);

            GameObject entering = overview ? overviewRoot : treeRoot;
            GameObject leaving = overview ? treeRoot : overviewRoot;
            if (leaving != null) leaving.SetActive(false);
            if (entering != null)
            {
                entering.SetActive(true);
                if (fade != null) StopCoroutine(fade);
                fade = StartCoroutine(FadeIn(entering));
            }
        }

        private IEnumerator FadeIn(GameObject root)
        {
            var group = root.GetComponent<CanvasGroup>();
            if (group == null) group = root.AddComponent<CanvasGroup>();
            const float duration = 0.15f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            group.alpha = 1f;
            fade = null;
        }

        private void OnCharacterSelected(CharacterData c)
        {
            selected = c;
            currentBuild = c != null ? c.defaultBuild : null; // começa na Default
            SetArtifactPopup(false);
            RefreshAll();
        }

        private void OnBuildSelected(CharacterBuild build)
        {
            currentBuild = build;
            SetArtifactPopup(false);
            RefreshAll();
        }

        private void OnCreateBuild()
        {
            if (selected == null) return;
            CharacterBuild b = BuildStore.CreateCustomBuild(selected.ID);
            if (b == null) return;
            currentBuild = b;
            RefreshAll();
            BeginRenameBuild(); // já abre para nomear a nova build
        }

        private void ToggleArtifactPopup()
        {
            if (!IsEditable) return; // só edita artefato em build custom
            SetArtifactPopup(artifactSelectorRoot != null && !artifactSelectorRoot.activeSelf);
        }

        private void SetArtifactPopup(bool on)
        {
            if (artifactSelectorRoot != null) artifactSelectorRoot.SetActive(on);
        }

        private void OnArtifactSelected(string artifactId)
        {
            if (!IsEditable || currentBuild == null) return;
            currentBuild.artifactId = artifactId;
            Save();
            SetArtifactPopup(false);
            RefreshPreview();
        }

        private void OnAllocationChanged()
        {
            if (!IsEditable) return;
            Save();
            if (buildSelector != null) buildSelector.Refresh(selected, currentBuild); // atualiza aviso de incompleta
            RefreshPreview();
        }

        private void Save()
        {
            if (selected != null && IsEditable) BuildStore.Persist(selected.ID);
        }

        private void RefreshAll()
        {
            if (buildSelector != null) buildSelector.Refresh(selected, currentBuild);
            RefreshBuildName();
            RefreshPreview();
        }

        private void RefreshBuildName()
        {
            if (buildNameInput != null)
            {
                buildNameInput.SetTextWithoutNotify(currentBuild != null ? currentBuild.label : string.Empty);
                buildNameInput.readOnly = true;
                buildNameInput.interactable = IsEditable;
            }
            if (buildNameEditButton != null) buildNameEditButton.gameObject.SetActive(IsEditable);
            if (buildDeleteButton != null) buildDeleteButton.gameObject.SetActive(IsEditable);
        }

        // === Excluir build (só custom; confirmação temática) ===
        private void ConfirmDeleteBuild()
        {
            if (!IsEditable || currentBuild == null) return;
            string label = string.IsNullOrEmpty(currentBuild.label) ? "esta build" : $"\"{currentBuild.label}\"";
            if (confirmDialog != null)
                confirmDialog.Show("Apagar build", $"Deseja apagar {label}? Esta ação não pode ser desfeita.", DoDeleteBuild);
            else
                DoDeleteBuild();
        }

        private void DoDeleteBuild()
        {
            if (selected == null || currentBuild == null || !IsEditable) return;
            BuildStore.DeleteCustomBuild(selected.ID, currentBuild);
            currentBuild = selected.defaultBuild; // volta para a Default
            SetArtifactPopup(false);
            RefreshAll();
        }

        private void RefreshPreview()
        {
            ArtifactData artifact = currentBuild != null && !string.IsNullOrEmpty(currentBuild.artifactId)
                ? DataManager.GetArtifact(currentBuild.artifactId) : null;

            if (menuPanel != null) menuPanel.Display(selected, tree, currentBuild, artifact, previewRank);
            if (artifactSelector != null) artifactSelector.Refresh(currentBuild != null ? currentBuild.artifactId : string.Empty);
            if (treeView != null) treeView.Render(tree, currentBuild, IsEditable);
            if (comparePanel != null) comparePanel.Display(selected, tree, currentBuild, artifact, menuPanel != null ? menuPanel.DisplayedStats : null);
            if (savedFeedback != null) savedFeedback.SetActive(false);

            for (int i = 0; i < spawnedItems.Count; i++)
                spawnedItems[i].SetClaimed(spawnedItems[i].CharacterData == selected);
        }

        // === Rename da build ===
        private void BeginRenameBuild()
        {
            if (!IsEditable || buildNameInput == null) return;
            buildNameInput.readOnly = false;
            buildNameInput.ActivateInputField();
            buildNameInput.Select();
        }

        private void EndRenameBuild(string text)
        {
            if (buildNameInput != null) buildNameInput.readOnly = true;
            if (!IsEditable || currentBuild == null) return;
            if (!string.IsNullOrWhiteSpace(text)) currentBuild.label = text.Trim();
            Save();
            if (buildSelector != null) buildSelector.Refresh(selected, currentBuild);
            RefreshBuildName();
        }

        // Comparação de ranks: visível enquanto SHIFT estiver pressionado na aba da árvore.
        private void Update()
        {
            if (treeRoot == null) return;
            bool onTree = treeRoot.activeSelf;
            bool show = onTree && ShiftHeld();
            if (comparePanelRoot != null && comparePanelRoot.activeSelf != show) comparePanelRoot.SetActive(show);
            // A dica de SHIFT só aparece na árvore quando NÃO se está comparando.
            if (shiftHint != null && shiftHint.activeSelf != (onTree && !show)) shiftHint.SetActive(onTree && !show);
        }

        private static bool ShiftHeld()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }

        private void OnSaveClicked()
        {
            Save();
            if (savedFeedback != null) savedFeedback.SetActive(IsEditable);
        }

        private void OnBack() => SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
