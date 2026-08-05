using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Controla feedback visual durante operações de drag
    /// Responsabilidade: atualização de estados visuais (grid, footprints)
    /// </summary>
    public class UIFeedbackController
    {
        private readonly PlacementGrid grid;
        private readonly BoardManager boardManager;
        private readonly PlacementValidator validator;

        // === CONSTRUCTOR ===

        public UIFeedbackController(
            PlacementGrid placementGrid,
            BoardManager board,
            PlacementValidator placementValidator)
        {
            grid = placementGrid;
            boardManager = board;
            validator = placementValidator;
        }

        // === PUBLIC API ===

        /// <summary>
        /// Mostra feedback visual ao iniciar drag
        /// </summary>
        public void ShowDragFeedback()
        {
            if (grid != null)
            {
                grid.ShowValidTiles();
            }
        }

        /// <summary>
        /// Esconde feedback visual ao terminar drag
        /// </summary>
        public void HideDragFeedback()
        {
            if (grid != null)
            {
                grid.HideValidTiles();
            }

            ResetAllFootprintsToNormal();
        }

        /// <summary>
        /// Atualiza estados dos footprints durante drag
        /// PRIORIDADE: Swap > FullBoard > Invalid > Valid
        /// </summary>
        public void UpdateFootprintStates(UnitController draggingUnit)
        {
            if (draggingUnit == null) return;

            var dragFootprint = draggingUnit.GetModule<FootprintModule>();
            if (dragFootprint == null) return;

            // Usar posição do FOOTPRINT (não do mouse)
            Vector2 footprintPos = dragFootprint.GetCurrentWorldPosition();

            // Detectar unidade próxima (overlap)
            UnitController overlappingUnit = validator.GetOverlappingUnit(footprintPos, boardManager);
            bool hasOverlap = (overlappingUnit != null);

            // Validar posição no grid
            bool isValid = validator.IsPositionValid(footprintPos);

            // Verificar se board está cheio
            bool isBoardFull = boardManager.IsFull;

            // === LÓGICA DE ESTADOS (PRIORIDADE) ===

            if (hasOverlap)
            {
                // SWAP (AMARELO): Ambos footprints ficam amarelos
                dragFootprint.ShowSwapState();

                var targetFootprint = overlappingUnit.GetModule<FootprintModule>();
                if (targetFootprint != null)
                {
                    targetFootprint.ShowSwapState();
                }

                // Outros footprints voltam ao normal
                ResetAllFootprintsToNormal(overlappingUnit);
            }
            else if (isBoardFull)
            {
                // BOARD CHEIO (LARANJA): Não pode posicionar
                dragFootprint.ShowFullBoardState();
                ResetAllFootprintsToNormal();
            }
            else if (!isValid)
            {
                // INVÁLIDO (VERMELHO): Fora do grid
                dragFootprint.ShowInvalidState();
                ResetAllFootprintsToNormal();
            }
            else
            {
                // VÁLIDO: verde normalmente; no Sandbox, se a posição cai no lado inimigo, tinge de inimigo.
                if (PreparationManager.EnemySidePlacement != null && PreparationManager.EnemySidePlacement(footprintPos))
                    dragFootprint.ShowEnemyState();
                else
                    dragFootprint.ShowValidState();
                ResetAllFootprintsToNormal();
            }
        }

        /// <summary>
        /// Reseta todos os footprints (exceto o especificado) para estado normal
        /// </summary>
        private void ResetAllFootprintsToNormal(UnitController except = null)
        {
            foreach (var unit in boardManager.GetAllUnits())
            {
                if (unit == except) continue;

                var footprint = unit.GetModule<FootprintModule>();
                if (footprint != null)
                {
                    footprint.ShowNormalState();
                }
            }
        }
    }
}
