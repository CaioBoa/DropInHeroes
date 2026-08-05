using UnityEngine;
using System;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Hub central que orquestra módulos especializados da unidade
    /// Gerencia estado geral e coordena módulos (Visual, Drag, Stats, etc)
    /// </summary>
    public class UnitController : MonoBehaviour
    {
        // === MODULE REGISTRY ===
        private Dictionary<Type, IUnitModule> modules = new Dictionary<Type, IUnitModule>();

        // Cache dos módulos lidos por frame no Update e em hot paths (targeting/projéteis/vfx) —
        // evita lookup de dicionário por frame. Os módulos vivem enquanto o GameObject existir
        // (limpos só no OnDestroy), então o cache sobrevive ao reuso de pool.
        private CombatModule combatModule;
        private StatsModule statsModule;
        private VisualModule visualModule;
        private StatusModule statusModule;
        private SummonModule summonModule;

        /// <summary>
        /// Resolve, por personagem, a build a equipar no deploy (setado pela camada de picks via TowerRunData).
        /// Null, ou retorno null, cai no CharacterData.defaultBuild. Mantém Combat desacoplado da Torre.
        /// </summary>
        public static Func<CharacterData, CharacterBuild> PickedBuildResolver;

        // === STATE ===
        private UnitState currentState = UnitState.Pooled;
        private CharacterData characterData;
        private Team team = Team.Player;
        private UnitTag tags = UnitTag.Hero;
        private UnitCapability capabilities = UnitCapability.All;

        // === LIFECYCLE ===

        private void Awake()
        {
            // Registrar módulos
            RegisterModule(new VisualModule());
            RegisterModule(new DragModule());
            RegisterModule(new FootprintModule());
            RegisterModule(new StatsModule());
            RegisterModule(new StatusModule());
            RegisterModule(new FocusModule());
            RegisterModule(new SkillsModule());
            RegisterModule(new MovementModule());
            RegisterModule(new CombatModule());
            RegisterModule(new SummonModule());
            RegisterModule(new OnHitModule());
            RegisterModule(new ShieldModule());
            RegisterModule(new LoadoutModule());
            RegisterModule(new HealthBarModule());
            RegisterModule(new StatusStripModule());
            RegisterModule(new EnergyBarModule());

            // Inicializar módulos
            foreach (var module in modules.Values)
            {
                module.Initialize(this);
            }

            combatModule = GetModule<CombatModule>();
            statsModule = GetModule<StatsModule>();
            visualModule = GetModule<VisualModule>();
            statusModule = GetModule<StatusModule>();
            summonModule = GetModule<SummonModule>();

            DebugManager.Log("Módulos registrados e inicializados", DebugCategory.Initialization);
        }

        private void Update()
        {
            if (currentState == UnitState.Combat)
            {
                combatModule?.Tick();
                visualModule?.Tick();
            }
        }

        private void OnDestroy()
        {
            // Pooled units não são destruídas em runtime; isto só roda no descarregamento
            // da cena, garantindo que módulos liberem assinaturas de evento.
            foreach (var module in modules.Values)
                module.Cleanup();
            modules.Clear();
        }

        // === MODULE MANAGEMENT ===

        private void RegisterModule(IUnitModule module)
        {
            Type moduleType = module.GetType();
            if (!modules.TryAdd(moduleType, module))
                DebugManager.LogWarning($"Módulo {moduleType.Name} já registrado!", DebugCategory.Initialization);
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
            // Categorias e capacidades vêm do dado; o spawn como invocação adiciona a tag Summon.
            tags = data.tags;
            if (config.isSummon) tags |= UnitTag.Summon;
            capabilities = data.capabilities;

            // Aplicar stats do CharacterData
            GetModule<StatsModule>()?.ApplyCharacterStats(data);

            // Loadout: build escolhida nos picks (via PickedBuildResolver) senão a padrão do personagem.
            CharacterBuild pickedBuild = PickedBuildResolver != null ? PickedBuildResolver(data) : null;
            GetModule<LoadoutModule>()?.Equip(pickedBuild != null ? pickedBuild : data.defaultBuild);

            // Aplicar visuais via VisualModule
            var visualModule = GetModule<VisualModule>();
            if (visualModule != null)
            {
                visualModule.ApplyCharacterVisuals(data);
                visualModule.ApplyCharacterAnimations(data);
                visualModule.ApplyIdleFacing();
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
            tags = UnitTag.Hero;
            capabilities = UnitCapability.All;
            Rank = 1;

            // Re-habilitar módulos para próximo uso e zerar estado visual/recursos
            GetModule<DragModule>()?.OnEnabled();
            GetModule<FootprintModule>()?.OnEnabled();
            GetModule<StatsModule>()?.ResetForPool();
            GetModule<StatusModule>()?.ClearAll();
            GetModule<FocusModule>()?.OnDisabled();
            GetModule<CombatModule>()?.OnDisabled();
            GetModule<MovementModule>()?.OnDisabled();
            GetModule<HealthBarModule>()?.ResetForPool();
            GetModule<EnergyBarModule>()?.ResetForPool();
            GetModule<VisualModule>()?.ResetForPool();
            GetModule<SummonModule>()?.ResetForPool();
            GetModule<OnHitModule>()?.ResetForPool();
            GetModule<ShieldModule>()?.ResetForPool();
            GetModule<LoadoutModule>()?.ResetForPool();

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

        /// <summary>
        /// Reatribui o time desta unidade e reorienta o idle para o lado adversário. Usado pelo modo
        /// Sandbox, onde o time é decidido pelo lado do campo em que a unidade foi posicionada (não no
        /// spawn). Chamar antes de <see cref="CombatController.StartCombat"/>.
        /// </summary>
        public void SetTeam(Team newTeam)
        {
            team = newTeam;
            visualModule?.ApplyIdleFacing();
        }

        // Categorias / capacidades (data-driven)
        public UnitTag Tags => tags;
        public UnitCapability Capabilities => capabilities;
        public bool HasTag(UnitTag t) => (tags & t) != 0;
        public bool HasCapability(UnitCapability c) => (capabilities & c) != 0;
        public bool IsSummon => HasTag(UnitTag.Summon);

        /// <summary>Módulos hot-path cacheados (ver campos): lidos por frame em targeting/projéteis/vfx.</summary>
        public StatsModule Stats => statsModule;
        public VisualModule Visual => visualModule;

        /// <summary>Pode ser focada? Falso sob furtividade (ver StatusModule). Default true.</summary>
        public bool IsTargetable => statusModule?.IsTargetable ?? true;

        /// <summary>Rank atual (1..maxRank). Setado em ApplyRank no spawn/deploy; lido pelo CharacterInfo.</summary>
        public int Rank { get; set; } = 1;

        // Grafo dono↔invocação — delegado ao SummonModule (mecânica de invocação coesa num módulo próprio).
        public UnitController Owner => summonModule?.Owner;
        public IReadOnlyList<UnitController> Summons => summonModule?.Summons;
        public void SetOwner(UnitController newOwner) => summonModule?.SetOwner(newOwner);
        public void UnregisterSummon(UnitController summon) => summonModule?.UnregisterSummon(summon);
    }

    // === ENUMS ===

    public enum UnitState
    {
        Pooled,    // Unidade na pool (inativa)
        Active,    // Unidade ativa (preparação/tabuleiro)
        Combat     // Em combate (futuro)
    }

}
