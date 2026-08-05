using UnityEngine;
using System;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Coordena todo o fluxo de drag and drop de unidades
    /// Responsabilidade: orquestração de drag (não lógica de validação ou swap)
    /// </summary>
    public class DragCoordinator
    {
        private readonly UnitPool unitPool;
        private readonly BoardManager boardManager;
        private readonly PlacementValidator validator;
        private readonly PreparationConfig config;
        private readonly Camera mainCamera;

        private UnitController currentDraggingUnit;
        private bool isDraggingFromBoard;
        private Vector2 dragOriginPosition; // Posição de onde a unidade saiu (para swap)
        // Um swap agenda um callback de lerp (~0.5s) que ainda mexe no board depois que
        // currentDraggingUnit já foi zerado. Bloquear novos drags nessa janela evita que a
        // unidade temporária ainda viva seja capturada por um segundo drag (double-return ao pool).
        private bool isSwapPending;

        // === EVENTS ===
        public event Action<UnitController> OnDragStarted;
        public event Action<UnitController> OnDragUpdated;
        public event Action<UnitController> OnDragCompleted;
        public event Action OnDragCancelled;

        // === CONSTRUCTOR ===

        public DragCoordinator(
            UnitPool pool,
            BoardManager board,
            PlacementValidator placementValidator,
            PreparationConfig configuration,
            Camera camera)
        {
            unitPool = pool;
            boardManager = board;
            validator = placementValidator;
            config = configuration;
            mainCamera = camera;
        }

        // === PUBLIC API ===

        /// <summary>
        /// Inicia drag do carousel - spawna unidade imediatamente
        /// </summary>
        public void StartDragFromCarousel(CharacterData characterData, Vector2 screenPos)
        {
            if (characterData == null)
            {
                DebugManager.LogError("CharacterData é null!", DebugCategory.Drag);
                return;
            }

            if (currentDraggingUnit != null || isSwapPending)
            {
                DebugManager.LogWarning("Já existe um drag em andamento!", DebugCategory.Drag);
                return;
            }

            Vector2 worldPos = ScreenToWorldPosition(screenPos);

            // Spawn unidade
            GameObject unitInstance = unitPool.SpawnUnit(characterData, worldPos);
            if (unitInstance == null)
            {
                DebugManager.LogError("Falha ao spawnar unidade!", DebugCategory.Drag);
                return;
            }

            currentDraggingUnit = unitInstance.GetComponent<UnitController>();
            if (currentDraggingUnit == null)
            {
                DebugManager.LogError("UnitController não encontrado!", DebugCategory.Drag);
                unitPool.ReturnUnit(unitInstance);
                return;
            }

            isDraggingFromBoard = false; // Drag do carousel

            // Configurar footprint para drag
            var footprintModule = currentDraggingUnit.GetModule<FootprintModule>();
            if (footprintModule != null)
            {
                footprintModule.SetYOffset(config.footprintDragOffset);
            }

            // Iniciar drag
            var dragModule = currentDraggingUnit.GetModule<DragModule>();
            if (dragModule != null)
            {
                dragModule.StartDrag(worldPos);
            }

            OnDragStarted?.Invoke(currentDraggingUnit);
            DebugManager.Log($"Drag iniciado do carousel: {characterData.displayName}", DebugCategory.Drag);
        }

        /// <summary>
        /// Inicia drag do board (re-posicionamento)
        /// </summary>
        public void StartDragFromBoard(UnitController unit)
        {
            if (unit == null)
            {
                DebugManager.LogError("Unidade é null!", DebugCategory.Drag);
                return;
            }

            if (currentDraggingUnit != null || isSwapPending)
            {
                DebugManager.LogWarning("Já existe um drag em andamento!", DebugCategory.Drag);
                return;
            }

            // Verificar se drag está habilitado para esta unidade
            var dragModule = unit.GetModule<DragModule>();
            if (dragModule == null || !dragModule.IsEnabled)
            {
                DebugManager.LogWarning("Drag não permitido para esta unidade!", DebugCategory.Drag);
                return;
            }

            currentDraggingUnit = unit;
            isDraggingFromBoard = true;
            dragOriginPosition = unit.transform.position; // Guardar posição de origem para swap

            // Remover temporariamente do board
            boardManager.RemoveUnit(unit);

            // Configurar footprint para drag
            var footprintModule = unit.GetModule<FootprintModule>();
            if (footprintModule != null)
            {
                footprintModule.SetYOffset(config.footprintDragOffset);
            }

            // Iniciar drag
            if (dragModule != null)
            {
                dragModule.StartDrag(unit.transform.position);
            }

            OnDragStarted?.Invoke(currentDraggingUnit);
            DebugManager.Log($"Drag iniciado do board: {unit.GetCharacterData()?.displayName}", DebugCategory.Drag);
        }

        /// <summary>
        /// Atualiza posição durante drag
        /// </summary>
        public void UpdateDrag(Vector2 screenPos)
        {
            if (currentDraggingUnit == null) return;

            Vector2 worldPos = ScreenToWorldPosition(screenPos);

            // Atualizar posição
            var dragModule = currentDraggingUnit.GetModule<DragModule>();
            if (dragModule != null)
            {
                dragModule.UpdateDragPosition(worldPos);
            }

            OnDragUpdated?.Invoke(currentDraggingUnit);
        }

        /// <summary>
        /// Finaliza drag - valida e posiciona ou cancela
        /// </summary>
        public void FinishDrag(Vector2 screenPos)
        {
            if (currentDraggingUnit == null) return;

            var dragModule = currentDraggingUnit.GetModule<DragModule>();
            var footprintModule = currentDraggingUnit.GetModule<FootprintModule>();

            // Usar posição do FOOTPRINT para validação
            Vector2 footprintPos = footprintModule.GetCurrentWorldPosition();

            // 1. Validar posição
            if (!validator.IsPositionValid(footprintPos))
            {
                CancelCurrentDrag();
                DebugManager.Log("Posição inválida - drag cancelado", DebugCategory.Drag);
                return;
            }

            // 2. Detectar overlap
            UnitController overlappingUnit = validator.GetOverlappingUnit(footprintPos, boardManager);
            bool hasOverlap = overlappingUnit != null;

            if (hasOverlap)
            {
                // SWAP: trocar unidade existente
                HandleSwap(overlappingUnit, footprintPos, dragModule, footprintModule);
            }
            else if (boardManager.IsFull)
            {
                // BOARD CHEIO: cancelar
                CancelCurrentDrag();
                DebugManager.LogWarning("Board cheio - drag cancelado", DebugCategory.Drag);
            }
            else
            {
                // PLACEMENT NORMAL: posicionar
                HandlePlacement(footprintPos, dragModule, footprintModule);
            }
        }

        // === PRIVATE METHODS ===

        private void HandleSwap(UnitController existingUnit, Vector2 dropPosition, DragModule dragModule, FootprintModule footprintModule)
        {
            var draggingUnit = currentDraggingUnit;
            bool isFromCarousel = !isDraggingFromBoard;

            // O swap só termina quando o callback de lerp roda; bloquear novos drags até lá.
            isSwapPending = true;

            // 1. Fixar footprint no destino antes do lerp
            footprintModule?.LockWorldPosition();

            // 2. Se Board→Board: mover existente para origem com lerp rápido
            if (!isFromCarousel)
            {
                boardManager.RemoveUnit(existingUnit);
                var existingDragModule = existingUnit.GetModule<DragModule>();
                if (existingDragModule != null)
                {
                    existingDragModule.StartSwapLerp(dragOriginPosition, () => {
                        boardManager.AddUnit(existingUnit);
                        isSwapPending = false;
                    });
                }
                else
                {
                    boardManager.AddUnit(existingUnit);
                    isSwapPending = false;
                }
            }

            // 3. Configurar callback para após lerp da unidade arrastada
            if (isFromCarousel)
            {
                // Carousel→Board: substituir unidade existente após lerp
                var newData = draggingUnit.GetCharacterData();
                var tempRef = draggingUnit;

                Action lerpCallback = null;
                lerpCallback = () => {
                    dragModule.OnDropLerpCompleted -= lerpCallback;

                    // IMPORTANTE: Remover unidade temporária do board antes de retornar ao pool
                    boardManager.RemoveUnit(tempRef);
                    unitPool.ReturnUnit(tempRef.gameObject);

                    ReplaceUnit(existingUnit, newData, dropPosition);
                    isSwapPending = false;
                };
                dragModule.OnDropLerpCompleted += lerpCallback;
            }

            // 4. Usar HandlePlacement para a unidade arrastada (reutilização!)
            HandlePlacement(dropPosition, dragModule, footprintModule);

            DebugManager.Log($"Swap {(isFromCarousel ? "Carousel→Board" : "Board↔Board")} executado", DebugCategory.Drag);
        }

        private void ReplaceUnit(UnitController oldUnit, CharacterData newData, Vector2 pos)
        {
            boardManager.RemoveUnit(oldUnit);
            unitPool.ReturnUnit(oldUnit.gameObject);

            var newObj = unitPool.SpawnUnit(newData, pos);
            var newUnit = newObj.GetComponent<UnitController>();

            newUnit.GetModule<DragModule>()?.OnEnabled();
            newUnit.GetModule<FootprintModule>()?.OnEnabled();

            boardManager.AddUnit(newUnit);
            OnDragCompleted?.Invoke(newUnit);
        }

        private void HandlePlacement(Vector2 position, DragModule dragModule, FootprintModule footprintModule)
        {
            UnitController placedUnit = currentDraggingUnit;

            // Executar lerp
            if (dragModule != null)
            {
                Action lerpCallback = null;
                lerpCallback = () => {
                    // Remover listener para evitar memory leak
                    dragModule.OnDropLerpCompleted -= lerpCallback;
                    OnPlacementLerpCompleted(placedUnit);
                };

                dragModule.OnDropLerpCompleted += lerpCallback;
                dragModule.EndDrag();
            }

            // Habilitar módulos
            if (dragModule != null)
            {
                dragModule.OnEnabled();
            }

            if (footprintModule != null)
            {
                footprintModule.OnEnabled();
            }

            // Adicionar ao board
            boardManager.AddUnit(placedUnit);

            currentDraggingUnit = null;

            OnDragCompleted?.Invoke(placedUnit);
            DebugManager.Log($"Unidade posicionada em {position}", DebugCategory.Drag);
        }

        private void CancelCurrentDrag()
        {
            if (currentDraggingUnit == null) return;

            var dragModule = currentDraggingUnit.GetModule<DragModule>();
            if (dragModule != null)
            {
                dragModule.CancelDrag();
            }

            unitPool.ReturnUnit(currentDraggingUnit.gameObject);
            currentDraggingUnit = null;

            OnDragCancelled?.Invoke();
        }

        private void OnPlacementLerpCompleted(UnitController unit)
        {
            if (unit == null) return;

            var footprintModule = unit.GetModule<FootprintModule>();
            if (footprintModule != null)
            {
                footprintModule.SetYOffset(config.footprintPlacedOffset);
            }
        }

        private Vector2 ScreenToWorldPosition(Vector2 screenPos)
        {
            if (mainCamera == null)
            {
                DebugManager.LogWarning("Camera não configurada!", DebugCategory.Drag);
                return Vector2.zero;
            }

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane));
            return new Vector2(worldPos.x, worldPos.y);
        }

        // === PROPERTIES ===

        public bool IsDragging => currentDraggingUnit != null;
        public UnitController CurrentDraggingUnit => currentDraggingUnit;
    }
}
