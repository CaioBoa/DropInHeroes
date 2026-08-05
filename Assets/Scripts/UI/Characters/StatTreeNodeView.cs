using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Um nó da árvore: fundo colorido pelo RANK, ícone do stat, anel ornamental (notables) e glow
    /// quando alocado. É um botão — clicar alterna ativar/desativar (binário). O CENTRO é uma âncora
    /// decorativa: sem ícone de stat, sem stats, não interativo (mostra o emblema, se fornecido).
    /// Alocar dispara um punch-scale sutil. Posicionado/dimensionado pelo <see cref="StatTreeView"/>.
    /// </summary>
    public class StatTreeNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Image ring;
        [SerializeField] private Image glow;
        [SerializeField] private Image icon;
        [SerializeField] private Button button;

        private string nodeId;
        private Action<string> onToggle;
        private Action<string, bool> onHover;
        private bool wasAllocated;
        private Coroutine punch;

        public RectTransform Rect => (RectTransform)transform;

        public void Setup(TreeNode node, StatDefinition def, Action<string> toggleHandler, Action<string, bool> hoverHandler, Sprite centerEmblem, bool isCenter)
        {
            nodeId = node.nodeId;
            onToggle = toggleHandler;
            onHover = hoverHandler;

            if (icon != null)
            {
                // Centro é decorativo: nunca mostra ícone de stat.
                Sprite s = isCenter ? null : (def != null ? def.icon : null);
                icon.sprite = s;
                icon.enabled = s != null;
            }
            if (background != null && isCenter && centerEmblem != null)
                background.sprite = centerEmblem;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onToggle?.Invoke(nodeId));
            }
        }

        public void Refresh(bool allocated, bool interactable, bool unlockable, bool isCenter, bool isNotable, Color rankColor)
        {
            if (background != null)
            {
                Color c;
                if (isCenter) c = new Color(1f, 0.85f, 0.45f);
                else if (allocated) c = Color.Lerp(rankColor, Color.white, 0.55f);
                else if (unlockable) c = rankColor;
                else c = rankColor * 0.45f; // bloqueado: escuro mas legível
                c.a = 1f;
                background.color = c;
            }

            if (ring != null)
            {
                ring.enabled = isNotable || isCenter;
                Color rc = isCenter ? new Color(1f, 0.85f, 0.45f) : Color.Lerp(rankColor, Color.white, allocated ? 0.7f : 0.25f);
                rc.a = 1f;
                ring.color = rc;
            }

            if (glow != null)
            {
                glow.enabled = allocated && !isCenter;
                Color gc = Color.Lerp(rankColor, Color.white, 0.4f);
                gc.a = 0.6f;
                glow.color = gc;
            }

            if (button != null) button.interactable = !isCenter && interactable;

            // Punch sutil ao ALOCAR (não ao desalocar/refresh).
            if (allocated && !wasAllocated && gameObject.activeInHierarchy)
            {
                if (punch != null) StopCoroutine(punch);
                punch = StartCoroutine(Punch());
            }
            wasAllocated = allocated;
        }

        private IEnumerator Punch()
        {
            const float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                transform.localScale = new Vector3(k, k, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;
            punch = null;
        }

        public void OnPointerEnter(PointerEventData e) => onHover?.Invoke(nodeId, true);
        public void OnPointerExit(PointerEventData e) => onHover?.Invoke(nodeId, false);

        private void OnDisable()
        {
            transform.localScale = Vector3.one;
            punch = null;
            onHover?.Invoke(nodeId, false);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }
    }
}
