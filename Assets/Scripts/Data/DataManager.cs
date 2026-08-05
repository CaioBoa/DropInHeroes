using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DropInHeroes.Combat;
using DropInHeroes.Utils;

namespace DropInHeroes.Data
{

    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        [SerializeField] public GameDataCatalog catalog;

        // Eventos
        public static event Action OnDataManagerReady;
        public static event Action<float> OnInitializationProgress;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        // Task para aguardar inicialização
        private TaskCompletionSource<bool> initializationTask;
        private CancellationTokenSource cancellationTokenSource;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Preparar task de inicialização
            initializationTask = new TaskCompletionSource<bool>();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private async void Start()
        {
            try
            {
                await Initialize(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[DataManager] Inicialização cancelada");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] Erro durante inicialização: {e.Message}");
                initializationTask.TrySetException(e);
            }
        }

        private async Task Initialize(CancellationToken cancellationToken)
        {
            Debug.Log("[DataManager] Iniciando carregamento...");

            // === FASE 1: Validar catalog ===
            OnInitializationProgress?.Invoke(0.1f);
            await Task.Yield(); // Equivalente a yield return null

            cancellationToken.ThrowIfCancellationRequested();

            if (catalog == null)
            {
                string error = "[DataManager] GameDataCatalog não atribuído!";
                Debug.LogError(error);
                throw new InvalidOperationException(error);
            }

            // === FASE 2: Inicializar catalog ===
            OnInitializationProgress?.Invoke(0.5f);

            // catalog.Initialize() percorre ScriptableObjects (UnityEngine.Object), que não são
            // thread-safe — roda na main thread. Task.Yield dá um respiro de frame ao redor.
            catalog.Initialize();
            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            // === FASE 3: Validações adicionais ===
            OnInitializationProgress?.Invoke(0.8f);
            await Task.Delay(200, cancellationToken);

            // === FASE 4: Finalizado ===
            OnInitializationProgress?.Invoke(1f);
            isInitialized = true;

            Debug.Log("[DataManager] ✓ Inicialização completa!");

            // Notificar eventos
            OnDataManagerReady?.Invoke();

            // Completar task
            initializationTask.TrySetResult(true);
        }

        /// <summary>
        /// Aguarda DataManager estar pronto. Pode ser chamado de qualquer lugar.
        /// </summary>
        public Task WaitForInitialization()
        {
            if (isInitialized)
                return Task.CompletedTask;

            return initializationTask.Task;
        }

        /// <summary>
        /// Aguarda com timeout. Retorna false se timeout expirar.
        /// </summary>
        public async Task<bool> WaitForInitialization(float timeoutSeconds)
        {
            if (isInitialized)
                return true;

            using var timeoutCts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), timeoutCts.Token);
            var completedTask = await Task.WhenAny(initializationTask.Task, timeoutTask);
            timeoutCts.Cancel(); // se a init venceu, cancela o Task.Delay para não deixá-lo órfão

            return completedTask == initializationTask.Task;
        }

        private void OnDestroy()
        {
            // Cancelar tarefas em andamento
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        // === ACESSO A DADOS ===

        public static CharacterData GetCharacter(string id)
        {
            if (Instance == null)
            {
                Debug.LogError("[DataManager] GetCharacter chamado sem DataManager na cena (abra a partir da cena Loading).");
                return null;
            }
            return Instance.catalog.GetCharacter(id);
        }

        public static List<CharacterData> GetAllCharacters()
        {
            if (Instance == null)
            {
                Debug.LogError("[DataManager] GetAllCharacters chamado sem DataManager na cena (abra a partir da cena Loading).");
                return new List<CharacterData>();
            }
            return Instance.catalog.GetAllCharacters();
        }

        public static ArtifactData GetArtifact(string id)
        {
            if (Instance == null)
            {
                Debug.LogError("[DataManager] GetArtifact chamado sem DataManager na cena (abra a partir da cena Loading).");
                return null;
            }
            return Instance.catalog.GetArtifact(id);
        }

        public static List<ArtifactData> GetAllArtifacts()
        {
            if (Instance == null)
            {
                Debug.LogError("[DataManager] GetAllArtifacts chamado sem DataManager na cena (abra a partir da cena Loading).");
                return new List<ArtifactData>();
            }
            return Instance.catalog.GetAllArtifacts();
        }

        public static StatTreeData GetSharedStatTree()
        {
            if (Instance == null)
            {
                Debug.LogError("[DataManager] GetSharedStatTree chamado sem DataManager na cena (abra a partir da cena Loading).");
                return null;
            }
            return Instance.catalog.GetSharedStatTree();
        }

        public static StatusTypeDef GetStatus(string id)
        {
            if (Instance == null)
            {
                Debug.LogError("[DataManager] GetStatus chamado sem DataManager na cena (abra a partir da cena Loading).");
                return null;
            }
            return Instance.catalog.GetStatus(id);
        }

        // Estado estático sobrevive ao recarregamento de domínio desabilitado (Enter Play Mode
        // Options); reseta para não reter Instance/handlers de uma sessão de Play anterior.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnDataManagerReady = null;
            OnInitializationProgress = null;
        }
    }
}
