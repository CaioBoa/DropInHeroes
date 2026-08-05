using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Data;
using DropInHeroes.UI;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Painel inferior-central dos picks: personagem em foco → dropdown de build (Padrão + customs do
    /// BuildStore), stats no rank definido (via StatPreviewCalculator), artefato (ícone+nome, hover mostra
    /// o texto via ArtifactTextBuilder) e Confirmar. Reaproveita os componentes da tela de Characters.
    /// </summary>
    public class PickDetailPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text characterNameText;
        [Tooltip("Foto do personagem, abaixo do nome. Usa cardPortrait (fallback defaultSprite/portrait2x1).")]
        [SerializeField] private Image characterPortrait;
        [SerializeField] private TMP_Dropdown buildDropdown;
        [Tooltip("Chip compacto (ícone + valor, hover mostra o nome). Substitui a linha de stat com nome.")]
        [SerializeField] private StatChip statChipPrefab;
        [SerializeField] private Transform statsContainer;
        [Tooltip("Ícone/cor/formato por stat. Se nulo, GameConfig.Active.")]
        [SerializeField] private StatDefinitionCatalog catalog;
        [SerializeField] private StatType[] displayedStats =
        {
            StatType.Attack, StatType.MagicalPower, StatType.Defense, StatType.MagicalDefense,
            StatType.MaxHealth, StatType.Speed, StatType.CritRate, StatType.CritDamage,
            StatType.Lifesteal, StatType.Penetration
        };
        [Header("Skills (0=básico, 1=supremo, 2=passiva)")]
        [Tooltip("Ícones das skills, abaixo dos stats. Cada um requer um SkillTooltipTrigger (hover mostra nome+texto; SHIFT detalha).")]
        [SerializeField] private Image[] skillIcons = new Image[3];

        [Header("Artefato")]
        [SerializeField] private Image artifactIcon;
        [SerializeField] private TMP_Text artifactNameText;
        [Tooltip("Hover do artefato: mesmo AbilityTooltip das skills (nome + grants + efeitos, com keywords e SHIFT). No ícone do artefato.")]
        [SerializeField] private SkillTooltipTrigger artifactTooltipTrigger;
        [SerializeField] private Button confirmButton;
        [SerializeField] private int rank = 3;

        public event Action<CharacterData, CharacterBuild> OnConfirm;

        private CharacterData character;
        private StatTreeData tree;
        private readonly List<CharacterBuild> builds = new List<CharacterBuild>();
        private CharacterBuild current;
        private readonly List<StatChip> chips = new List<StatChip>();

        private StatDefinitionCatalog Catalog
        {
            get { if (catalog == null) catalog = GameConfig.Active?.StatDefinitions; return catalog; }
        }
        private void Awake()
        {
            if (buildDropdown != null) buildDropdown.onValueChanged.AddListener(OnBuildChanged);
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
            Hide();
        }

        private void OnDestroy()
        {
            if (buildDropdown != null) buildDropdown.onValueChanged.RemoveListener(OnBuildChanged);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
        }

        public void Show(CharacterData c, StatTreeData treeData, bool alreadyInTeam)
        {
            character = c;
            tree = treeData;
            if (root != null) root.SetActive(true);
            if (characterNameText != null) characterNameText.text = c != null ? c.displayName : string.Empty;
            BindPortrait(c);
            BindSkills(c);

            builds.Clear();
            if (c != null)
            {
                builds.Add(c.defaultBuild);
                IReadOnlyList<CharacterBuild> customs = BuildStore.GetCustomBuilds(c.ID);
                for (int i = 0; i < customs.Count; i++) builds.Add(customs[i]);
            }
            if (buildDropdown != null)
            {
                buildDropdown.ClearOptions();
                var opts = new List<string>(builds.Count);
                for (int i = 0; i < builds.Count; i++)
                    opts.Add(i == 0 ? "Padrão" : (!string.IsNullOrEmpty(builds[i].label) ? builds[i].label : "Build " + i));
                buildDropdown.AddOptions(opts);
                buildDropdown.SetValueWithoutNotify(0);
                buildDropdown.RefreshShownValue();
            }
            current = builds.Count > 0 ? builds[0] : null;
            Refresh();
            if (confirmButton != null) confirmButton.interactable = c != null && !alreadyInTeam;
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        private void OnBuildChanged(int idx)
        {
            current = idx >= 0 && idx < builds.Count ? builds[idx] : null;
            Refresh();
        }

        private void Refresh()
        {
            StatDefinitionCatalog cat = Catalog;
            ArtifactData artifact = current != null && !string.IsNullOrEmpty(current.artifactId)
                ? DataManager.GetArtifact(current.artifactId) : null;

            for (int i = 0; i < displayedStats.Length; i++)
            {
                StatChip chip;
                if (i < chips.Count) { chip = chips[i]; chip.gameObject.SetActive(true); }
                else { chip = Instantiate(statChipPrefab, statsContainer); chips.Add(chip); }

                StatType t = displayedStats[i];
                StatDefinition def = cat != null ? cat.GetStatDefinition(t) : null;
                bool pct = def != null && def.isPercent;
                string statName = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : t.ToString();
                float val = StatPreviewCalculator.PreviewStat(character, tree, current, artifact, rank, t, cat);
                chip.Set(def != null ? def.icon : null, val.ToString("0.#") + (pct ? "%" : string.Empty), statName);
            }
            for (int i = displayedStats.Length; i < chips.Count; i++) chips[i].gameObject.SetActive(false);

            if (artifactIcon != null) { artifactIcon.sprite = artifact != null ? artifact.icon : null; artifactIcon.enabled = artifact != null && artifact.icon != null; }
            if (artifactNameText != null) artifactNameText.text = artifact != null ? artifact.displayName : "Sem artefato";
            if (artifactTooltipTrigger != null)
                artifactTooltipTrigger.SetSkill(artifact, artifact != null ? artifact.displayName : null,
                    artifact != null ? ArtifactTextBuilder.Header(artifact, cat).TrimEnd() : null);
        }

        private void BindPortrait(CharacterData c)
        {
            if (characterPortrait == null) return;
            Sprite s = c != null
                ? (c.cardPortrait != null ? c.cardPortrait : (c.defaultSprite != null ? c.defaultSprite : c.portrait2x1))
                : null;
            characterPortrait.sprite = s;
            characterPortrait.enabled = s != null;
            characterPortrait.preserveAspect = true;
        }

        private void BindSkills(CharacterData c)
        {
            SetSkill(0, c != null ? c.baseSkill : null, null);
            SetSkill(1, c != null ? c.supremeSkill : null, null);
            SetSkill(2, null, c != null ? c.passiveSkill : null);
        }

        // Mesmo padrão do CharacterInfoPanel: o ícone hospeda um SkillTooltipTrigger (hover→tooltip, SHIFT
        // detalha). Slot sem skill some para o LayoutGroup compactar; a Image fica habilitada (raycast do hover).
        private void SetSkill(int slot, ActiveSkill active, PassiveSkill passive)
        {
            if (skillIcons == null || slot >= skillIcons.Length || skillIcons[slot] == null) return;

            bool has = active != null || passive != null;
            if (skillIcons[slot].gameObject.activeSelf != has) skillIcons[slot].gameObject.SetActive(has);
            if (!has) return;

            skillIcons[slot].sprite = active != null ? active.icon : passive.icon; // null => quadrado branco

            var trigger = skillIcons[slot].GetComponent<SkillTooltipTrigger>();
            if (trigger != null)
            {
                object s = (object)active ?? passive;
                string nm = active != null ? active.displayName : passive.displayName;
                string desc = active != null ? active.description : passive.description;
                trigger.SetSkill(s, nm, desc);
            }
        }

        private void Confirm() { if (character != null) OnConfirm?.Invoke(character, current); }
    }
}
