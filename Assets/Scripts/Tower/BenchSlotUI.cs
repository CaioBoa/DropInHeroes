using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// UI de um slot do bench. Reflete o BenchEntry vinculado e dispara
    /// drag para o board reaproveitando PreparationManager.StartDraggingFromRoulette.
    /// </summary>
    public class BenchSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Visual")]
        [SerializeField] private Image portrait;
        [SerializeField] private GameObject emptyPlaceholder;
        [SerializeField] private GameObject deployedOverlay;

        private BenchEntry entry;
        private bool isDragging;

        public BenchEntry Entry => entry;

        public void Bind(BenchEntry boundEntry)
        {
            entry = boundEntry;
            Refresh();
        }

        public void Refresh()
        {
            bool empty = entry == null || entry.IsEmpty;
            bool deployed = entry != null && entry.IsDeployed;

            if (emptyPlaceholder != null) emptyPlaceholder.SetActive(empty);
            if (deployedOverlay != null) deployedOverlay.SetActive(deployed);

            if (portrait != null)
            {
                portrait.gameObject.SetActive(!empty);
                if (!empty) portrait.sprite = entry.Character != null ? entry.Character.cardPortrait : null;
            }
        }

        // === DRAG ===

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag()) return;

            isDragging = true;
            PreparationManager.Instance?.StartDraggingFromRoulette(entry.Character, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            PreparationManager.Instance?.UpdateDragging(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;
            PreparationManager.Instance?.FinishDragging(eventData.position);
            // Refresh acontece pelo evento OnEntryChanged disparado pelo TowerRunController
            // após detectar mudança no board.
        }

        private bool CanDrag()
        {
            return entry != null && !entry.IsEmpty && !entry.IsDeployed && PreparationManager.Instance != null;
        }
    }
}
