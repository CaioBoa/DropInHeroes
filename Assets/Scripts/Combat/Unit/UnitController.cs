using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Hub central que orquestra módulos especializados da unidade
/// Gerencia estado geral e coordena módulos (Visual, Drag, Stats, etc)
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class UnitController : MonoBehaviour
{
    // === MODULE REGISTRY ===
    private Dictionary<Type, IUnitModule> modules = new Dictionary<Type, IUnitModule>();

    // === STATE ===
    private UnitState currentState = UnitState.Pooled;
    private CharacterData characterData;
    private Team team = Team.Player;

    // === LIFECYCLE ===

    private void Awake()
    {
        // Registrar módulos
        RegisterModule(new VisualModule());
        RegisterModule(new DragModule());
        RegisterModule(new FootprintModule());
        RegisterModule(new StatsModule());
        RegisterModule(new CombatModule());
        RegisterModule(new HealthBarModule());
        RegisterModule(new EnergyBarModule());

        // Inicializar módulos
        foreach (var module in modules.Values)
        {
            module.Initialize(this);
        }

        DebugManager.Log("Módulos registrados e inicializados", DebugCategory.Initialization);
    }

    private void Update()
    {
        if (currentState == UnitState.Combat)
        {
            GetModule<CombatModule>()?.Tick();
        }
    }

    // === MODULE MANAGEMENT ===

    private void RegisterModule(IUnitModule module)
    {
        Type moduleType = module.GetType();
        if (modules.ContainsKey(moduleType))
        {
            DebugManager.LogWarning($"Módulo {moduleType.Name} já registrado!", DebugCategory.Initialization);
            return;
        }

        modules[moduleType] = module;
    }

    public T GetModule<T>() where T : class, IUnitModule
    {
        Type moduleType = typeof(T);
        if (modules.TryGetValue(moduleType, out var module))
        {
            return module as T;
        }

        DebugManager.LogWarning($"Módulo {moduleType.Name} não encontrado!", DebugCategory.Combat);
        return null;
    }

    // === INITIALIZATION ===

    public void Initialize(CharacterData data)
    {
        Initialize(data, UnitConfig.Player);
    }

    public void Initialize(CharacterData data, UnitConfig config)
    {
        if (data == null)
        {
            DebugManager.LogError("Cannot initialize with null CharacterData!", DebugCategory.Combat);
            return;
        }

        characterData = data;
        team = config.team;

        // Aplicar stats do CharacterData
        GetModule<StatsModule>()?.ApplyCharacterStats(data);

        // Aplicar visuais via VisualModule
        var visualModule = GetModule<VisualModule>();
        if (visualModule != null)
        {
            visualModule.ApplyCharacterVisuals(data);
            visualModule.ApplyCharacterAnimations(data);
        }

        // Configurar módulos baseado no config
        ConfigureModules(config);

        SetState(UnitState.Active);

        DebugManager.Log($"Initialized: {characterData.displayName} (Team: {team})", DebugCategory.Combat);
    }

    private void ConfigureModules(UnitConfig config)
    {
        if (!config.enableDrag)
            GetModule<DragModule>()?.OnDisabled();

        if (!config.enableFootprint)
            GetModule<FootprintModule>()?.OnDisabled();
    }

    public void ResetToPool()
    {
        SetState(UnitState.Pooled);
        SetDragging(false);

        characterData = null;
        team = Team.Player;

        // Re-habilitar módulos para próximo uso
        GetModule<DragModule>()?.OnEnabled();
        GetModule<FootprintModule>()?.OnEnabled();
        GetModule<StatsModule>()?.ResetForPool();
        GetModule<CombatModule>()?.OnDisabled();
        GetModule<HealthBarModule>()?.ResetForPool();

        DebugManager.Log("Reset to pool", DebugCategory.Pool);
    }

    // === STATE MANAGEMENT ===

    public void SetState(UnitState newState)
    {
        if (currentState == newState) return;

        UnitState previousState = currentState;
        currentState = newState;

        OnStateChanged(previousState, newState);

        DebugManager.Log($"State: {previousState} → {newState}", DebugCategory.State);
    }

    private void OnStateChanged(UnitState oldState, UnitState newState)
    {
        var visualModule = GetModule<VisualModule>();
        if (visualModule == null) return;

        switch (newState)
        {
            case UnitState.Pooled:
                visualModule.SetDraggingAnimation(false);
                break;

            case UnitState.Active:
                // Animação controlada por drag state
                break;

            case UnitState.Combat:
                // Futuro: transição para animações de combate
                visualModule.SetDraggingAnimation(false);
                break;
        }
    }

    // === ANIMATION CONTROL ===

    public void SetDragging(bool isDragging)
    {
        DebugManager.Log($"SetDragging({isDragging}) - currentState: {currentState}", DebugCategory.Drag);

        if (currentState != UnitState.Active)
        {
            DebugManager.LogWarning($"SetDragging({isDragging}) called while not Active! Current state: {currentState}", DebugCategory.Drag);
            return;
        }

        var visualModule = GetModule<VisualModule>();
        if (visualModule != null)
        {
            visualModule.SetDraggingAnimation(isDragging);
        }
    }

    /// <summary>
    /// Chamado pelo Animation Event no frame de impacto do ataque.
    /// Repassa para o VisualModule que dispara o evento OnAttackHit.
    /// </summary>
    public void OnAttackHitFrame()
    {
        GetModule<VisualModule>()?.OnAttackHitFrame();
    }

    // === QUERIES ===

    public UnitState GetCurrentState() => currentState;
    public CombatState GetCombatState() => GetModule<CombatModule>()?.State ?? CombatState.Waiting;
    public CharacterData GetCharacterData() => characterData;
    public bool IsActive() => currentState == UnitState.Active;
    public bool IsInCombat() => currentState == UnitState.Combat;
    public bool IsPooled() => currentState == UnitState.Pooled;

    // Team queries
    public Team GetTeam() => team;
    public bool IsPlayerUnit() => team == Team.Player;
    public bool IsEnemyUnit() => team == Team.Enemy;
}

// === ENUMS ===

public enum UnitState
{
    Pooled,    // Unidade na pool (inativa)
    Active,    // Unidade ativa (preparação/tabuleiro)
    Combat     // Em combate (futuro)
}

