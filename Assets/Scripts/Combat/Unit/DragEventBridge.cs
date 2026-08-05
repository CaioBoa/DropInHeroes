using UnityEngine;
using UnityEngine.EventSystems;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ponte entre eventos de drag do Unity e o sistema de módulos
    /// Permite arrastar unidades já posicionadas no board
    /// Pode ser colocado no Model (child) ou no root da Unit
    /// </summary>
    public class DragEventBridge : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private UnitController unitController;

        private void Awake()
        {
            // Buscar UnitController no próprio GameObject
            unitController = GetComponent<UnitController>();

            // Se não encontrar, buscar no pai (para Model child)
            if (unitController == null)
            {
                unitController = GetComponentInParent<UnitController>();
            }

            if (unitController == null)
            {
                DebugManager.LogError("UnitController não encontrado nem no próprio GameObject nem nos pais!", DebugCategory.Drag);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (unitController == null) return;

            DebugManager.Log($"OnBeginDrag para {unitController.GetCharacterData()?.displayName}", DebugCategory.Drag);

            if (PreparationManager.Instance != null)
            {
                PreparationManager.Instance.StartDraggingFromBoard(unitController);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (PreparationManager.Instance != null)
            {
                PreparationManager.Instance.UpdateDragging(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (PreparationManager.Instance != null)
            {
                PreparationManager.Instance.FinishDragging(eventData.position);
            }
        }
    }
}
