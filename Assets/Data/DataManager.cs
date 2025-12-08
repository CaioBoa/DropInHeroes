using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        // Executar em background (se catalog.Initialize for pesado)
        await Task.Run(() => catalog.Initialize(), cancellationToken);

        // Voltar para main thread (necessário para Unity API)
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

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
        var completedTask = await Task.WhenAny(initializationTask.Task, timeoutTask);

        return completedTask == initializationTask.Task;
    }

    private void OnDestroy()
    {
        // Cancelar tarefas em andamento
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    // === ACESSO A DADOS ===

    public static CharacterData GetCharacter(string id) => Instance.catalog.GetCharacter(id);

    public static DialogueData GetDialogue(string id) => Instance.catalog.GetDialogue(id);

    public static BattleData GetBattle(string id) => Instance.catalog.GetBattle(id);
}
