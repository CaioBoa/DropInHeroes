using UnityEngine;
using UnityEngine.EventSystems;
using DropInHeroes.Data;
using DropInHeroes.UI;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Torna a unidade selecionável por ponteiro via EventSystem (a Main Camera tem Physics2DRaycaster
    /// e a unidade tem CircleCollider2D). Click fixa a seleção; hover faz preview. Coexiste com
    /// <c>DragEventBridge</c> (o EventSystem separa click de drag automaticamente).
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    public class UnitSelectable : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Anel/destaque world-space sob a unidade, ligado quando ela está selecionada.")]
        [SerializeField] private GameObject selectionRing;

        private UnitController unit;

        private void Awake()
        {
            unit = GetComponent<UnitController>();
            if (selectionRing != null) selectionRing.SetActive(false);
        }

        // Ao voltar ao pool (GameObject desativado), garante que o anel não fique aceso e avisa o
        // manager de seleção para soltar qualquer referência a esta unidade (que será reciclada).
        private void OnDisable()
        {
            if (selectionRing != null) selectionRing.SetActive(false);
            UnitSelectionManager.Instance?.NotifyUnitDespawned(unit);
        }

        public void SetHighlighted(bool on)
        {
            if (selectionRing != null) selectionRing.SetActive(on);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            UnitSelectionManager.Instance?.Click(unit);
        }

        public void OnPointerEnter(PointerEventData eventData)
            => UnitSelectionManager.Instance?.SetHovered(unit);

        public void OnPointerExit(PointerEventData eventData)
            => UnitSelectionManager.Instance?.ClearHovered(unit);
    }
}
