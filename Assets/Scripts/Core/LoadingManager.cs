using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading;
using System.Threading.Tasks;
using DropInHeroes.Data;
using DropInHeroes.UI;
using DropInHeroes.Utils;

namespace DropInHeroes.Core
{

    public class LoadingManager : MonoBehaviour
    {
        [Header("Loading Settings")]
        [SerializeField] private float minimumLoadTime = 2f;

        [Header("References")]
        [SerializeField] private LoadingUI loadingUI;

        private float currentProgress = 0f;
        private CancellationTokenSource cancellationTokenSource;

        private void OnEnable()
        {
            DataManager.OnInitializationProgress += OnDataManagerProgress;
        }

        private void OnDisable()
        {
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
                DebugManager.LogWarning("Carregamento cancelado", DebugCategory.Initialization);
            }
            catch (Exception e)
            {
                DebugManager.LogError($"Erro durante carregamento: {e.Message}", DebugCategory.Initialization);
                UpdateProgress(0f, $"ERRO: {e.Message}");
            }
        }

        private async Task LoadGameSequence(CancellationToken cancellationToken)
        {
            float startTime = Time.time;

            UpdateProgress(0.1f, "Inicializando sistemas...");

            if (DataManager.Instance == null)
            {
                throw new InvalidOperationException("DataManager não encontrado na cena!");
            }

            UpdateProgress(0.2f, "Carregando base de dados...");

            bool success = await DataManager.Instance.WaitForInitialization(10f);
            if (!success)
            {
                throw new TimeoutException("DataManager demorou demais para inicializar!");
            }

            DebugManager.Log("DataManager pronto!", DebugCategory.Initialization);
            UpdateProgress(0.7f, "Preparando...");

            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minimumLoadTime)
            {
                float remainingTime = minimumLoadTime - elapsedTime;
                await Task.Delay(TimeSpan.FromSeconds(remainingTime), cancellationToken);
            }

            UpdateProgress(1f, "Iniciando jogo...");
            await Task.Delay(500, cancellationToken);

            await LoadScene(LoadingRequest.TargetScene, cancellationToken);
        }

        private async Task LoadScene(string sceneName, CancellationToken cancellationToken)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                cancellationToken.ThrowIfCancellationRequested();
                float sceneProgress = asyncLoad.progress / 0.9f;
                DebugManager.Log($"Carregando cena: {sceneProgress * 100:F0}%", DebugCategory.Initialization);
                await Task.Yield();
            }

            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }

            Scene loadingScene = gameObject.scene;
            if (loadingScene.IsValid() && loadingScene.name != sceneName)
            {
                await SceneManager.UnloadSceneAsync(loadingScene);
                DebugManager.Log($"Scene '{loadingScene.name}' descarregada", DebugCategory.Initialization);
            }

            DebugManager.Log("Cena carregada!", DebugCategory.Initialization);
        }

        private void OnDataManagerProgress(float progress)
        {
            // Mapear progresso do DataManager para faixa 0.2 - 0.7
            float mappedProgress = 0.2f + (progress * 0.5f);
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

            DebugManager.Log($"{progress * 100:F0}% - {message}", DebugCategory.Initialization);
        }

        private void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
}
