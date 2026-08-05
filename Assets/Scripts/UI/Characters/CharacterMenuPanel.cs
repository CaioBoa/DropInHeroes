using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Aba Overview da tela de Personagens. Mostra o retrato 2x1, cabeçalho (nome/tipos/lore), a grade
    /// de stats com valor BASE e valor da BUILD (rank 3) lado a lado (via <see cref="StatPreviewCalculator"/>),
    /// as skills com texto COMPLETO (versão shiftada) inline — sem hover — e o quadrado do artefato.
    /// Reaproveita StatDefinitionCatalog + AbilityTextFormatter.
    /// </summary>
    public class CharacterMenuPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Image portrait;            // 2x1 (vertical)
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text loreText;
        [Tooltip("Chips de tipo (0=primário, 1=secundário). Fundo tintado pela cor do tipo.")]
        [SerializeField] private Image[] typeChips = new Image[2];
        [SerializeField] private TMP_Text[] typeChipTexts = new TMP_Text[2];
        [SerializeField] private Image[] typeChipIcons = new Image[2];
        [Tooltip("Ícone por tipo (glyph branco, tintado pelo chip). Tipos sem entrada ficam só com texto.")]
        [SerializeField] private TypeIconEntry[] typeIcons;

        [Header("Stats (base | build rank 3)")]
        [SerializeField] private StatRowView statRowPrefab;
        [SerializeField] private Transform statsContainer;
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

        [Header("Skills (0=base, 1=supreme, 2=passive)")]
        [SerializeField] private Image[] skillIcons = new Image[3];
        [Tooltip("Nome da skill (caixa separada — permite formatação/tamanho próprios).")]
        [SerializeField] private TMP_Text[] skillNames = new TMP_Text[3];
        [SerializeField] private TMP_Text[] skillTexts = new TMP_Text[3];
        [Tooltip("Keywords para o texto rico. Se nulo, GameConfig.Active.")]
        [SerializeField] private KeywordCatalog keywordCatalog;

        [Header("Artefato")]
        [Tooltip("Estado com artefato: ícone + nome + texto do efeito.")]
        [SerializeField] private GameObject artifactFilledRoot;
        [Tooltip("Estado sem artefato: texto 'Sem artefato' ocupando o espaço + botão '+'.")]
        [SerializeField] private GameObject artifactEmptyRoot;
        [SerializeField] private Image artifactSquare;
        [SerializeField] private TMP_Text artifactName;
        [Tooltip("Texto de explicação do efeito do artefato, ao lado do ícone/nome.")]
        [SerializeField] private TMP_Text artifactEffectText;
        [Tooltip("Clique no ícone do artefato (abre o modal de seleção).")]
        [SerializeField] private Button artifactButton;
        [Tooltip("Botão '+' do estado vazio (abre o modal para adicionar).")]
        [SerializeField] private Button artifactAddButton;

        [Header("Empty State")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private GameObject emptyState;

        /// <summary>Disparado ao clicar no quadrado do artefato (controller abre o seletor).</summary>
        public event Action OnArtifactClicked;

        /// <summary>Stats exibidos no Overview — usados também pela comparação (fonte única).</summary>
        public IReadOnlyList<StatType> DisplayedStats => displayedStats;

        private readonly List<StatRowView> rows = new List<StatRowView>();

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
            if (artifactButton != null) artifactButton.onClick.AddListener(() => OnArtifactClicked?.Invoke());
            if (artifactAddButton != null) artifactAddButton.onClick.AddListener(() => OnArtifactClicked?.Invoke());
            Clear();
        }

        private void OnDestroy()
        {
            if (artifactButton != null) artifactButton.onClick.RemoveAllListeners();
            if (artifactAddButton != null) artifactAddButton.onClick.RemoveAllListeners();
        }

        public void Clear()
        {
            if (contentRoot != null) contentRoot.SetActive(false);
            if (emptyState != null) emptyState.SetActive(true);
        }

        /// <summary>Exibe o personagem. build null = só valores base nas duas colunas.</summary>
        public void Display(CharacterData character, StatTreeData tree, CharacterBuild build, ArtifactData artifact, int rank)
        {
            if (character == null) { Clear(); return; }
            if (contentRoot != null) contentRoot.SetActive(true);
            if (emptyState != null) emptyState.SetActive(false);

            if (portrait != null)
            {
                Sprite s = character.portrait2x1 != null ? character.portrait2x1
                    : (character.defaultSprite != null ? character.defaultSprite : character.cardPortrait);
                portrait.sprite = s;
                portrait.enabled = s != null;
                portrait.preserveAspect = true;
            }
            if (nameText != null) nameText.text = character.displayName;
            if (loreText != null) loreText.text = character.lore;
            BindTypeChips(character);

            BuildStatRows(character, tree, build, artifact, rank);
            BindSkills(character);
            BindArtifact(artifact);
        }

        private void BindTypeChips(CharacterData c)
        {
            SetChip(0, c.primaryType);
            SetChip(1, c.secondaryType);
        }

        private void SetChip(int i, CharacterType type)
        {
            if (typeChips == null || i >= typeChips.Length || typeChips[i] == null) return;
            bool show = type != CharacterType.None;
            typeChips[i].gameObject.SetActive(show);
            if (!show) return;
            Color c = TypeColor(type);
            c.a = 0.9f;
            typeChips[i].color = c;
            if (typeChipTexts != null && i < typeChipTexts.Length && typeChipTexts[i] != null)
                typeChipTexts[i].text = type.ToString();
            if (typeChipIcons != null && i < typeChipIcons.Length && typeChipIcons[i] != null)
            {
                Sprite s = FindTypeIcon(type);
                typeChipIcons[i].sprite = s;
                typeChipIcons[i].enabled = s != null;
            }
        }

        private Sprite FindTypeIcon(CharacterType type)
        {
            if (typeIcons != null)
                for (int i = 0; i < typeIcons.Length; i++)
                    if (typeIcons[i].type == type) return typeIcons[i].icon;
            return null;
        }

        // Paleta por tipo (badge). Tons escuros o bastante para texto branco por cima.
        private static Color TypeColor(CharacterType t)
        {
            switch (t)
            {
                case CharacterType.Fairy: return new Color(0.72f, 0.35f, 0.66f);
                case CharacterType.Classic: return new Color(0.42f, 0.48f, 0.60f);
                case CharacterType.Dark: return new Color(0.30f, 0.24f, 0.42f);
                case CharacterType.Hero: return new Color(0.82f, 0.62f, 0.22f);
                case CharacterType.Art: return new Color(0.24f, 0.58f, 0.62f);
                case CharacterType.Chaos: return new Color(0.68f, 0.26f, 0.26f);
                case CharacterType.Chill: return new Color(0.32f, 0.56f, 0.78f);
                case CharacterType.Aura: return new Color(0.46f, 0.66f, 0.40f);
                case CharacterType.Order: return new Color(0.56f, 0.54f, 0.44f);
                case CharacterType.Revolution: return new Color(0.76f, 0.42f, 0.20f);
                case CharacterType.Favela: return new Color(0.28f, 0.62f, 0.52f);
                default: return new Color(0.40f, 0.42f, 0.50f);
            }
        }

        private void BuildStatRows(CharacterData character, StatTreeData tree, CharacterBuild build, ArtifactData artifact, int rank)
        {
            StatDefinitionCatalog cat = Catalog;

            for (int i = 0; i < displayedStats.Length; i++)
            {
                StatRowView row;
                if (i < rows.Count) { row = rows[i]; row.gameObject.SetActive(true); }
                else { row = Instantiate(statRowPrefab, statsContainer); rows.Add(row); }

                StatType type = displayedStats[i];
                StatDefinition def = cat != null ? cat.GetStatDefinition(type) : null;
                bool pct = def != null && def.isPercent;

                if (row.Background != null) row.Background.color = new Color(1f, 1f, 1f, i % 2 == 0 ? 0.02f : 0.07f); // listras
                if (row.Icon != null) row.Icon.sprite = def != null ? def.icon : null;
                if (row.NameText != null) row.NameText.text = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : type.ToString();

                float baseVal = StatPreviewCalculator.PreviewStat(character, tree, null, null, rank, type, cat);
                float buildVal = StatPreviewCalculator.PreviewStat(character, tree, build, artifact, rank, type, cat);

                if (row.BaseValue != null) row.BaseValue.text = Fmt(baseVal, pct);
                if (row.BuildValue != null)
                {
                    row.BuildValue.text = Fmt(buildVal, pct);
                    // Verde quando a build melhora; branco quando igual (sem build → igual à base).
                    row.BuildValue.color = buildVal > baseVal + 0.001f ? new Color(0.45f, 0.85f, 0.5f)
                        : (buildVal < baseVal - 0.001f ? new Color(0.86f, 0.45f, 0.5f) : Color.white);
                }
            }

            for (int i = displayedStats.Length; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
        }

        private static string Fmt(float value, bool isPercent) => value.ToString("0.#") + (isPercent ? "%" : string.Empty);

        private void BindSkills(CharacterData c)
        {
            SetSkill(0, c.baseSkill, null);
            SetSkill(1, c.supremeSkill, null);
            SetSkill(2, null, c.passiveSkill);
        }

        private void SetSkill(int slot, ActiveSkill active, PassiveSkill passive)
        {
            if (skillIcons != null && slot < skillIcons.Length && skillIcons[slot] != null)
            {
                Sprite icon = active != null ? active.icon : (passive != null ? passive.icon : null);
                skillIcons[slot].sprite = icon;
                skillIcons[slot].enabled = icon != null;
            }
            object s = (object)active ?? passive;
            string nm = active != null ? active.displayName : (passive != null ? passive.displayName : string.Empty);
            string desc = active != null ? active.description : (passive != null ? passive.description : string.Empty);

            if (skillNames != null && slot < skillNames.Length && skillNames[slot] != null)
                skillNames[slot].text = nm;
            if (skillTexts != null && slot < skillTexts.Length && skillTexts[slot] != null)
                skillTexts[slot].text = s != null ? BuildSkillText(s, desc) : string.Empty;
        }

        // Texto completo (sempre versão shiftada/detalhada), inline: descrição da skill + linhas de cada efeito.
        private string BuildSkillText(object skill, string desc)
        {
            var sb = new StringBuilder();
            var at = AbilityTextFormatter.Format(desc, skill, Keywords, Catalog, true);
            if (!string.IsNullOrEmpty(at.richText)) sb.Append(at.richText);

            if (skill is ISkillEffectSource src && src.DescribableEffects != null)
            {
                foreach (var fx in src.DescribableEffects)
                {
                    if (fx == null || string.IsNullOrEmpty(fx.description)) continue;
                    var fat = AbilityTextFormatter.Format(fx.description, fx, Keywords, Catalog, true);
                    if (string.IsNullOrEmpty(fat.richText)) continue;
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(fat.richText);
                }
            }
            return sb.ToString();
        }

        private void BindArtifact(ArtifactData artifact)
        {
            bool has = artifact != null;
            if (artifactFilledRoot != null) artifactFilledRoot.SetActive(has);
            if (artifactEmptyRoot != null) artifactEmptyRoot.SetActive(!has);
            if (!has) return;

            if (artifactSquare != null)
            {
                artifactSquare.sprite = artifact.icon;
                artifactSquare.enabled = artifact.icon != null;
            }
            if (artifactName != null) artifactName.text = artifact.displayName;
            if (artifactEffectText != null) artifactEffectText.text = ArtifactTextBuilder.Summary(artifact, Catalog, Keywords);
        }
    }

    /// <summary>Mapeamento tipo → ícone do chip (glyph branco tintável).</summary>
    [Serializable]
    public struct TypeIconEntry
    {
        public CharacterType type;
        public Sprite icon;
    }
}
