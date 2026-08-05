using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Tabela comparativa da árvore (aba retrátil no canto inferior direito): para cada stat, mostra o
    /// valor Base e os valores da build atual em Rank 1 / 2 / 3 (via <see cref="StatPreviewCalculator"/>,
    /// cumulativo por tier). Atualizada a cada mudança de alocação/artefato/personagem.
    /// </summary>
    public class StatsComparePanel : MonoBehaviour
    {
        [SerializeField] private StatCompareRow rowPrefab;
        [SerializeField] private Transform container;
        [Tooltip("Ícone/cor/formato por stat. Se nulo, GameConfig.Active.")]
        [SerializeField] private StatDefinitionCatalog catalog;
        [SerializeField] private StatType[] displayedStats =
        {
            StatType.Attack, StatType.MagicalPower, StatType.Defense, StatType.MagicalDefense,
            StatType.MaxHealth, StatType.Speed, StatType.Range, StatType.EnergyRegeneration,
            StatType.CritRate, StatType.CritDamage, StatType.Penetration, StatType.MagicalPenetration,
            StatType.Lifesteal, StatType.Accuracy, StatType.Dodge, StatType.Effectiveness,
            StatType.Tenacity, StatType.Control, StatType.DamageBonus, StatType.DamageReduction
        };

        private readonly List<StatCompareRow> rows = new List<StatCompareRow>();

        private StatDefinitionCatalog Catalog
        {
            get { if (catalog == null) catalog = GameConfig.Active?.StatDefinitions; return catalog; }
        }

        /// <summary>Exibe a comparação. 'stats' = mesma lista da aba Overview (fallback: lista própria).</summary>
        public void Display(CharacterData character, StatTreeData tree, CharacterBuild build, ArtifactData artifact, IReadOnlyList<StatType> stats)
        {
            if (character == null || rowPrefab == null || container == null) return;
            StatDefinitionCatalog cat = Catalog;
            IReadOnlyList<StatType> list = stats != null && stats.Count > 0 ? stats : displayedStats;

            for (int i = 0; i < list.Count; i++)
            {
                StatCompareRow row;
                if (i < rows.Count) { row = rows[i]; row.gameObject.SetActive(true); }
                else { row = Instantiate(rowPrefab, container); rows.Add(row); }

                StatType type = list[i];
                StatDefinition def = cat != null ? cat.GetStatDefinition(type) : null;
                bool pct = def != null && def.isPercent;

                if (row.Icon != null) row.Icon.sprite = def != null ? def.icon : null;
                if (row.NameText != null) row.NameText.text = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : type.ToString();

                float b = StatPreviewCalculator.PreviewStat(character, tree, null, null, 3, type, cat);
                float r1 = StatPreviewCalculator.PreviewStat(character, tree, build, artifact, 1, type, cat);
                float r2 = StatPreviewCalculator.PreviewStat(character, tree, build, artifact, 2, type, cat);
                float r3 = StatPreviewCalculator.PreviewStat(character, tree, build, artifact, 3, type, cat);

                if (row.BaseValue != null) row.BaseValue.text = Fmt(b, pct);
                if (row.R1 != null) row.R1.text = Fmt(r1, pct);
                if (row.R2 != null) row.R2.text = Fmt(r2, pct);
                if (row.R3 != null) row.R3.text = Fmt(r3, pct);
            }

            for (int i = list.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
        }

        private static string Fmt(float value, bool isPercent) => value.ToString("0.#") + (isPercent ? "%" : string.Empty);
    }
}
