using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Combat;
using static DropInHeroes.EditorTools.StatTreeSymmetry;

namespace DropInHeroes.EditorTools
{

    /// <summary>
    /// "Semear base": gera uma árvore inicial PERFEITAMENTE simétrica (5 setores idênticos por rotação de 72°)
    /// em formato de grupo, pronta para ser editada à mão no <see cref="StatTreeEditorWindow"/>. Cada feição do
    /// setor canônico (topo) vira um grupo com 5 cópias; arestas são replicadas com o mesmo deslocamento de setor.
    /// Diferente do gerador antigo, o lado da roda depende só do rank (não do setor) — por isso não há mais o
    /// desalinhamento do topo. Stats aqui são apenas um ponto de partida; o designer reatribui por nó no editor.
    /// </summary>
    public static class StatTreeSeeder
    {
        [System.Serializable]
        public class SeedParams
        {
            public int[] pointsPerTier = { 8, 10, 12 };
            public int nodePoints = 10;
            public int notablePoints = 20;
            [Tooltip("Raio do nó interno da estrada por rank (topo → borda).")]
            public float[] inner = { 0.24f, 0.56f, 0.88f };
            [Tooltip("Raio do nó externo da estrada por rank.")]
            public float[] outer = { 0.42f, 0.74f, 1.06f };
            public bool wheels = true;
            public float wheelPerp = 0.20f;
            public float wheelRadius = 0.085f;
            public StatType[] roadStat = { StatType.Attack, StatType.Defense, StatType.Speed };
            public StatType[] wheelStat = { StatType.CritRate, StatType.Lifesteal, StatType.CritDamage };
            public StatType[] bridgeStat = { StatType.MaxHealth, StatType.DamageBonus };
        }

        public static void Build(SeedParams p, out List<TreeNode> nodes, out List<TreeEdge> edges, out int[] ppt)
        {
            nodes = new List<TreeNode>();
            edges = new List<TreeEdge>();
            List<TreeNode> nList = nodes;
            List<TreeEdge> eList = edges;
            int gid = 0;

            nList.Add(new TreeNode
            {
                nodeId = CenterId, tier = 0, normalizedPos = Vector2.zero,
                stat = StatType.MaxHealth, maxPoints = 0, grantPoints = 0, isNotable = false
            });

            // Cria um grupo (5 cópias) a partir da posição canônica no setor 0; devolve o gid.
            int AddGroup(Vector2 canon, StatType stat, int tier, bool notable, int pts)
            {
                int g = gid++;
                for (int s = 0; s < Sectors; s++)
                    nList.Add(new TreeNode
                    {
                        nodeId = GroupId(g, s), tier = tier, normalizedPos = Rotate(canon, SectorDeg * s),
                        stat = stat, maxPoints = 1, grantPoints = notable ? p.notablePoints : pts, isNotable = notable
                    });
                return g;
            }
            // Aresta entre grupos, replicada nos 5 setores com deslocamento (offB permite pontes entre setores vizinhos).
            void EdgeGroup(int gA, int gB, int offB)
            {
                for (int r = 0; r < Sectors; r++)
                    eList.Add(new TreeEdge { fromNodeId = GroupId(gA, r), toNodeId = GroupId(gB, (r + offB) % Sectors) });
            }
            void EdgeFromCenter(int gB)
            {
                for (int r = 0; r < Sectors; r++)
                    eList.Add(new TreeEdge { fromNodeId = CenterId, toNodeId = GroupId(gB, r) });
            }

            Vector2 dir = new Vector2(0f, 1f);   // topo
            Vector2 perp = new Vector2(1f, 0f);

            int[] inG = new int[3], outG = new int[3];
            for (int tr = 0; tr < 3; tr++)
            {
                inG[tr] = AddGroup(dir * p.inner[tr], p.roadStat[tr], tr, false, p.nodePoints);
                outG[tr] = AddGroup(dir * p.outer[tr], p.roadStat[tr], tr, false, p.nodePoints);

                if (tr == 0) EdgeFromCenter(inG[0]);
                else EdgeGroup(outG[tr - 1], inG[tr], 0);
                EdgeGroup(inG[tr], outG[tr], 0);

                if (p.wheels)
                {
                    float side = (tr % 2 == 0) ? 1f : -1f;      // depende SÓ do rank → simétrico entre setores
                    float mid = (p.inner[tr] + p.outer[tr]) * 0.5f;
                    float wr = p.wheelRadius;
                    Vector2 wc = dir * mid + perp * (side * p.wheelPerp);
                    int w0 = AddGroup(wc - dir * wr, p.wheelStat[tr], tr, false, p.nodePoints);
                    int w1 = AddGroup(wc + perp * (side * wr), p.wheelStat[tr], tr, true, p.nodePoints);   // notable
                    int w2 = AddGroup(wc + dir * wr, p.wheelStat[tr], tr, false, p.nodePoints);
                    int w3 = AddGroup(wc - perp * (side * wr), p.wheelStat[tr], tr, false, p.nodePoints);
                    EdgeGroup(inG[tr], w0, 0);
                    EdgeGroup(outG[tr], w2, 0);
                    EdgeGroup(w0, w1, 0); EdgeGroup(w1, w2, 0); EdgeGroup(w2, w3, 0); EdgeGroup(w3, w0, 0);
                }
            }

            // Pontes pentagonais entre setores vizinhos, nos ranks 1 e 3.
            int[] bridgeTiers = { 0, 2 };
            for (int bi = 0; bi < bridgeTiers.Length; bi++)
            {
                int tr = bridgeTiers[bi];
                Vector2 canon = Rotate(dir * p.outer[tr], SectorDeg * 0.5f);   // fronteira setor 0 / setor 1
                int bg = AddGroup(canon, p.bridgeStat[bi], tr, false, p.nodePoints);
                EdgeGroup(bg, outG[tr], 0);   // liga ao setor r
                EdgeGroup(bg, outG[tr], 1);   // e ao setor r+1
            }

            ppt = (int[])p.pointsPerTier.Clone();
        }
    }
}
