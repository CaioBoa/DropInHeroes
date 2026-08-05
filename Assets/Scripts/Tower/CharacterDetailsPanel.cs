using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.UI;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Painel inferior-direito com detalhes do personagem em foco
    /// (não confundir com escolhido — apenas o último clicado).
    /// Stats exibidos como células ícone+valor via <see cref="StatDefinitionCatalog"/> (mesmo
    /// catálogo do HUD de combate) — coesão de ícone/cor/formato vem do catálogo.
    /// </summary>
    public class CharacterDetailsPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Image fullPortrait;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typesText;

        [Header("Stats")]
        [SerializeField] private Transform statsContainer;
        [SerializeField] private StatCellView statCellPrefab;
        [Tooltip("Catálogo de ícone/cor/formato por stat. Se nulo, GameConfig.Active.")]
        [SerializeField] private StatDefinitionCatalog catalog;
        [SerializeField] private StatType[] displayedStats = new[]
        {
            StatType.Attack,
            StatType.Defense,
            StatType.MaxHealth,
            StatType.Speed,
            StatType.Range,
            StatType.MaxEnergy,
            StatType.EnergyRegeneration,
            StatType.CritRate,
            StatType.CritDamage
        };

        [Header("Skills")]
        [SerializeField] private TextMeshProUGUI baseSkillText;
        [SerializeField] private TextMeshProUGUI supremeSkillText;
        [SerializeField] private TextMeshProUGUI passiveSkillText;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject contentRoot;

        private readonly List<StatCellView> spawnedCells = new List<StatCellView>();

        private StatDefinitionCatalog Catalog
        {
            get
            {
                if (catalog == null) catalog = GameConfig.Active?.StatDefinitions;
                return catalog;
            }
        }

        private void Awake()
        {
            Display(null);
        }

        public void Display(CharacterData data)
        {
            bool hasData = data != null;

            if (emptyState != null) emptyState.SetActive(!hasData);
            if (contentRoot != null) contentRoot.SetActive(hasData);
            if (!hasData) return;

            if (fullPortrait != null) fullPortrait.sprite = data.cardPortrait;
            if (nameText != null) nameText.text = data.displayName;

            if (typesText != null)
            {
                string types = data.primaryType.ToString();
                if (data.secondaryType != CharacterType.None)
                    types += " / " + data.secondaryType.ToString();
                typesText.text = types;
            }

            BuildStatCells(data);
            BuildSkillTexts(data);
        }

        private void BuildStatCells(CharacterData data)
        {
            StatDefinitionCatalog cat = Catalog;

            // Reaproveita células já instanciadas — instancia novas só se faltarem.
            for (int i = 0; i < displayedStats.Length; i++)
            {
                StatCellView cell;
                if (i < spawnedCells.Count)
                {
                    cell = spawnedCells[i];
                    cell.gameObject.SetActive(true);
                }
                else
                {
                    cell = Instantiate(statCellPrefab, statsContainer);
                    spawnedCells.Add(cell);
                }

                StatType type = displayedStats[i];
                StatDefinition def = cat != null ? cat.GetStatDefinition(type) : null;

                if (cell.Icon != null) cell.Icon.sprite = def != null ? def.icon : null;
                if (cell.Value != null)
                {
                    if (def != null && def.TintColor.HasValue) cell.Value.color = def.TintColor.Value;
                    cell.Value.text = FormatStat(StatBudget.ComputeBaseStat(data, type), def != null && def.isPercent);
                }
            }

            for (int i = displayedStats.Length; i < spawnedCells.Count; i++)
            {
                spawnedCells[i].gameObject.SetActive(false);
            }
        }

        private static string FormatStat(float value, bool isPercent)
        {
            // Stats percentuais já estão em base 100 (100 = 100%): só anexa "%", sem multiplicar.
            return value.ToString("0.#") + (isPercent ? "%" : string.Empty);
        }

        private void BuildSkillTexts(CharacterData data)
        {
            if (baseSkillText != null) baseSkillText.text = FormatSkill(data.baseSkill);
            if (supremeSkillText != null) supremeSkillText.text = FormatSkill(data.supremeSkill);
            if (passiveSkillText != null) passiveSkillText.text = FormatSkill(data.passiveSkill);
        }

        private static string FormatSkill(ActiveSkill skill)
        {
            if (skill == null) return "—";
            return $"<b>{skill.displayName}</b>\n{skill.description}";
        }

        private static string FormatSkill(PassiveSkill skill)
        {
            if (skill == null) return "—";
            return $"<b>{skill.displayName}</b>\n{skill.description}";
        }
    }
}
