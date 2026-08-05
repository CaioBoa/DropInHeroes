using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropInHeroes.UI
{
    /// <summary>Arranjo do chip: ícone ACIMA do valor (Vertical) ou ícone AO LADO do valor (Horizontal).</summary>
    public enum StatChipOrientation { Vertical, Horizontal }

    /// <summary>
    /// Chip compacto de stat: ícone + valor. O NOME aparece só no hover (balão ligado/desligado por um
    /// <see cref="UIHoverProxy"/> no próprio chip). O arranjo ícone/valor é dinâmico via
    /// <see cref="StatChipOrientation"/>, aplicado por âncoras fracionárias (adapta-se ao tamanho da célula
    /// do grid). Vertical é o layout autorado no prefab; Horizontal (ícone à esquerda) cabe melhor em
    /// células largas e baixas. Preenchido por quem lista.
    /// </summary>
    public class StatChip : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text valueText;
        [Tooltip("Texto do nome do stat, dentro do balão de hover.")]
        [SerializeField] private TMP_Text nameText;
        [Tooltip("Arranjo ícone/valor. Horizontal = ícone ao lado do número (bom p/ células largas e baixas).")]
        [SerializeField] private StatChipOrientation orientation = StatChipOrientation.Vertical;

        private void Awake()
        {
            // Vertical é o layout já autorado no prefab — só re-arranja quando Horizontal, para não mexer
            // nos consumidores que usam o padrão (ex.: tela de picks).
            if (orientation == StatChipOrientation.Horizontal) ApplyOrientation();
        }

        public void Set(Sprite iconSprite, string value, string statName)
        {
            if (icon != null) { icon.sprite = iconSprite; icon.enabled = iconSprite != null; }
            if (valueText != null) valueText.text = value;
            if (nameText != null) nameText.text = statName;
        }

        /// <summary>Atualiza só o valor (ícone/nome são estáticos). Para quem atualiza por frame.</summary>
        public void SetValue(string value)
        {
            if (valueText != null) valueText.text = value;
        }

        /// <summary>Define o arranjo ícone/valor em runtime — o consumidor decide por painel.</summary>
        public void SetOrientation(StatChipOrientation value)
        {
            orientation = value;
            ApplyOrientation();
        }

        // Reposiciona ícone e valor por âncoras fracionárias, então adapta a qualquer tamanho de célula.
        private void ApplyOrientation()
        {
            if (icon == null || valueText == null) return;
            var ir = (RectTransform)icon.transform;
            var vr = (RectTransform)valueText.transform;

            if (orientation == StatChipOrientation.Horizontal)
            {
                Frame(ir, new Vector2(0f, 0f), new Vector2(0.42f, 1f), new Vector2(3f, 3f), new Vector2(-1f, -3f));
                Frame(vr, new Vector2(0.42f, 0f), new Vector2(1f, 1f), new Vector2(3f, 0f), new Vector2(-3f, 0f));
                valueText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            }
            else
            {
                Frame(ir, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(2f, 1f), new Vector2(-2f, -1f));
                Frame(vr, new Vector2(0f, 0f), new Vector2(1f, 0.42f), new Vector2(1f, 1f), new Vector2(-1f, 0f));
                valueText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            }
            valueText.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        private static void Frame(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

#if UNITY_EDITOR
        // Preview no editor: só aplica quando Horizontal (Vertical mantém o layout autorado do prefab intacto).
        private void OnValidate()
        {
            if (orientation == StatChipOrientation.Horizontal) ApplyOrientation();
        }
#endif
    }
}
