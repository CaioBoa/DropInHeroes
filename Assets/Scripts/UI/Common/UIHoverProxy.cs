using UnityEngine;
using UnityEngine.EventSystems;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Mostra/oculta um alvo enquanto o mouse está sobre este elemento. Reutilizável — ex.: tooltip do
    /// texto do artefato nos picks. Coloque no elemento com raycast (ícone) e ligue <see cref="target"/>.
    /// </summary>
    public class UIHoverProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject target;
        [SerializeField] private bool hoverEnabled = true;

        public void SetEnabled(bool on)
        {
            hoverEnabled = on;
            if (!on && target != null) target.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData e) { if (hoverEnabled && target != null) target.SetActive(true); }
        public void OnPointerExit(PointerEventData e) { if (target != null) target.SetActive(false); }

        private void OnDisable() { if (target != null) target.SetActive(false); }
    }
}
