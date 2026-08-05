using System.Collections.Generic;

namespace DropInHeroes.Data
{

    /// <summary>
    /// Regras de alocação de uma <see cref="CharacterBuild"/> na árvore de stats — FONTE ÚNICA,
    /// compartilhada pela UI do jogo (StatTreeView) e pelo editor de build padrão (StatTreeEditorWindow),
    /// para que autoria e jogo nunca divirjam. Nós são binários (0/1). Um nó só é alocável se ligado ao
    /// CENTRO (âncora sempre ativa) ou a um nó já alocado; orçamento por rank (<c>pointsPerTier</c>);
    /// desalocar refaz o alcance a partir do centro e remove órfãos (refund em cascata). Trabalha sobre
    /// arrays de topologia (não sobre o SO) para servir também o editor com edições ainda não salvas.
    /// </summary>
    public class BuildAllocator
    {
        public const string CenterId = "center";

        private readonly int[] pointsPerTier;
        private readonly Dictionary<string, TreeNode> nodeLookup = new Dictionary<string, TreeNode>();
        private readonly Dictionary<string, List<string>> adjacency = new Dictionary<string, List<string>>();

        public BuildAllocator(StatTreeData tree) : this(tree?.nodes, tree?.edges, tree?.pointsPerTier) { }

        public BuildAllocator(IReadOnlyList<TreeNode> nodes, IReadOnlyList<TreeEdge> edges, int[] pointsPerTier)
        {
            this.pointsPerTier = pointsPerTier;

            if (nodes != null)
                for (int i = 0; i < nodes.Count; i++)
                    if (!string.IsNullOrEmpty(nodes[i].nodeId)) nodeLookup[nodes[i].nodeId] = nodes[i];

            if (edges != null)
                for (int i = 0; i < edges.Count; i++)
                {
                    AddAdj(edges[i].fromNodeId, edges[i].toNodeId);
                    AddAdj(edges[i].toNodeId, edges[i].fromNodeId);
                }
        }

        private void AddAdj(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return;
            if (!adjacency.TryGetValue(a, out var l)) { l = new List<string>(); adjacency[a] = l; }
            l.Add(b);
        }

        private bool TryGetNode(string id, out TreeNode node) => nodeLookup.TryGetValue(id ?? string.Empty, out node);

        public int GetPoints(CharacterBuild build, string nodeId)
        {
            if (build == null || build.allocations == null) return 0;
            for (int i = 0; i < build.allocations.Count; i++)
                if (build.allocations[i].nodeId == nodeId) return build.allocations[i].points;
            return 0;
        }

        public bool IsAllocated(CharacterBuild build, string nodeId) => GetPoints(build, nodeId) > 0;

        /// <summary>Alcançável: vizinho é o centro (sempre ativo) ou um nó já alocado.</summary>
        public bool IsUnlockable(CharacterBuild build, string nodeId)
        {
            if (adjacency.TryGetValue(nodeId, out var nbrs))
                for (int i = 0; i < nbrs.Count; i++)
                    if (nbrs[i] == CenterId || GetPoints(build, nbrs[i]) > 0) return true;
            return false;
        }

        public int TierBudget(int tier)
            => pointsPerTier != null && tier >= 0 && tier < pointsPerTier.Length ? pointsPerTier[tier] : 0;

        public int TierSpent(CharacterBuild build, int tier)
        {
            if (build == null || build.allocations == null) return 0;
            int sum = 0;
            for (int i = 0; i < build.allocations.Count; i++)
            {
                TreeAllocation a = build.allocations[i];
                if (a.points <= 0) continue;
                if (TryGetNode(a.nodeId, out TreeNode nd) && nd.tier == tier) sum += a.points;
            }
            return sum;
        }

        /// <summary>Pode ligar este nó agora? (existe, não é centro, alcançável e há orçamento no rank dele).</summary>
        public bool CanAllocate(CharacterBuild build, string nodeId)
        {
            if (build == null || nodeId == CenterId) return false;
            if (!TryGetNode(nodeId, out TreeNode node) || IsAllocated(build, nodeId)) return false;
            return IsUnlockable(build, nodeId) && TierSpent(build, node.tier) < TierBudget(node.tier);
        }

        /// <summary>Alterna a alocação do nó respeitando as regras. Retorna true se algo mudou.</summary>
        public bool Toggle(CharacterBuild build, string nodeId)
        {
            if (build == null || nodeId == CenterId || !TryGetNode(nodeId, out TreeNode node)) return false;

            if (IsAllocated(build, nodeId))
            {
                SetPoints(build, nodeId, 0);
                PruneOrphans(build);
                return true;
            }
            if (!IsUnlockable(build, nodeId) || TierSpent(build, node.tier) >= TierBudget(node.tier)) return false;
            SetPoints(build, nodeId, 1);
            return true;
        }

        public void Clear(CharacterBuild build)
        {
            if (build?.allocations != null) build.allocations.Clear();
        }

        /// <summary>Remove nós alocados que não alcançam mais o centro por um caminho de nós alocados.</summary>
        public void PruneOrphans(CharacterBuild build)
        {
            if (build == null || build.allocations == null) return;

            var reachable = new HashSet<string> { CenterId };
            var queue = new Queue<string>();
            queue.Enqueue(CenterId);
            while (queue.Count > 0)
            {
                string cur = queue.Dequeue();
                if (!adjacency.TryGetValue(cur, out var nbrs)) continue;
                for (int i = 0; i < nbrs.Count; i++)
                    if (!reachable.Contains(nbrs[i]) && GetPoints(build, nbrs[i]) > 0)
                    {
                        reachable.Add(nbrs[i]);
                        queue.Enqueue(nbrs[i]);
                    }
            }

            for (int i = build.allocations.Count - 1; i >= 0; i--)
                if (build.allocations[i].points > 0 && !reachable.Contains(build.allocations[i].nodeId))
                    build.allocations.RemoveAt(i);
        }

        private static void SetPoints(CharacterBuild build, string nodeId, int points)
        {
            for (int i = 0; i < build.allocations.Count; i++)
                if (build.allocations[i].nodeId == nodeId)
                {
                    if (points <= 0) build.allocations.RemoveAt(i);
                    else build.allocations[i] = new TreeAllocation { nodeId = nodeId, points = points };
                    return;
                }
            if (points > 0) build.allocations.Add(new TreeAllocation { nodeId = nodeId, points = points });
        }
    }
}
