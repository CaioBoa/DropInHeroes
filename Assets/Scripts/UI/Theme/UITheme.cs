using UnityEngine;
using TMPro;
using DropInHeroes.Data;

namespace DropInHeroes.UI
{
    /// <summary>Token de cor do tema. Resolvido para uma <see cref="Color"/> concreta pelo <see cref="UITheme"/>.</summary>
    public enum UIColor
    {
        PanelBg,
        PanelBgDark,
        FrameTint,
        ButtonNormal,
        ButtonHighlighted,
        ButtonPressed,
        ButtonDisabled,
        ButtonLabel,
        TextPrimary,
        TextSecondary,
        TextMuted,
        Accent,
        Positive,
        Negative,
        Energy,
        SlotEmpty,
        Overlay
    }

    /// <summary>Moldura 9-slice do tema, aplicada por appliers a <see cref="UnityEngine.UI.Image"/>.</summary>
    public enum UIFrame
    {
        None,
        Panel,
        Button,
        BarFrame,
        BarFill
    }

    /// <summary>
    /// Fonte única de verdade da identidade visual da UI (cores, tipografia, espaçamento, molduras).
    /// Componentes appliers (<see cref="ThemedGraphic"/>, <see cref="ThemedButton"/>) leem estes tokens
    /// e tingem os elementos, de modo que a coesão de cor vem do tema — não da arte.
    /// Instância ativa carregada de Resources/UITheme.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "Game/UI/Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Superfícies")]
        [SerializeField] private Color panelBg = new Color(0.42f, 0.30f, 0.22f, 1f);
        [SerializeField] private Color panelBgDark = new Color(0.26f, 0.18f, 0.13f, 1f);
        [SerializeField] private Color frameTint = new Color(0.58f, 0.43f, 0.30f, 1f);
        [SerializeField] private Color slotEmpty = new Color(0.20f, 0.14f, 0.10f, 1f);
        [SerializeField] private Color overlay = new Color(0f, 0f, 0f, 0.6f);

        [Header("Botão (color transition)")]
        [SerializeField] private Color buttonNormal = new Color(0.92f, 0.55f, 0.66f, 1f);
        [SerializeField] private Color buttonHighlighted = new Color(0.98f, 0.66f, 0.76f, 1f);
        [SerializeField] private Color buttonPressed = new Color(0.70f, 0.38f, 0.50f, 1f);
        [SerializeField] private Color buttonDisabled = new Color(0.50f, 0.42f, 0.44f, 1f);
        [SerializeField] private Color buttonLabel = new Color(0.17f, 0.08f, 0.11f, 1f);

        [Header("Texto")]
        [SerializeField] private Color textPrimary = new Color(0.96f, 0.93f, 0.88f, 1f);
        [SerializeField] private Color textSecondary = new Color(0.80f, 0.74f, 0.66f, 1f);
        [SerializeField] private Color textMuted = new Color(0.58f, 0.52f, 0.46f, 1f);

        [Header("Acentos / semânticos")]
        [SerializeField] private Color accent = new Color(0.95f, 0.80f, 0.35f, 1f);
        [SerializeField] private Color positive = new Color(0.45f, 0.80f, 0.40f, 1f);
        [SerializeField] private Color negative = new Color(0.86f, 0.36f, 0.36f, 1f);
        [SerializeField] private Color energy = new Color(0.40f, 0.70f, 0.95f, 1f);

        [Header("Tipografia")]
        [Tooltip("Fonte de títulos. Se nula, mantém a fonte existente do TMP.")]
        public TMP_FontAsset titleFont;
        [Tooltip("Fonte de corpo. Se nula, mantém a fonte existente do TMP.")]
        public TMP_FontAsset bodyFont;
        [Tooltip("Tamanhos em espaço 1080p.")]
        public float titleSize = 48f;
        public float headingSize = 32f;
        public float bodySize = 24f;
        public float captionSize = 18f;

        [Header("Espaçamento (px em 1080p)")]
        public float paddingLarge = 32f;
        public float paddingMedium = 16f;
        public float paddingSmall = 8f;
        public float gutter = 24f;

        [Header("Molduras 9-slice")]
        public Sprite panelFrame;
        public Sprite buttonFrame;
        public Sprite barFrame;
        public Sprite barFill;

        public Color Resolve(UIColor token)
        {
            switch (token)
            {
                case UIColor.PanelBg: return panelBg;
                case UIColor.PanelBgDark: return panelBgDark;
                case UIColor.FrameTint: return frameTint;
                case UIColor.ButtonNormal: return buttonNormal;
                case UIColor.ButtonHighlighted: return buttonHighlighted;
                case UIColor.ButtonPressed: return buttonPressed;
                case UIColor.ButtonDisabled: return buttonDisabled;
                case UIColor.ButtonLabel: return buttonLabel;
                case UIColor.TextPrimary: return textPrimary;
                case UIColor.TextSecondary: return textSecondary;
                case UIColor.TextMuted: return textMuted;
                case UIColor.Accent: return accent;
                case UIColor.Positive: return positive;
                case UIColor.Negative: return negative;
                case UIColor.Energy: return energy;
                case UIColor.SlotEmpty: return slotEmpty;
                case UIColor.Overlay: return overlay;
                default: return Color.magenta;
            }
        }

        public Sprite GetFrame(UIFrame frame)
        {
            switch (frame)
            {
                case UIFrame.Panel: return panelFrame;
                case UIFrame.Button: return buttonFrame;
                case UIFrame.BarFrame: return barFrame;
                case UIFrame.BarFill: return barFill;
                default: return null;
            }
        }

        /// <summary>Tema ativo, resolvido via <see cref="GameConfig.Active"/> (edit-time e runtime).</summary>
        public static UITheme Active => GameConfig.Active != null ? GameConfig.Active.Theme : null;
    }
}
