using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.UI
{
    /// <summary>
    /// Tooltip de habilidade: nome + descrição rica (via <see cref="AbilityTextFormatter"/>) e, à
    /// direita, a pilha de explicações das keywords usadas. SHIFT alterna a versão detalhada (valores).
    /// Abre acima-à-direita do ícone em hover (alimentado pelo <see cref="SkillTooltipTrigger"/>).
    /// </summary>
    public class AbilityTooltip : MonoBehaviour
    {
        [Header("Catálogos (fallback: GameConfig)")]
        [SerializeField] private KeywordCatalog keywordCatalog;
        [SerializeField] private StatDefinitionCatalog statCatalog;

        [Header("Refs")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Transform keywordStack;          // VerticalLayoutGroup
        [SerializeField] private GameObject keywordItemPrefab;    // filhos "Name" + "Explanation" (TMP)
        [Tooltip("Deslocamento a partir do canto superior-direito do ícone.")]
        [SerializeField] private Vector2 offset = new Vector2(8f, 8f);

        [Header("Caixa")]
        [Tooltip("Largura fixa (ajuste no RectTransform da Box); a caixa cresce para cima conforme o texto.")]
        [SerializeField] private RectTransform box;

        private CanvasGroup group;
        private object skill;
        private string skillName, description;
        private bool shown, lastShift;
        private readonly List<GameObject> items = new List<GameObject>();

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (statCatalog == null) statCatalog = GameConfig.Active?.StatDefinitions;
            if (keywordCatalog == null) keywordCatalog = GameConfig.Active?.Keywords;
            SetVisible(false);
        }

        public void Show(string name, string description, object skill, RectTransform anchor)
        {
            skillName = name;
            this.description = description;
            this.skill = skill;
            shown = true;
            lastShift = ShiftHeld();
            Render();
            Position(anchor);
            SetVisible(true);
        }

        public void Hide()
        {
            shown = false;
            SetVisible(false);
        }

        private void Update()
        {
            if (!shown) return;
            bool sh = ShiftHeld();
            if (sh != lastShift) { lastShift = sh; Render(); }
        }

        private static bool ShiftHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }

        private void Render()
        {
            // Aloca por render (acontece em hover/SHIFT, não por frame).
            var sb = new StringBuilder();
            var kws = new List<KeywordEntry>();
            var seen = new HashSet<string>();

            // Descrição da skill (reflete a própria skill), + descrição de cada contact effect
            // (refletindo o efeito — onde moram os valores como chance/duração).
            AppendFormatted(sb, kws, seen, description, skill, false);
            if (skill is ISkillEffectSource effects)
                foreach (var fx in effects.DescribableEffects)
                    if (fx != null && !string.IsNullOrEmpty(fx.description))
                        AppendFormatted(sb, kws, seen, fx.description, fx, true);

            if (nameText != null) nameText.text = skillName;
            if (descText != null) descText.text = sb.ToString();
            BuildKeywordItems(kws);
            FitBox();
        }

        private void AppendFormatted(StringBuilder sb, List<KeywordEntry> kws, HashSet<string> seen,
                                     string template, object reflectTarget, bool newline)
        {
            if (string.IsNullOrEmpty(template)) return;
            var at = AbilityTextFormatter.Format(template, reflectTarget, keywordCatalog, statCatalog, lastShift);
            if (string.IsNullOrEmpty(at.richText)) return; // ex.: descrição só-SHIFT no modo normal — não cria linha vazia
            if (newline && sb.Length > 0) sb.Append('\n');
            sb.Append(at.richText);
            if (at.keywords != null)
                for (int i = 0; i < at.keywords.Count; i++)
                    if (seen.Add(at.keywords[i].key)) kws.Add(at.keywords[i]);
        }

        // Largura fixa (definida no editor); a caixa cresce para cima conforme o texto (pivot 0,0 +
        // ContentSizeFitter vertical). Só força o recálculo imediato da altura ao mostrar/alternar SHIFT.
        private void FitBox()
        {
            if (box != null) LayoutRebuilder.ForceRebuildLayoutImmediate(box);
        }

        private void BuildKeywordItems(List<KeywordEntry> keywords)
        {
            int n = keywords != null ? keywords.Count : 0;
            for (int i = 0; i < items.Count; i++) items[i].SetActive(i < n);
            for (int i = 0; i < n; i++)
            {
                if (i >= items.Count) items.Add(Instantiate(keywordItemPrefab, keywordStack));
                var go = items[i];
                go.SetActive(true);
                var e = keywords[i];
                var nm = go.transform.Find("Name").GetComponent<TMP_Text>();
                var ex = go.transform.Find("Explanation").GetComponent<TMP_Text>();
                if (nm != null) { nm.text = e.Display; nm.color = KeywordDisplayColor(e); }
                if (ex != null) ex.text = e.explanation;
            }
            if (keywordStack != null) keywordStack.gameObject.SetActive(n > 0);
        }

        private Color KeywordDisplayColor(KeywordEntry e)
        {
            if (e.useStatColor && statCatalog != null)
            {
                var def = statCatalog.GetStatDefinition(e.statColor);
                if (def != null && def.TintColor.HasValue) return def.TintColor.Value;
            }
            return e.hasColor ? e.color : Color.white;
        }

        // Posiciona o canto inferior-esquerdo do tooltip no canto superior-direito do ícone (cresce p/ cima-direita).
        private void Position(RectTransform anchor)
        {
            if (anchor == null) return;
            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners); // 0=BL,1=TL,2=TR,3=BR
            ((RectTransform)transform).position = corners[2] + new Vector3(offset.x, offset.y, 0f);
        }

        private void SetVisible(bool on)
        {
            if (group == null) return;
            group.alpha = on ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false; // não intercepta o ponteiro (o hover do ícone segue válido)
        }
    }
}
