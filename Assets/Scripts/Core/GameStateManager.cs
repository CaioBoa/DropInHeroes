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
                DebugManager.LogError($"Config ausente para estado {state}.", DebugCategory.State);
            }
        }

        // Inicializar pilha com Gameplay (estado base imutável)
        stateStack.Push(GameState.Gameplay);
        ApplyStateEffects(GameState.Gameplay);
        DebugManager.Log($"Inicializado em {GameState.Gameplay}", DebugCategory.State);
    }

    // === STACK LOGIC ===

    public GameState GetCurrentState()
    {
        if (stateStack.Count == 0)
        {
            DebugManager.LogError("Pilha vazia!", DebugCategory.State);
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
        DebugManager.Log($"{previousState} → {newState} (Stack: {stateStack.Count})", DebugCategory.State);
    }

    /// <summary>
    /// Remove o estado do topo e retorna ao anterior
    /// Gameplay (base) nunca pode ser removido
    /// </summary>
    public void PopState()
    {
        if (stateStack.Count <= 1)
        {
            DebugManager.LogWarning("Não é possível remover Gameplay (estado base)", DebugCategory.State);
            return;
        }

        GameState oldState = stateStack.Pop();
        GameState newState = GetCurrentState();

        ApplyStateEffects(newState);

        OnStateChanged?.Invoke(oldState, newState);
        DebugManager.Log($"{oldState} → {newState} (Stack: {stateStack.Count})", DebugCategory.State);
    }

    // === APPLY EFFECTS ===

    private void ApplyStateEffects(GameState state)
    {
        if (!configLookup.TryGetValue(state, out GameStateConfig config))
        {
            DebugManager.LogWarning($"Config não encontrado para {state}", DebugCategory.State);
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
