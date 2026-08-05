using DropInHeroes.Data;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Calculador puro (sem UnitController) do preview "build aplicada". Replica exatamente a
    /// matemática de <see cref="Stat"/> — clamp((base + Σflat) × max(0, 1 + Σpercent)) — construindo
    /// um Stat descartável, de forma que o preview nunca diverge do combate.
    ///
    /// Tudo que soma stat é medido em PONTOS e convertido por <see cref="StatBudget.PointsToValue"/>
    /// (× statPointValue): base (StatBudget) + pontos alocados na árvore + pontos flat do artefato
    /// compartilham o mesmo balde aditivo; o percent do artefato entra no fold (1 + Σpercent).
    /// </summary>
    public static class StatPreviewCalculator
    {
        /// <summary>Valor de um stat com a build aplicada até o rank dado (tiers 0..rank-1, cumulativo).</summary>
        public static float PreviewStat(
            CharacterData character, StatTreeData tree, CharacterBuild build, ArtifactData artifact,
            int rank, StatType type, StatDefinitionCatalog catalog)
        {
            if (character == null) return 0f;

            StatDefinition def = catalog != null ? catalog.GetStatDefinition(type) : null;

            // Base (piso geral + statPoints da unidade), já em valor bruto.
            float value = StatBudget.ComputeBaseStat(character, type);

            // Pontos alocados na árvore para este stat, até o tier do rank (cumulativo).
            value += StatBudget.PointsToValue(type, TreePoints(build, tree, type, rank)); // mesmo statPointValue da base

            var stat = new Stat(type, value, def);

            // Artefato: flat em pontos (× statPointValue) + percent no fold. Cada grant é um modifier próprio.
            if (artifact != null && artifact.statGrants != null)
            {
                for (int i = 0; i < artifact.statGrants.Length; i++)
                {
                    StatGrant g = artifact.statGrants[i];
                    if (g.stat != type) continue;
                    stat.AddModifier("artifact_" + i, StatBudget.PointsToValue(type, g.points), g.percent);
                }
            }

            return stat.CurrentValue;
        }

        /// <summary>
        /// Pontos alocados na árvore para um stat, até o tier do rank (cumulativo). Cada alocação vale
        /// grantPoints do nó (0 = legado, vale 1). FONTE ÚNICA — usada pelo preview e pela aplicação da
        /// build em combate (<see cref="StatsModule.ApplyBuildStats"/>), para preview == combate.
        /// </summary>
        public static float TreePoints(CharacterBuild build, StatTreeData tree, StatType type, int rank)
        {
            if (build == null || build.allocations == null || tree == null) return 0f;
            float points = 0f;
            for (int i = 0; i < build.allocations.Count; i++)
            {
                TreeAllocation alloc = build.allocations[i];
                if (alloc.points <= 0) continue;
                if (!tree.TryGetNode(alloc.nodeId, out TreeNode node)) continue;
                if (node.stat != type || node.tier > rank - 1) continue; // só até o tier do rank
                points += alloc.points * (node.grantPoints > 0 ? node.grantPoints : 1);
            }
            return points;
        }

        /// <summary>Valor base puro do stat (sem build), já com o clamp da StatDefinition.</summary>
        public static float BaseStat(CharacterData character, StatType type, StatDefinitionCatalog catalog)
        {
            if (character == null) return 0f;
            StatDefinition def = catalog != null ? catalog.GetStatDefinition(type) : null;
            return new Stat(type, StatBudget.ComputeBaseStat(character, type), def).CurrentValue;
        }
    }
}
