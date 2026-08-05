using UnityEngine;
using UnityEngine.UI;

namespace DropInHeroes.UI
{
    /// <summary>
    /// Tinge um <see cref="Graphic"/> (Image ou TMP) com um token do <see cref="UITheme"/> e,
    /// opcionalmente, aplica uma moldura 9-slice. Roda em edit-time para preview no editor.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class ThemedGraphic : MonoBehaviour
    {
        [SerializeField] private UIColor color = UIColor.TextPrimary;
        [Tooltip("Mantém o alpha atual do Graphic em vez do alpha do token.")]
        [SerializeField] private bool preserveAlpha = false;

        [Header("Moldura (só Image)")]
        [SerializeField] private UIFrame frame = UIFrame.None;

        private Graphic graphic;

        private void OnEnable() => Apply();
#if UNITY_EDITOR
        private void OnValidate() => Apply();
#endif

        public void Apply()
        {
            if (graphic == null) graphic = GetComponent<Graphic>();
            UITheme theme = UITheme.Active;
            if (theme == null || graphic == null) return;

            Color c = theme.Resolve(color);
            if (preserveAlpha) c.a = graphic.color.a;
            graphic.color = c;

            if (frame != UIFrame.None && graphic is Image img)
            {
                Sprite sprite = theme.GetFrame(frame);
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;
                }
            }
        }
    }
}
