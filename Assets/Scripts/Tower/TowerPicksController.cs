using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.UI;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Orquestra a tela de picks da Torre (viewer-first, layout pronto para 4 jogadores). Centro: grid de
    /// personagens + busca. Cantos: painel por jogador (local + "Aguardando"), mostrando o time. Inferior:
    /// <see cref="PickDetailPanel"/> (build por dropdown, stats no rank, artefato) → Confirmar adiciona ao seu
    /// time COM a build escolhida. Clicar num item já escolhido remove. Ao completar o time, um contador no topo
    /// dispara; ao zerar, os picks (personagem+build) vão para <see cref="TowerRunData"/> e a run é carregada.
    /// </summary>
    public class TowerPicksController : MonoBehaviour
    {
        public const int TeamSize = 5;

        [Header("Roster")]
        [SerializeField] private Transform listContent;
        [SerializeField] private CharacterListItem listItemPrefab;
        [SerializeField] private TMP_InputField searchInput;

        [Header("Pick detail")]
        [SerializeField] private PickDetailPanel pickDetailPanel;

        [Header("Jogadores (4 cantos; índice 0 = jogador local)")]
        [SerializeField] private PlayerCornerPanel[] cornerPanels = new PlayerCornerPanel[4];
        [SerializeField] private int localPlayerIndex = 0;
        [SerializeField] private string localPlayerName = "Você";

        [Header("Início automático (contador no topo)")]
        [Tooltip("Container do timer no topo (Canvas/Timer). Escondido até o time ficar completo; aparece durante a contagem.")]
        [SerializeField] private GameObject countdownRoot;
        [Tooltip("Texto do contador (segundos restantes).")]
        [SerializeField] private TMP_Text countdownText;
        [Tooltip("Segundos de contagem após completar o time, antes de iniciar o combate.")]
        [SerializeField] private float startCountdownSeconds = 5f;

        private StatTreeData tree;
        private readonly List<CharacterData> team = new List<CharacterData>(TeamSize);
        private readonly List<CharacterBuild> teamBuilds = new List<CharacterBuild>(TeamSize);
        private readonly List<CharacterListItem> spawnedItems = new List<CharacterListItem>();
        private CharacterData focused;
        private Coroutine countdown;

        private bool IsFull => team.Count >= TeamSize;
        private PlayerCornerPanel LocalCorner =>
            cornerPanels != null && localPlayerIndex >= 0 && localPlayerIndex < cornerPanels.Length ? cornerPanels[localPlayerIndex] : null;

        private void Start()
        {
            tree = DataManager.GetSharedStatTree();

            for (int i = 0; i < cornerPanels.Length; i++)
            {
                if (cornerPanels[i] == null) continue;
                if (i == localPlayerIndex) cornerPanels[i].SetupLocal(localPlayerName);
                else cornerPanels[i].SetupWaiting();
            }

            if (pickDetailPanel != null) pickDetailPanel.OnConfirm += OnConfirmPick;
            if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);

            ShowCountdown(false);
            BuildList();
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (pickDetailPanel != null) pickDetailPanel.OnConfirm -= OnConfirmPick;
            if (searchInput != null) searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        }

        private void BuildList()
        {
            if (listItemPrefab == null || listContent == null)
            {
                DebugManager.LogError("Roster não configurado (listItemPrefab/listContent).", DebugCategory.UI);
                return;
            }
            var characters = DataManager.GetAllCharacters();
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData c = characters[i];
                if (c == null) continue;
                if ((c.tags & UnitTag.Summon) != 0) continue; // invocações não são picáveis
                CharacterListItem item = Instantiate(listItemPrefab, listContent);
                item.Setup(c, OnListItemClicked);
                spawnedItems.Add(item);
            }
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

        // Clique num item abre o painel de pick. Picks são definitivos (sem deseleção): se o herói já
        // está no time, o painel abre só para visualização, com o botão Confirmar desabilitado.
        private void OnListItemClicked(CharacterData c)
        {
            if (c == null) return;
            focused = c;
            if (pickDetailPanel != null) pickDetailPanel.Show(c, tree, team.Contains(c));
        }

        private void OnConfirmPick(CharacterData c, CharacterBuild build)
        {
            if (c == null || IsFull || team.Contains(c)) return;
            team.Add(c);
            teamBuilds.Add(build);
            focused = null;
            if (pickDetailPanel != null) pickDetailPanel.Hide();
            RefreshUI();
            if (IsFull) BeginCountdown(); // time completo → dispara a contagem de início
        }

        private void RefreshUI()
        {
            if (LocalCorner != null) LocalCorner.SetTeam(team);
            for (int i = 0; i < spawnedItems.Count; i++)
                spawnedItems[i].SetClaimed(team.Contains(spawnedItems[i].CharacterData));
        }

        // Contador no topo: ao completar o time, conta N segundos e então carrega a run.
        private void BeginCountdown()
        {
            CancelCountdown();
            countdown = StartCoroutine(CountdownRoutine());
        }

        private void CancelCountdown()
        {
            if (countdown != null) { StopCoroutine(countdown); countdown = null; }
            ShowCountdown(false);
        }

        private IEnumerator CountdownRoutine()
        {
            ShowCountdown(true);
            float t = startCountdownSeconds;
            while (t > 0f)
            {
                if (countdownText != null) countdownText.text = Mathf.CeilToInt(t).ToString();
                t -= Time.deltaTime;
                yield return null;
            }
            if (countdownText != null) countdownText.text = "0";
            countdown = null;
            StartCombat();
        }

        private void ShowCountdown(bool on)
        {
            if (countdownRoot != null) countdownRoot.SetActive(on);
        }

        private void StartCombat()
        {
            if (!IsFull) return;
            TowerRunData.Set(team, teamBuilds);
            LoadingRequest.Configure(SceneNames.TowerRun);
            SceneManager.LoadScene(SceneNames.Loading);
        }
    }
}
