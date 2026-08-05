using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.UI
{
    /// <summary>
    /// Painel CharacterInfo (canto inferior-esquerdo, estilo RPG de turno). Para a unidade selecionada
    /// (via <see cref="UnitSelectionManager"/>), atualiza retrato, nome, rank em estrelas, barras de
    /// Vida/Energia, ícones de habilidades, artefato e status. Os stats vêm de DUAS listas dinâmicas —
    /// principais (sempre visíveis) e adicionais (revelados ao segurar SHIFT, numa seção que abre no topo
    /// e faz o painel crescer para cima) — renderizados como <see cref="StatChip"/> (ícone+valor, o nome
    /// aparece no hover). Esconde-se via CanvasGroup quando nada está selecionado.
    /// </summary>
    public class CharacterInfoPanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image[] starImages;
        [SerializeField] private UIBar hpBar;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private UIBar energyBar;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private Image[] skillIcons;
        [SerializeField] private Transform statusContainer;
        [SerializeField] private GameObject statusIconPrefab;
        [Tooltip("Tooltip compartilhado (o mesmo das skills). Hover num pip de status mostra nome + descrição.")]
        [SerializeField] private AbilityTooltip statusTooltip;

        [Header("Artefato")]
        [Tooltip("Ícone do artefato equipado (canto superior direito). Some se a unidade não tem artefato.")]
        [SerializeField] private Image artifactIcon;
        [Tooltip("Hover do artefato: mesmo AbilityTooltip das skills (nome + grants + efeitos, com SHIFT).")]
        [SerializeField] private SkillTooltipTrigger artifactTooltipTrigger;

        [Header("Stats (listas dinâmicas + StatChip)")]
        [Tooltip("Catálogo: ícone, cor e formato (%) por stat.")]
        [SerializeField] private StatDefinitionCatalog catalog;
        [Tooltip("Prefab do chip (ícone + valor; hover mostra o nome). Mesmo StatChip da tela de picks.")]
        [SerializeField] private StatChip statChipPrefab;
        [Tooltip("Arranjo dos chips. Horizontal (ícone ao lado do número) cabe melhor nas células largas do painel.")]
        [SerializeField] private StatChipOrientation chipOrientation = StatChipOrientation.Horizontal;
        [Tooltip("Container dos stats principais (sempre visíveis).")]
        [SerializeField] private Transform defaultStatsContainer;
        [Tooltip("Stats principais, sempre visíveis.")]
        [SerializeField] private StatType[] defaultStats =
        {
            StatType.Attack, StatType.MagicalPower, StatType.Defense, StatType.MagicalDefense,
            StatType.CritRate, StatType.CritDamage, StatType.EnergyRegeneration, StatType.Lifesteal
        };
        [Tooltip("Seção que abre no topo do painel ao segurar SHIFT (o painel cresce para cima).")]
        [SerializeField] private GameObject shiftSection;
        [Tooltip("Container dos stats adicionais (dentro da shiftSection).")]
        [SerializeField] private Transform shiftStatsContainer;
        [Tooltip("Stats adicionais, revelados ao segurar SHIFT.")]
        [SerializeField] private StatType[] shiftStats =
        {
            StatType.Speed, StatType.Penetration, StatType.MagicalPenetration, StatType.Accuracy,
            StatType.Dodge, StatType.Effectiveness, StatType.Tenacity, StatType.Control,
            StatType.DamageBonus, StatType.DamageReduction
        };
        [Tooltip("Espaço (px) entre a aba do SHIFT e a box de status quando ela sobe para ficar acima da aba.")]
        [SerializeField] private float statusShiftMargin = 8f;

        [Header("Cores (tint em runtime)")]
        [SerializeField] private Color starFilled = new Color(1f, 0.84f, 0.2f, 1f);
        [SerializeField] private Color starEmpty = new Color(1f, 1f, 1f, 0.15f);
        [Header("Cores de status por categoria (caixa)")]
        [SerializeField] private Color statusPositive = new Color(0.42f, 0.78f, 0.5f, 1f);
        [SerializeField] private Color statusNegative = new Color(0.86f, 0.4f, 0.46f, 1f);
        [SerializeField] private Color statusNeutral = new Color(0.45f, 0.6f, 0.85f, 1f);
        [SerializeField] private Color supremeReady = new Color(1f, 0.86f, 0.3f, 1f);

        private CanvasGroup group;
        private RectTransform statusRect;   // a box de status (statusContainer) — reposicionada no SHIFT
        private RectTransform shiftRect;    // a aba do SHIFT — sua altura define o quanto a box sobe
        private float statusDefaultY;       // y de repouso da box (sem SHIFT)
        private readonly List<StatusItem> statusItems = new List<StatusItem>();
        private readonly List<StatChipEntry> defaultChips = new List<StatChipEntry>();
        private readonly List<StatChipEntry> shiftChips = new List<StatChipEntry>();
        private UnitController shown;
        private bool subscribed;
        private bool lastShift;

        private class StatusItem { public GameObject go; public Image box; public Image icon; public TMP_Text number; public SkillTooltipTrigger trigger; }
        private struct StatChipEntry { public StatChip chip; public StatType type; public bool isPercent; }

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            statusRect = statusContainer as RectTransform;
            if (statusRect != null)
            {
                statusDefaultY = statusRect.anchoredPosition.y;
                // A box de status deve ficar SEMPRE acima da aba do SHIFT (fundo opaco, irmã posterior):
                // como último irmão do painel, renderiza por cima dela em vez de ser encoberta.
                statusRect.SetAsLastSibling();
            }
            if (shiftSection != null) shiftRect = shiftSection.GetComponent<RectTransform>();
            BuildChips(defaultStats, defaultStatsContainer, defaultChips);
            BuildChips(shiftStats, shiftStatsContainer, shiftChips);
            if (shiftSection != null) shiftSection.SetActive(false);
            SetVisible(false);
        }

        private void OnEnable() => TrySubscribe();

        // Start roda após todos os Awake: garante a inscrição mesmo se o OnEnable do painel
        // rodar antes do Awake do UnitSelectionManager (ordem entre objetos é indefinida).
        private void Start() => TrySubscribe();

        private void TrySubscribe()
        {
            if (subscribed || UnitSelectionManager.Instance == null) return;
            UnitSelectionManager.Instance.OnSelectionChanged += OnSelectionChanged;
            subscribed = true;
            OnSelectionChanged(UnitSelectionManager.Instance.Current); // sincroniza seleção já existente
        }

        private void OnDisable()
        {
            if (subscribed && UnitSelectionManager.Instance != null)
                UnitSelectionManager.Instance.OnSelectionChanged -= OnSelectionChanged;
            subscribed = false;
        }

        private void OnSelectionChanged(UnitController unit)
        {
            shown = unit;
            if (unit == null) { SetVisible(false); return; }
            SetVisible(true);
            PopulateStatic(unit);
        }

        private void Update()
        {
            if (shown == null) return;
            if (!shown.gameObject.activeInHierarchy) { shown = null; SetVisible(false); return; }
            UpdateShift();
            RefreshDynamic(shown);
        }

        private void SetVisible(bool on)
        {
            group.alpha = on ? 1f : 0f;
            group.interactable = on;
            group.blocksRaycasts = on;
            if (!on)
            {
                lastShift = false;
                if (shiftSection != null) shiftSection.SetActive(false);
                PositionStatusForShift(false);
            }
        }

        // SHIFT revela a seção de stats adicionais no topo — a seção fica ancorada acima do conteúdo,
        // então o painel "cresce" para cima. A box de status subiria coberta pela aba, então a movemos
        // para acima da aba enquanto o SHIFT estiver ativo (ver PositionStatusForShift).
        private void UpdateShift()
        {
            bool shift = ShiftHeld();
            if (shift == lastShift) return;
            lastShift = shift;
            if (shiftSection != null) shiftSection.SetActive(shift);
            PositionStatusForShift(shift);
        }

        // Com SHIFT, a box de status sobe para acima da aba (altura real da aba + margem); sem SHIFT,
        // volta ao repouso. Assim os status ficam sempre visíveis, acima da aba adicional quando aberta.
        private void PositionStatusForShift(bool shift)
        {
            if (statusRect == null) return;
            float y = statusDefaultY;
            if (shift && shiftRect != null)
                y = statusDefaultY + shiftRect.rect.height + statusShiftMargin;
            Vector2 p = statusRect.anchoredPosition;
            p.y = y;
            statusRect.anchoredPosition = p;
        }

        private static bool ShiftHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }

        // === POPULATE ===

        private void PopulateStatic(UnitController unit)
        {
            var data = unit.GetCharacterData();
            nameText.text = data != null ? data.displayName : unit.name;

            Sprite p = data != null ? data.cardPortrait : null;
            portrait.sprite = p;
            portrait.color = p != null ? Color.white : new Color(1f, 1f, 1f, 0.12f);

            for (int i = 0; i < starImages.Length; i++)
                starImages[i].color = i < unit.Rank ? starFilled : starEmpty;

            // Dados de exibição (ícone/nome/descrição) vêm dos assets em CharacterData — sempre
            // disponíveis (inclusive na preparação). Os clones de runtime do SkillsModule só existem
            // em combate; o estado runtime (ex.: supremo pronto) é lido à parte em RefreshDynamic.
            SetSkill(0, data != null ? data.baseSkill : null, null);          // básico
            SetSkill(1, null, data != null ? data.passiveSkill : null);       // passiva (meio)
            SetSkill(2, data != null ? data.supremeSkill : null, null);       // supremo (último)

            BindArtifact(unit);
        }

        private void SetSkill(int slot, ActiveSkill active, PassiveSkill passive)
        {
            if (skillIcons[slot] == null) return;

            // Unidade sem a habilidade (ex.: invocação só com ataque) não mostra o quadrado — o slot
            // some e o HorizontalLayoutGroup do container compacta os restantes.
            bool has = active != null || passive != null;
            if (skillIcons[slot].gameObject.activeSelf != has)
                skillIcons[slot].gameObject.SetActive(has);
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

        // Ícone do artefato + hover pela mesma pipeline das skills (ArtifactData é ISkillEffectSource;
        // Header = descrição + grants; os efeitos vêm da interface, com keywords/SHIFT).
        private void BindArtifact(UnitController unit)
        {
            ArtifactData art = ResolveArtifact(unit);
            bool has = art != null;
            if (artifactIcon != null)
            {
                if (artifactIcon.gameObject.activeSelf != has) artifactIcon.gameObject.SetActive(has);
                if (has) { artifactIcon.sprite = art.icon; artifactIcon.enabled = art.icon != null; }
            }
            if (artifactTooltipTrigger != null)
                artifactTooltipTrigger.SetSkill(art, has ? art.displayName : null,
                    has ? ArtifactTextBuilder.Header(art, catalog).TrimEnd() : null);
        }

        private static ArtifactData ResolveArtifact(UnitController unit)
        {
            LoadoutModule loadout = unit.GetModule<LoadoutModule>();
            if (loadout == null) return null;
            if (loadout.Artifact != null) return loadout.Artifact;
            CharacterBuild b = loadout.Build;
            return b != null && !string.IsNullOrEmpty(b.artifactId) ? DataManager.GetArtifact(b.artifactId) : null;
        }

        private void RefreshDynamic(UnitController unit)
        {
            var stats = unit.GetModule<StatsModule>();
            if (stats != null)
            {
                float hp = stats.CurrentHealth, hpMax = stats.MaxHealth;
                var shieldModule = unit.GetModule<ShieldModule>();
                float shieldAmt = shieldModule != null ? shieldModule.TotalShield : 0f;
                float hpTotal = hpMax + shieldAmt; // reescala: total = maxHP + escudo (ver HealthBar)
                hpBar.SetValue(hpTotal > 0f ? hp / hpTotal : 0f,
                               hpTotal > 0f ? (hp + shieldAmt) / hpTotal : 0f);
                hpText.text = Mathf.CeilToInt(hp) + "/" + Mathf.CeilToInt(hpMax);

                float en = stats.CurrentEnergy, enMax = stats.MaxEnergy;
                energyBar.SetValue(enMax > 0f ? en / enMax : 0f);
                energyText.text = enMax > 0f ? (Mathf.CeilToInt(en) + "/" + Mathf.CeilToInt(enMax)) : "—";

                UpdateChipValues(defaultChips, stats);
                if (lastShift) UpdateChipValues(shiftChips, stats); // adicionais só atualizam enquanto visíveis
            }

            var skills = unit.GetModule<SkillsModule>();
            bool ready = skills != null && skills.IsSupremeReady;
            if (skillIcons[2] != null && skillIcons[2].gameObject.activeSelf)
                skillIcons[2].color = ready ? supremeReady : Color.white; // supremo é o slot 2 (se existir)

            RefreshStatus(unit.GetModule<StatusModule>());
        }

        private void UpdateChipValues(List<StatChipEntry> chips, StatsModule stats)
        {
            for (int i = 0; i < chips.Count; i++)
                chips[i].chip.SetValue(FormatStat(stats.GetStat(chips[i].type), chips[i].isPercent));
        }

        private static string FormatStat(float v, bool isPercent)
        {
            // Stats percentuais já estão em base 100 (100 = 100%): só anexa "%", sem multiplicar.
            if (isPercent) return v.ToString("0.#") + "%";
            return v.ToString("0.#");
        }

        // Instancia os chips (ícone + nome no hover) UMA vez; o valor é atualizado por frame em RefreshDynamic.
        private void BuildChips(StatType[] stats, Transform container, List<StatChipEntry> into)
        {
            into.Clear();
            if (stats == null || container == null || statChipPrefab == null) return;
            for (int i = 0; i < stats.Length; i++)
            {
                StatType t = stats[i];
                StatDefinition def = catalog != null ? catalog.GetStatDefinition(t) : null;
                string nm = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : t.ToString();
                StatChip chip = Instantiate(statChipPrefab, container);
                chip.SetOrientation(chipOrientation);
                chip.Set(def != null ? def.icon : null, string.Empty, nm);
                into.Add(new StatChipEntry { chip = chip, type = t, isPercent = def != null && def.isPercent });
            }
        }

        private void RefreshStatus(StatusModule status)
        {
            var effects = status != null ? status.ActiveEffects : null;
            int n = effects != null ? effects.Count : 0;

            for (int i = 0; i < statusItems.Count; i++)
                statusItems[i].go.SetActive(i < n);

            for (int i = 0; i < n; i++)
            {
                if (i >= statusItems.Count) statusItems.Add(CreateStatusItem());
                var item = statusItems[i];
                var fx = effects[i];
                item.go.SetActive(true);
                item.box.color = CategoryColor(fx.Kind);
                var sprite = StatusVisuals.ResolveIcon(fx, catalog);
                item.icon.sprite = sprite;
                item.icon.enabled = sprite != null; // sem sprite => mostra só a caixa colorida
                item.number.text = StatusVisuals.IndicatorText(fx);
                if (item.trigger != null)
                    item.trigger.SetSkill(fx, StatusVisuals.DisplayName(fx), StatusVisuals.Description(fx), statusTooltip);
            }
        }

        private Color CategoryColor(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Negative: return statusNegative;
                case StatusKind.Neutral: return statusNeutral;
                default: return statusPositive;
            }
        }

        // Instancia um item de status do prefab autorado sob o container (o LayoutGroup posiciona).
        private StatusItem CreateStatusItem()
        {
            var go = Instantiate(statusIconPrefab, statusContainer);
            return new StatusItem
            {
                go = go,
                box = go.GetComponent<Image>(),
                icon = go.transform.Find("Icon").GetComponent<Image>(),
                number = go.transform.Find("Number").GetComponent<TMP_Text>(),
                trigger = go.GetComponent<SkillTooltipTrigger>()
            };
        }
    }
}
