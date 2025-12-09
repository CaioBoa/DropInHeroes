using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("State Configuration")]
    [SerializeField] private GameStateConfig[] stateConfigs;
    private Dictionary<GameState, GameStateConfig> configLookup;
    private Stack<GameState> stateStack;

    // Eventos
    public event Action<GameState, GameState> OnStateChanged;

    // === INITIALIZATION ===

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeConfigLookup();
    }

    private void InitializeConfigLookup()
    {
        configLookup = new Dictionary<GameState, GameStateConfig>();
        stateStack = new Stack<GameState>();

        // Carregar configs do Inspector
        foreach (var config in stateConfigs)
        {
            configLookup[config.state] = config;
        }

        // Criar configs padrão para estados sem configuração
        foreach (GameState state in Enum.GetValues(typeof(GameState)))
        {
            if (!configLookup.ContainsKey(state))
            {
                Debug.LogError($"[GameStateManager] Config ausente para estado {state}.");
            }
        }

        // Inicializar pilha com Gameplay (estado base imutável)
        stateStack.Push(GameState.Gameplay);
        ApplyStateEffects(GameState.Gameplay);
        Debug.Log($"[GameStateManager] Inicializado em {GameState.Gameplay}");
    }

    // === STACK LOGIC ===

    public GameState GetCurrentState()
    {
        if (stateStack.Count == 0)
        {
            Debug.LogError("[GameStateManager] Pilha vazia!");
            return GameState.Gameplay;
        }
        return stateStack.Peek();
    }

    /// <summary>
    /// Empilha um novo estado sobre o atual
    /// </summary>
    public void PushState(GameState newState)
    {
        GameState previousState = GetCurrentState();

        stateStack.Push(newState);
        ApplyStateEffects(newState);

        OnStateChanged?.Invoke(previousState, newState);
        Debug.Log($"[GameStateManager] {previousState} → {newState} (Stack: {stateStack.Count})");
    }

    /// <summary>
    /// Remove o estado do topo e retorna ao anterior
    /// Gameplay (base) nunca pode ser removido
    /// </summary>
    public void PopState()
    {
        if (stateStack.Count <= 1)
        {
            Debug.LogWarning("[GameStateManager] Não é possível remover Gameplay (estado base)");
            return;
        }

        GameState oldState = stateStack.Pop();
        GameState newState = GetCurrentState();

        ApplyStateEffects(newState);

        OnStateChanged?.Invoke(oldState, newState);
        Debug.Log($"[GameStateManager] {oldState} → {newState} (Stack: {stateStack.Count})");
    }

    // === APPLY EFFECTS ===

    private void ApplyStateEffects(GameState state)
    {
        if (!configLookup.TryGetValue(state, out GameStateConfig config))
        {
            Debug.LogWarning($"[GameStateManager] Config não encontrado para {state}");
            return;
        }

        // Aplicar efeito de tempo
        Time.timeScale = config.freezeTime ? 0f : 1f;
    }

    // === QUERIES ===

    public bool CanPlayerMove()
    {
        return configLookup.TryGetValue(GetCurrentState(), out var config) && config.allowPlayerMovement;
    }

    public bool CanPlayerInteract()
    {
        return configLookup.TryGetValue(GetCurrentState(), out var config) && config.allowPlayerInteraction;
    }

    public bool CanPause()
    {
        return configLookup.TryGetValue(GetCurrentState(), out var config) && config.allowPause;
    }

    public bool ShouldShowInteractionIndicators()
    {
        return configLookup.TryGetValue(GetCurrentState(), out var config) && config.showInteractionIndicators;
    }

    public bool ShouldDetectInteractables()
    {
        return configLookup.TryGetValue(GetCurrentState(), out var config) && config.detectInteractables;
    }

    public bool ShouldShowHUD()
    {
        return configLookup.TryGetValue(GetCurrentState(), out var config) && config.showHUD;
    }

    public bool IsInState(GameState state) => GetCurrentState() == state;
}
