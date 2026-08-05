using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DropInHeroes.UI
{
    /// <summary>
    /// Aplica o ColorBlock do <see cref="UITheme"/> a um <see cref="Button"/> (normal/hover/pressed/disabled),
    /// tinge o label e aplica a moldura de botão. Roda em edit-time para preview no editor.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Button))]
    public class ThemedButton : MonoBehaviour
    {
        [Tooltip("Cor do texto do botão.")]
        [SerializeField] private UIColor label = UIColor.ButtonLabel;
        [Tooltip("Aplica a moldura de botão do tema ao targetGraphic.")]
        [SerializeField] private bool applyFrame = true;

        private Button button;

        private void OnEnable() => Apply();
#if UNITY_EDITOR
        private void OnValidate() => Apply();
#endif

        public void Apply()
        {
            if (button == null) button = GetComponent<Button>();
            UITheme theme = UITheme.Active;
            if (theme == null || button == null) return;

            ColorBlock cb = button.colors;
            cb.normalColor = theme.Resolve(UIColor.ButtonNormal);
            cb.highlightedColor = theme.Resolve(UIColor.ButtonHighlighted);
            cb.pressedColor = theme.Resolve(UIColor.ButtonPressed);
            cb.selectedColor = theme.Resolve(UIColor.ButtonHighlighted);
            cb.disabledColor = theme.Resolve(UIColor.ButtonDisabled);
            cb.colorMultiplier = 1f;
            button.colors = cb;

            if (applyFrame && button.targetGraphic is Image img)
            {
                Sprite sprite = theme.GetFrame(UIFrame.Button);
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;
                }
            }

            TMP_Text txt = GetComponentInChildren<TMP_Text>(true);
            if (txt != null) txt.color = theme.Resolve(label);
        }
    }
}
