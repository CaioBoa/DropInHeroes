using System;
using UnityEngine;
using DropInHeroes.Combat;

namespace DropInHeroes.Data
{

    /// <summary>
    /// Topologia compartilhada da árvore de stats (pentagrama). UM asset para todos os personagens:
    /// define a forma (nós, tiers, arestas, posições normalizadas) e o que cada nó concede quando
    /// alocado. As builds (default + custom) apenas escolhem QUAIS nós alocar; o valor concedido por
    /// nó vive aqui. Tier 0/1/2 corresponde a Rank 1/2/3 — um nó só passa a valer a partir do seu tier.
    /// </summary>
    [CreateAssetMenu(fileName = "SharedStatTree", menuName = "Game/Data/Stat Tree")]
    public class StatTreeData : ScriptableObject, IGameData
    {
        [Header("Metadata (Governance)")]
        [SerializeField] private string id = "shared_stat_tree";
        [SerializeField] private DataCategory category = DataCategory.StatTree;

        [Header("Alocação")]
        [Tooltip("Pontos alocáveis por tier (índice 0..2 = tier 1..3). Limita as escolhas na UI de build.")]
        public int[] pointsPerTier = { 1, 1, 1 };

        [Header("Topologia")]
        public TreeNode[] nodes = Array.Empty<TreeNode>();
        public TreeEdge[] edges = Array.Empty<TreeEdge>();

        public string ID => id;
        public DataCategory Category => category;

        public bool TryGetNode(string nodeId, out TreeNode node)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].nodeId == nodeId) { node = nodes[i]; return true; }
            }
            node = default;
            return false;
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = "shared_stat_tree";
            if (pointsPerTier == null || pointsPerTier.Length != 3)
                pointsPerTier = new[] { 1, 1, 1 };
        }
    #endif
    }

    /// <summary>
    /// Um nó do pentagrama (compartilhado por todos os personagens). Declara posição/estética, tier e
    /// QUAL stat investe. Cada ponto alocado pelo jogador = +1 ponto neste stat (× statPointValue via
    /// StatBudget) — o nó não carrega valor cru, herdando a coesão do orçamento de pontos.
    /// </summary>
    [Serializable]
    public struct TreeNode
    {
        public string nodeId;
        [Range(0, 2)] public int tier;
        [Tooltip("Posição normalizada (-1..1 em cada eixo) dentro do widget do pentagrama.")]
        public Vector2 normalizedPos;
        [Tooltip("Stat que este nó investe.")]
        public StatType stat;
        [Tooltip("Máximo de pontos alocáveis neste nó (0 = limitado apenas pelo orçamento do tier).")]
        public int maxPoints;
        [Tooltip("Pontos de stat concedidos ao alocar este nó (× statPointValue). 0 = trata como 1.")]
        public int grantPoints;
        [Tooltip("Nó notable (cluster/roda): visual maior e ornamentado, como no PoE.")]
        public bool isNotable;
    }

    /// <summary>Aresta estética entre dois nós (raio do pentagrama).</summary>
    [Serializable]
    public struct TreeEdge
    {
        public string fromNodeId;
        public string toNodeId;
    }
}
