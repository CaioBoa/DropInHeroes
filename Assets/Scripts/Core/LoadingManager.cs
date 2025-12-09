using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading;
using System.Threading.Tasks;

public class LoadingManager : MonoBehaviour
{
    [Header("Loading Settings")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string battleSceneName = "Battle";
    [SerializeField] private float minimumLoadTime = 2f;

    [Header("References")]
    [SerializeField] private LoadingUI loadingUI;

    private float currentProgress = 0f;
    private CancellationTokenSource cancellationTokenSource;
    private Scene battleScene;

    private void OnEnable()
    {
        // Inscrever-se nos eventos (ainda podemos usar eventos junto com async!)
        DataManager.OnInitializationProgress += OnDataManagerProgress;
    }

    private void OnDisable()
    {
        // Desinscrever
        DataManager.OnInitializationProgress -= OnDataManagerProgress;
    }

    private async void Start()
    {
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await LoadGameSequence(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[LoadingManager] Carregamento cancelado");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoadingManager] Erro durante carregamento: {e.Message}");
            UpdateProgress(0f, $"ERRO: {e.Message}");
        }
    }

    private async Task LoadGameSequence(CancellationToken cancellationToken)
    {
        float startTime = Time.time;

        // === FASE 1: Inicializar ===
        UpdateProgress(0.1f, "Inicializando sistemas...");

        if (DataManager.Instance == null)
        {
            throw new InvalidOperationException("DataManager não encontrado na cena!");
        }

        // === FASE 2: Aguardar DataManager (async/await style!) ===
        UpdateProgress(0.2f, "Carregando base de dados...");

        // Aguardar com timeout de 10 segundos
        bool success = await DataManager.Instance.WaitForInitialization(10f);

        if (!success)
        {
            throw new TimeoutException("DataManager demorou demais para inicializar!");
        }

        Debug.Log("[LoadingManager] ✓ DataManager pronto!");

        // === FASE 3: Pre-carregar recursos de batalha ===
        await PreloadBattleScene(cancellationToken);
        UpdateProgress(0.6f, "Preparando Componentes de Batalha...");

        // === FASE 4: Garantir tempo mínimo ===
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadTime)
        {
            float remainingTime = minimumLoadTime - elapsedTime;
            UpdateProgress(0.9f, "Preparando...");

            // Delay async
            await Task.Delay(TimeSpan.FromSeconds(remainingTime), cancellationToken);
        }

        // === FASE 5: Carregar cena ===
        UpdateProgress(1f, "Iniciando jogo...");
        await Task.Delay(500, cancellationToken);

        // Carregar cena de forma assíncrona
        await LoadScene(gameSceneName, cancellationToken);
    }

    private async Task PreloadBattleScene(CancellationToken cancellationToken)
    {
        Debug.Log($"[LoadingManager] Pré-carregando {battleSceneName}...");

        // Carregar scene de forma aditiva
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }

        battleScene = SceneManager.GetSceneByName(battleSceneName);

        // Inicializar BattleManager ANTES de desativar
        BattleManager battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.Initialize();
            Debug.Log("[LoadingManager] BattleManager inicializado!");
        }
        else
        {
            Debug.LogError("[LoadingManager] BattleManager não encontrado na Battle scene!");
        }

        // DESATIVAR todos os root objects APÓS inicializar
        foreach (GameObject rootObj in battleScene.GetRootGameObjects())
        {
            rootObj.SetActive(false);
        }

        Debug.Log($"[LoadingManager] {battleSceneName} pré-carregada e desativada!");
    }

    /// <summary>
    /// Carrega cena de forma assíncrona
    /// </summary>
    private async Task LoadScene(string sceneName, CancellationToken cancellationToken)
    {
        // Iniciar operação assíncrona do Unity (Additive para não destruir Battle scene!)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Prevenir ativação automática (queremos controlar quando ativar)
        asyncLoad.allowSceneActivation = false;

        // Aguardar carregamento (90% = pronto para ativar)
        while (asyncLoad.progress < 0.9f)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Atualizar progresso
            float sceneProgress = asyncLoad.progress / 0.9f;
            Debug.Log($"[LoadingManager] Carregando cena: {sceneProgress * 100:F0}%");

            // Aguardar próximo frame
            await Task.Yield();
        }

        // Ativar cena
        asyncLoad.allowSceneActivation = true;

        // Aguardar ativação completa
        while (!asyncLoad.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }

        // Definir como Active Scene (importante para additive loading)
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        // Descarregar a scene de Loading (não é mais necessária)
        Scene loadingScene = gameObject.scene;
        if (loadingScene.IsValid() && loadingScene.name != sceneName)
        {
            await SceneManager.UnloadSceneAsync(loadingScene);
            Debug.Log($"[LoadingManager] Scene '{loadingScene.name}' descarregada");
        }

        Debug.Log("[LoadingManager] ✓ Cena carregada!");
    }

    // Callback de progresso (evento do DataManager)
    private void OnDataManagerProgress(float progress)
    {
        // Mapear progresso do DataManager para faixa 0.2 - 0.6
        float mappedProgress = 0.2f + (progress * 0.4f);
        currentProgress = Mathf.Max(currentProgress, mappedProgress);
        UpdateProgress(currentProgress, "Carregando dados do jogo...");
    }

    private void UpdateProgress(float progress, string message)
    {
        currentProgress = progress;

        if (loadingUI != null)
        {
            loadingUI.UpdateProgress(progress, message);
        }

        Debug.Log($"[Loading] {progress * 100:F0}% - {message}");
    }

    private void OnDestroy()
    {
        // Cancelar operações async
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}
