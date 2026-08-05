using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Widget da árvore de stats (pentagrama tipo teia) com zoom/pan CONTIDO ao próprio painel.
    /// Nós são binários: clicar alterna ativar/desativar. Anéis por RANK (tier 0/1/2 = rank 1/2/3),
    /// cada rank com várias camadas e ligações radiais + circunferenciais (várias builds possíveis).
    /// Conectividade: o CENTRO é âncora fixa (só visual, não marcável) e libera seus vizinhos; um nó só
    /// é ativável se ligado ao centro ou a um nó já ativo. Desativar refaz o alcance a partir do centro
    /// e remove órfãos (refund em cascata). Pontos por rank via pointsPerTier, com indicadores.
    /// </summary>
    public class StatTreeView : MonoBehaviour, IDragHandler, IScrollHandler
    {
        private const string CenterId = "center";

        [SerializeField] private RectTransform nodesArea;
        [SerializeField] private StatTreeNodeView nodePrefab;
        [Tooltip("Emblema decorativo do nó central (ex.: pentagrama). Null = círculo dourado simples.")]
        [SerializeField] private Sprite centerEmblem;
        [Tooltip("Ícones dos stats. Se nulo, GameConfig.Active.")]
        [SerializeField] private StatDefinitionCatalog catalog;
        [Tooltip("Cor por rank: [0]=rank1 (interno), [1]=rank2, [2]=rank3 (externo).")]
        [SerializeField] private Color[] rankColors =
        {
            new Color(0.30f, 0.70f, 1f), new Color(0.72f, 0.52f, 1f), new Color(1f, 0.62f, 0.30f)
        };
        [Tooltip("Labels de pontos restantes por rank [rank1, rank2, rank3].")]
        [SerializeField] private TMP_Text[] tierPointsLabels = new TMP_Text[3];

        [SerializeField] private float radiusFraction = 0.40f;
        [SerializeField] private float edgeThickness = 4f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 2.5f;
        [Tooltip("Folga de pan além do raio da árvore (fração do viewport). Somada ao raio×zoom para permitir alcançar cantos/topo com zoom.")]
        [SerializeField] private float panMargin = 0.6f;

        [Header("Hover")]
        [Tooltip("Balão que, ao passar o mouse num nó, mostra quanto de stat REAL aquele nó concede.")]
        [SerializeField] private RectTransform hoverTooltip;
        [SerializeField] private TMP_Text hoverTooltipText;

        public event Action OnAllocationChanged;

        private RectTransform content;
        private Canvas canvas;
        private readonly List<StatTreeNodeView> nodeViews = new List<StatTreeNodeView>();
        private readonly List<Image> edgeViews = new List<Image>();
        private BuildAllocator allocator;
        private StatTreeData tree;
        private CharacterBuild build;
        private bool editable;
        private float zoom = 1f;
        private float contentRadius;

        private StatDefinitionCatalog Catalog
        {
            get { if (catalog == null) catalog = GameConfig.Active?.StatDefinitions; return catalog; }
        }

        private void EnsureContent()
        {
            if (content != null) return;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            var go = new GameObject("TreeContent", typeof(RectTransform));
            content = (RectTransform)go.transform;
            content.SetParent(nodesArea != null ? nodesArea : (RectTransform)transform, false);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
        }

        public void Render(StatTreeData treeData, CharacterBuild characterBuild, bool isEditable)
        {
            // Zoom/pan só resetam ao trocar de árvore/build; re-render da MESMA build (ex.: clique
            // num nó dispara re-render via controller) preserva a vista do jogador.
            bool viewChanged = treeData != tree || characterBuild != build;
            tree = treeData;
            build = characterBuild;
            editable = isEditable;

            EnsureContent();
            if (viewChanged)
            {
                content.anchoredPosition = Vector2.zero;
                zoom = 1f;
                content.localScale = Vector3.one;
            }

            // Reconstrói a cada render (barato): pega pointsPerTier/topologia AO VIVO do asset — o
            // orçamento na tela acompanha edições da árvore sem precisar reabrir a tela.
            if (hoverTooltip != null) hoverTooltip.gameObject.SetActive(false);

            allocator = new BuildAllocator(tree);
            float radius = Mathf.Min(nodesArea.rect.width, nodesArea.rect.height) * radiusFraction;
            contentRadius = radius;

            int n = tree != null && tree.nodes != null ? tree.nodes.Length : 0;
            while (nodeViews.Count < n) nodeViews.Add(Instantiate(nodePrefab, content));
            for (int i = 0; i < nodeViews.Count; i++) nodeViews[i].gameObject.SetActive(i < n);
            for (int i = 0; i < n; i++)
            {
                TreeNode node = tree.nodes[i];
                bool isCenter = node.nodeId == CenterId;
                StatTreeNodeView v = nodeViews[i];
                v.transform.SetParent(content, false);
                v.Rect.anchoredPosition = node.normalizedPos * radius;
                // Hierarquia visual PoE: centro > notables (rodas) > nós de estrada.
                float size = isCenter ? 68f : (node.isNotable ? 54f : 40f);
                v.Rect.sizeDelta = new Vector2(size, size);
                v.Setup(node, Catalog != null ? Catalog.GetStatDefinition(node.stat) : null, OnNodeToggle, OnNodeHover, centerEmblem, isCenter);
                v.transform.SetAsLastSibling();
            }

            BuildEdges(radius);
            RefreshNodes();
        }

        private void BuildEdges(float radius)
        {
            int m = tree != null && tree.edges != null ? tree.edges.Length : 0;
            while (edgeViews.Count < m)
            {
                var g = new GameObject("Edge", typeof(RectTransform));
                var im = g.AddComponent<Image>();
                im.raycastTarget = false;
                var rt = (RectTransform)g.transform;
                rt.SetParent(content, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                edgeViews.Add(im);
            }
            for (int i = 0; i < edgeViews.Count; i++) edgeViews[i].gameObject.SetActive(i < m);

            for (int i = 0; i < m; i++)
            {
                TreeEdge e = tree.edges[i];
                if (!tree.TryGetNode(e.fromNodeId, out TreeNode a) || !tree.TryGetNode(e.toNodeId, out TreeNode b))
                {
                    edgeViews[i].gameObject.SetActive(false);
                    continue;
                }
                Vector2 pa = a.normalizedPos * radius, pb = b.normalizedPos * radius;
                var rt = (RectTransform)edgeViews[i].transform;
                rt.SetAsFirstSibling();
                rt.anchoredPosition = (pa + pb) * 0.5f;
                Vector2 d = pb - pa;
                rt.sizeDelta = new Vector2(d.magnitude, edgeThickness);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                Color c = RankColor(Mathf.Max(a.tier, b.tier));
                c.a = 0.7f; // legibilidade sobre o fundo de constelação
                edgeViews[i].color = c;
            }
        }

        private static readonly Color[] DefaultRankColors =
        {
            new Color(0.30f, 0.70f, 1f), new Color(0.72f, 0.52f, 1f), new Color(1f, 0.62f, 0.30f)
        };

        private Color RankColor(int tier)
        {
            Color[] pal = rankColors != null && rankColors.Length >= 3 ? rankColors : DefaultRankColors;
            return tier >= 0 && tier < pal.Length ? pal[tier] : Color.gray;
        }

        // Regras de alocação (conectividade + orçamento + prune) delegadas ao BuildAllocator — mesma
        // lógica usada pelo editor de build padrão, para autoria e jogo nunca divergirem.
        private bool IsUnlockable(string nodeId) => allocator != null && allocator.IsUnlockable(build, nodeId);

        private void OnNodeToggle(string nodeId)
        {
            if (!editable || allocator == null) return;
            if (allocator.Toggle(build, nodeId)) { RefreshNodes(); OnAllocationChanged?.Invoke(); }
        }

        private void RefreshNodes()
        {
            int n = tree != null && tree.nodes != null ? tree.nodes.Length : 0;
            for (int i = 0; i < n; i++)
            {
                TreeNode node = tree.nodes[i];
                bool isCenter = node.nodeId == CenterId;
                bool alloc = GetPoints(node.nodeId) > 0;
                bool unlock = isCenter || IsUnlockable(node.nodeId);
                bool canToggle = !isCenter && editable && (alloc || (unlock && TierSpent(node.tier) < TierBudget(node.tier)));
                nodeViews[i].Refresh(alloc, canToggle, unlock, isCenter, node.isNotable, RankColor(node.tier));
            }

            if (tierPointsLabels != null)
                for (int t = 0; t < tierPointsLabels.Length; t++)
                    if (tierPointsLabels[t] != null)
                        tierPointsLabels[t].text = "Rank " + (t + 1) + "   " + (TierBudget(t) - TierSpent(t)) + "/" + TierBudget(t);
        }

        private int TierBudget(int tier) => allocator != null ? allocator.TierBudget(tier) : 0;
        private int TierSpent(int tier) => allocator != null ? allocator.TierSpent(build, tier) : 0;
        private int GetPoints(string nodeId) => allocator != null ? allocator.GetPoints(build, nodeId) : 0;

        // === Pan / Zoom (zoom para o cursor; pan escalado pelo zoom p/ alcançar topo e cantos) ===
        public void OnDrag(PointerEventData e)
        {
            if (content == null || nodesArea == null) return;
            float sf = canvas != null ? canvas.scaleFactor : 1f;
            content.anchoredPosition = ClampPan(content.anchoredPosition + e.delta / (sf <= 0f ? 1f : sf));
        }

        public void OnScroll(PointerEventData e)
        {
            if (content == null || nodesArea == null) return;
            float old = zoom;
            zoom = Mathf.Clamp(zoom * (1f + e.scrollDelta.y * 0.12f), minZoom, maxZoom);
            if (Mathf.Approximately(zoom, old)) return;
            content.localScale = new Vector3(zoom, zoom, 1f);

            // Mantém fixo o ponto sob o cursor (zoom para onde o mouse está).
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(nodesArea, e.position, EventCam(), out Vector2 m))
            {
                m -= nodesArea.rect.center;
                Vector2 c = content.anchoredPosition;
                content.anchoredPosition = ClampPan(m - (m - c) * (zoom / old));
            }
        }

        private Camera EventCam()
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera;
        }

        private Vector2 ClampPan(Vector2 p)
        {
            float maxX = contentRadius * zoom + nodesArea.rect.width * panMargin;
            float maxY = contentRadius * zoom + nodesArea.rect.height * panMargin;
            p.x = Mathf.Clamp(p.x, -maxX, maxX);
            p.y = Mathf.Clamp(p.y, -maxY, maxY);
            return p;
        }

        // Balão de hover: quanto de stat REAL este nó concede (pontos × statPointValue).
        private void OnNodeHover(string nodeId, bool entered)
        {
            if (hoverTooltip == null || tree == null) return;
            if (!entered || !tree.TryGetNode(nodeId, out TreeNode node) || node.nodeId == CenterId)
            {
                hoverTooltip.gameObject.SetActive(false);
                return;
            }
            float pts = node.grantPoints > 0 ? node.grantPoints : 1f;
            float val = StatBudget.PointsToValue(node.stat, pts);
            StatDefinition def = Catalog != null ? Catalog.GetStatDefinition(node.stat) : null;
            string statName = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : node.stat.ToString();
            bool pct = def != null && def.isPercent;
            if (hoverTooltipText != null)
                hoverTooltipText.text = "+" + val.ToString("0.#") + (pct ? "%" : string.Empty) + "  " + statName;

            int idx = IndexOfNode(nodeId);
            if (idx >= 0) hoverTooltip.position = nodeViews[idx].Rect.position + new Vector3(0f, 44f, 0f);
            hoverTooltip.SetAsLastSibling();
            hoverTooltip.gameObject.SetActive(true);
        }

        private int IndexOfNode(string nodeId)
        {
            int n = tree != null && tree.nodes != null ? tree.nodes.Length : 0;
            for (int i = 0; i < n; i++) if (tree.nodes[i].nodeId == nodeId) return i;
            return -1;
        }
    }
}
