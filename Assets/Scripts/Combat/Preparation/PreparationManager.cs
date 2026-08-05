using UnityEngine;
using System.Threading.Tasks;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Orquestrador da fase de preparação de combate.
    /// Coordena BoardManager, PlacementValidator, DragCoordinator e UIFeedbackController.
    /// </summary>
    public class PreparationManager : MonoBehaviour
    {
        public static PreparationManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private PreparationConfig config;
        public PreparationConfig Config => config;

        [Header("References")]
        [SerializeField] private UnitPool unitPool;
        [SerializeField] private Camera mainCamera;

        [Header("Placement")]
        [SerializeField] private PlacementGrid placementGrid;

        [Header("Combat Transition")]
        [SerializeField] private RectTransform preCombatUI;
        [SerializeField] private CanvasGroup preCombatCanvasGroup;
        [SerializeField] private Camera battleCamera;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.8f;
        [SerializeField] private float slideDistance = 300f;
        [SerializeField] private float zoomAmount = 0.5f;

        // === GLOBAL DRAG EVENTS (FootprintModule reage a estes p/ mostrar/esconder o círculo) ===
        public event System.Action OnGlobalDragStarted;
        public event System.Action OnGlobalDragEnded;

        /// <summary>
        /// Hook opcional: dada uma posição de mundo, retorna true se ela cai no LADO INIMIGO do campo.
        /// Setado pelo modo Sandbox (que decide o time pelo lado); null na Torre → sem efeito. Consultado
        /// pelo feedback de drag para tingir o footprint de inimigo em posições válidas do lado adversário.
        /// </summary>
        public static System.Func<Vector2, bool> EnemySidePlacement;

        // Componentes especializados (injetados via Awake)
        private BoardManager boardManager;
        private PlacementValidator placementValidator;
        private DragCoordinator dragCoordinator;
        private UIFeedbackController uiFeedbackController;

        // Acesso público ao BoardManager para sistema de combate
        public BoardManager Board => boardManager;

        // State
        private TaskCompletionSource<bool> confirmationTCS;

        // Transition state
        private float originalCameraSize;
        private Vector2 originalUIPosition;

        // === INITIALIZATION ===

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Carregar config se não foi atribuído
            if (config == null)
            {
                config = Resources.Load<PreparationConfig>("PreparationConfig");
                if (config == null)
                {
                    DebugManager.LogWarning("PreparationConfig não encontrado! Usando valores padrão.", DebugCategory.Initialization);
                    // Criar config temporário com valores padrão
                    config = ScriptableObject.CreateInstance<PreparationConfig>();
                }
            }

            // Inicializar placement grid
            if (placementGrid != null)
            {
                placementGrid.Initialize();
            }

            // Inicializar componentes especializados
            InitializeComponents();

            DebugManager.Log("PreparationManager inicializado", DebugCategory.Initialization);
        }

        private void InitializeComponents()
        {
            // BoardManager
            boardManager = new BoardManager(config.maxUnitsOnField);

            // PlacementValidator
            placementValidator = new PlacementValidator(placementGrid, config.swapDetectionRadius);

            // DragCoordinator
            dragCoordinator = new DragCoordinator(
                unitPool,
                boardManager,
                placementValidator,
                config,
                mainCamera
            );

            // UIFeedbackController
            uiFeedbackController = new UIFeedbackController(
                placementGrid,
                boardManager,
                placementValidator
            );

            // Inscrever em eventos
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            // Eventos do DragCoordinator
            dragCoordinator.OnDragStarted += HandleDragStarted;
            dragCoordinator.OnDragUpdated += HandleDragUpdated;
            dragCoordinator.OnDragCompleted += HandleDragCompleted;
            dragCoordinator.OnDragCancelled += HandleDragCancelled;
        }

        private void OnDestroy()
        {
            // Desinscrever eventos do DragCoordinator
            if (dragCoordinator != null)
            {
                dragCoordinator.OnDragStarted -= HandleDragStarted;
                dragCoordinator.OnDragUpdated -= HandleDragUpdated;
                dragCoordinator.OnDragCompleted -= HandleDragCompleted;
                dragCoordinator.OnDragCancelled -= HandleDragCancelled;
            }

            // Desinscrever evento de combate
            if (CombatController.Instance != null)
            {
                CombatController.Instance.OnCombatEnded -= HandleCombatEnded;
            }
        }

        private void Start()
        {
            InitializeTransition();

            if (CombatController.Instance != null)
            {
                CombatController.Instance.OnCombatEnded += HandleCombatEnded;
            }
        }

        private void InitializeTransition()
        {
            if (battleCamera != null)
                originalCameraSize = battleCamera.orthographicSize;

            if (preCombatUI != null)
                originalUIPosition = preCombatUI.anchoredPosition;
        }

        // === PUBLIC API ===

        /// <summary>
        /// Spawna uma unidade do bench e inicia drag (entrada para o board).
        /// </summary>
        public void StartDraggingFromRoulette(CharacterData characterData, Vector2 screenPos)
        {
            if (characterData == null)
            {
                DebugManager.LogError("CharacterData é null!", DebugCategory.Drag);
                return;
            }

            dragCoordinator.StartDragFromCarousel(characterData, screenPos);
        }

        /// <summary>
        /// Inicia drag do board (re-posicionamento)
        /// </summary>
        public void StartDraggingFromBoard(UnitController unit)
        {
            if (unit == null)
            {
                DebugManager.LogError("UnitController é null!", DebugCategory.Drag);
                return;
            }

            dragCoordinator.StartDragFromBoard(unit);
        }

        public void UpdateDragging(Vector2 screenPos)
        {
            dragCoordinator.UpdateDrag(screenPos);
        }

        public void FinishDragging(Vector2 screenPos)
        {
            dragCoordinator.FinishDrag(screenPos);
        }

        // === EVENT HANDLERS (delegação para componentes) ===

        private void HandleDragStarted(UnitController unit)
        {
            uiFeedbackController.ShowDragFeedback();

            OnGlobalDragStarted?.Invoke();
            DebugManager.Log($"Drag iniciado: {unit?.GetCharacterData()?.displayName}", DebugCategory.Drag);
        }

        private void HandleDragUpdated(UnitController unit)
        {
            if (unit == null) return;

            uiFeedbackController.UpdateFootprintStates(unit);
        }

        private void HandleDragCompleted(UnitController unit)
        {
            uiFeedbackController.HideDragFeedback();

            OnGlobalDragEnded?.Invoke();
            DebugManager.Log($"Drag concluído: {unit?.GetCharacterData()?.displayName}", DebugCategory.Drag);
        }

        private void HandleDragCancelled()
        {
            uiFeedbackController.HideDragFeedback();

            OnGlobalDragEnded?.Invoke();
            DebugManager.Log("Drag cancelado", DebugCategory.Drag);
        }

        // === PHASE MANAGEMENT ===

        public async Task StartPreparationPhase()
        {
            DebugManager.Log("Iniciando fase de preparação...", DebugCategory.State);

            confirmationTCS = new TaskCompletionSource<bool>();
            await confirmationTCS.Task;

            DebugManager.Log("Fase de preparação concluída", DebugCategory.State);
        }

        public async void ConfirmPositioning()
        {
            if (confirmationTCS == null || confirmationTCS.Task.IsCompleted)
            {
                DebugManager.LogWarning("PreparationManager: ConfirmPositioning chamado mas não há confirmação pendente.", DebugCategory.State);
                return;
            }

            DebugManager.Log("PreparationManager: ConfirmPositioning chamado. Iniciando transição.", DebugCategory.State);
        
            // Se a transição falhar ou demorar demais, não devemos travar o jogo
            try 
            {
                await PlayCombatTransition();
            }
            catch (System.Exception e)
            {
                DebugManager.LogError($"PreparationManager: Erro na transição de combate: {e.Message}", DebugCategory.State);
            }

            DebugManager.Log("PreparationManager: Confirmando Task.", DebugCategory.State);
            confirmationTCS.TrySetResult(true);
        }

        // === COMBAT TRANSITION ===

        private async Task PlayCombatTransition()
        {
            DebugManager.Log("PreparationManager: Iniciando PlayCombatTransition", DebugCategory.State);
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Slide down + Fade UI
                if (preCombatUI != null && preCombatCanvasGroup != null)
                {
                    preCombatUI.anchoredPosition = originalUIPosition + Vector2.down * slideDistance * smoothT;
                    preCombatCanvasGroup.alpha = 1f - smoothT;
                }

                // Zoom da câmera
                if (battleCamera != null)
                {
                    battleCamera.orthographicSize = Mathf.Lerp(originalCameraSize, originalCameraSize - zoomAmount, smoothT);
                }

                await Task.Yield();
            }

            // Desligar UI para poupar recursos
            if (preCombatUI != null)
                preCombatUI.gameObject.SetActive(false);
        }

        public void ResetTransition()
        {
            if (preCombatUI != null)
            {
                preCombatUI.anchoredPosition = originalUIPosition;
                preCombatUI.gameObject.SetActive(true);
            }

            if (preCombatCanvasGroup != null)
                preCombatCanvasGroup.alpha = 1f;

            if (battleCamera != null)
                battleCamera.orthographicSize = originalCameraSize;
        }

        private void HandleCombatEnded(Team winner)
        {
            ResetTransition();
        }
    }
}
