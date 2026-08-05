using UnityEngine;
using System;
using System.Collections;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Módulo responsável pela lógica de drag and drop da unidade
    /// Gerencia estado de drag, posicionamento e comunicação com animator
    /// </summary>
    public class DragModule : IUnitModule
    {
        private UnitController controller;
        private VisualModule visualModule;
        private FootprintModule footprintModule; // Referência ao FootprintModule

        // Estados de drag
        public enum DragState
        {
            Idle,       // Não arrastando
            Dragging,   // Arraste ativo
            Dropping,   // Animação de queda (lerp ativo)
            Cancelled   // Drag cancelado (estado transitório)
        }

        private DragState dragState = DragState.Idle;
        private Vector2 dragStartPosition;
        private Vector2 currentPosition;
        private bool isEnabled = true;

        // Lerp de drop
        private Coroutine dropLerpCoroutine;
        private float DROP_LERP_DURATION;
        private float SWAP_LERP_DURATION; // Lerp rápido para unidade sendo swapada

        // Eventos para sistemas externos
        public event Action<Vector2> OnDragPositionChanged;
        public event Action OnDragStarted;
        public event Action OnDragEnded;
        public event Action OnDragCancelled;
        public event Action OnDropLerpCompleted; // Novo: quando lerp de queda termina

        // === INTERFACE IMPLEMENTATION ===

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            visualModule = controller.GetModule<VisualModule>();
            footprintModule = controller.GetModule<FootprintModule>();

            // Aplicar configurações globais do PreparationConfig
            if (PreparationManager.Instance != null && PreparationManager.Instance.Config != null)
            {
                ApplyConfig(PreparationManager.Instance.Config);
            }
            else
            {
                DebugManager.LogWarning("PreparationConfig não acessível! Footprint pode ter escala/offset incorreto.", DebugCategory.Drag);
            }

            if (visualModule == null)
            {
                DebugManager.LogError("VisualModule não encontrado! Drag pode não funcionar corretamente.", DebugCategory.Drag);
            }

            if (footprintModule == null)
            {
                DebugManager.LogError("FootprintModule não encontrado! DragModule depende de FootprintModule para collider.", DebugCategory.Drag);
            }
        }

        private void ApplyConfig(PreparationConfig config)
        {
            if (config == null)
            {
                DebugManager.LogWarning("PreparationConfig é null! Usando valores padrão.", DebugCategory.Drag);
                DROP_LERP_DURATION = 0.5f;
                SWAP_LERP_DURATION = 0.15f;
                return;
            }

            DROP_LERP_DURATION = config.dropLerpDuration;
            SWAP_LERP_DURATION = config.swapLerpDuration;
        }
        public void OnEnabled()
        {
            isEnabled = true;
            // Ao reativar (ex.: retorno ao pool), garante que nenhum drag/drop anterior tenha ficado
            // preso — senão o dragState travado rejeita o próximo StartDrag.
            if (dragState != DragState.Idle) CancelDrag();
            footprintModule?.OnEnabled();
        }

        public void OnDisabled()
        {
            isEnabled = false;
            footprintModule?.OnDisabled();
        }

        public void Cleanup()
        {
            if (dragState == DragState.Dragging || dragState == DragState.Dropping)
            {
                CancelDrag();
            }
            controller = null;
            visualModule = null;
        }

        // === PUBLIC API ===

        /// <summary>
        /// Inicia o drag da unidade
        /// </summary>
        public void StartDrag(Vector2 worldPosition)
        {
            if (!isEnabled)
            {
                DebugManager.LogWarning("DragModule desabilitado! Drag bloqueado.", DebugCategory.Drag);
                return;
            }

            if (dragState == DragState.Dragging || dragState == DragState.Dropping)
            {
                DebugManager.LogWarning("Drag já está ativo!", DebugCategory.Drag);
                return;
            }

            dragState = DragState.Dragging;
            dragStartPosition = worldPosition;
            currentPosition = worldPosition;

            // Unidade na posição do mouse (sem offset)
            UpdatePosition(worldPosition);

            // Ativar animação de drag
            if (visualModule != null)
            {
                visualModule.SetDraggingAnimation(true);
            }

            OnDragStarted?.Invoke();
            DebugManager.Log($"Drag iniciado na posição {worldPosition}", DebugCategory.Drag);
        }

        /// <summary>
        /// Atualiza a posição durante o drag
        /// </summary>
        public void UpdateDragPosition(Vector2 mouseWorldPosition)
        {
            if (dragState != DragState.Dragging)
            {
                DebugManager.LogWarning("UpdateDragPosition chamado mas drag não está ativo!", DebugCategory.Drag);
                return;
            }

            currentPosition = mouseWorldPosition;

            // Unidade SEMPRE na posição do mouse (sem offset)
            UpdatePosition(mouseWorldPosition);

            // Notificar para atualizar footprint (que terá offset)
            OnDragPositionChanged?.Invoke(mouseWorldPosition);
        }

        /// <summary>
        /// Finaliza o drag - personagem cai até a posição do FOOTPRINT
        /// </summary>
        public void EndDrag()
        {
            if (dragState != DragState.Dragging)
            {
                DebugManager.LogWarning("EndDrag chamado mas drag não está ativo!", DebugCategory.Drag);
                return;
            }

            dragState = DragState.Dropping;

            // Personagem cai até a posição do FOOTPRINT (no grid)
            Vector2 footprintPosition = footprintModule.GetCurrentWorldPosition();

            // Iniciar lerp (NÃO desativa animação ainda)
            if (controller != null)
            {
                // Parar corrotina anterior se existir
                if (dropLerpCoroutine != null)
                {
                    controller.StopCoroutine(dropLerpCoroutine);
                }

                dropLerpCoroutine = controller.StartCoroutine(DropLerpCoroutine(footprintPosition));
            }

            OnDragEnded?.Invoke();
            DebugManager.Log($"Iniciando drop lerp para footprint em {footprintPosition}", DebugCategory.Drag);
        }

        private IEnumerator DropLerpCoroutine(Vector2 targetPosition)
        {
            Vector2 startPos = currentPosition;
            float elapsed = 0f;

            while (elapsed < DROP_LERP_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / DROP_LERP_DURATION;
                float easedT = t * t * (3f - 2f * t);

                currentPosition = Vector2.Lerp(startPos, targetPosition, easedT);
                UpdatePosition(currentPosition);

                // Manter footprint fixo durante lerp
                footprintModule?.MaintainLockedPosition();

                yield return null;
            }

            // Finalizar
            currentPosition = targetPosition;
            UpdatePosition(targetPosition);
            dragState = DragState.Idle;

            // Liberar lock do footprint
            footprintModule?.UnlockPosition();

            // Desativar animação
            if (visualModule != null)
            {
                visualModule.SetDraggingAnimation(false);
            }

            dropLerpCoroutine = null;
            OnDropLerpCompleted?.Invoke();

            DebugManager.Log("Drop lerp finalizado", DebugCategory.Drag);
        }

        /// <summary>
        /// Cancela o drag e retorna à posição inicial
        /// </summary>
        public void CancelDrag()
        {
            // Cobre tanto o arraste ativo quanto a queda em curso (Dropping) — antes, uma unidade
            // devolvida ao pool durante a queda ficava com dragState travado (Cleanup/OnEnabled
            // chamavam CancelDrag, mas o guard só aceitava Dragging).
            if (dragState != DragState.Dragging && dragState != DragState.Dropping)
            {
                DebugManager.LogWarning("CancelDrag chamado mas drag não está ativo!", DebugCategory.Drag);
                return;
            }

            bool wasDragging = dragState == DragState.Dragging;

            StopDropLerp();
            dragState = DragState.Idle;

            // Arraste ativo cancelado volta à origem; uma queda em curso já ia para o tabuleiro,
            // então só destrava o footprint e mantém a posição atual.
            if (wasDragging) UpdatePosition(dragStartPosition);
            else footprintModule?.UnlockPosition();

            if (visualModule != null)
            {
                visualModule.SetDraggingAnimation(false);
            }

            OnDragCancelled?.Invoke();
            DebugManager.Log("Drag cancelado", DebugCategory.Drag);
        }

        private void StopDropLerp()
        {
            if (dropLerpCoroutine != null && controller != null)
                controller.StopCoroutine(dropLerpCoroutine);
            dropLerpCoroutine = null;
        }

        /// <summary>
        /// Define a posição da unidade sem iniciar drag
        /// </summary>
        public void SetPosition(Vector2 worldPosition)
        {
            currentPosition = worldPosition;
            UpdatePosition(worldPosition);
        }

        private void UpdatePosition(Vector2 worldPosition)
        {
            if (controller != null)
            {
                controller.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
            }
        }

        // === SWAP LERP (lerp rápido para unidade sendo swapada) ===

        /// <summary>
        /// Inicia lerp rápido para unidade sendo swapada (sem alterar animação)
        /// </summary>
        public void StartSwapLerp(Vector2 targetPosition, Action onComplete = null)
        {
            if (controller != null)
            {
                controller.StartCoroutine(SwapLerpCoroutine(targetPosition, onComplete));
            }
        }

        private IEnumerator SwapLerpCoroutine(Vector2 targetPosition, Action onComplete)
        {
            Vector2 startPos = currentPosition;
            float elapsed = 0f;

            while (elapsed < SWAP_LERP_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / SWAP_LERP_DURATION;
                float easedT = t * t * (3f - 2f * t);

                currentPosition = Vector2.Lerp(startPos, targetPosition, easedT);
                UpdatePosition(currentPosition);

                yield return null;
            }

            currentPosition = targetPosition;
            UpdatePosition(targetPosition);
            onComplete?.Invoke();
        }

        // === PROPERTIES ===

        public DragState CurrentDragState => dragState;
        public Vector2 CurrentPosition => currentPosition;
        public bool IsDragging => dragState == DragState.Dragging || dragState == DragState.Dropping;
        public Vector2 DragStartPosition => dragStartPosition;
        public bool IsEnabled => isEnabled;
    }
}
