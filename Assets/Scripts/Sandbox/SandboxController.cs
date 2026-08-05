using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Data;
using DropInHeroes.Tower;
using DropInHeroes.Utils;

namespace DropInHeroes.Sandbox
{

    /// <summary>
    /// Orquestra o modo Sandbox: um campo livre para testar unidades, sem loja nem rounds. Reaproveita
    /// o pipeline de combate (CombatController), o posicionamento (PreparationManager/BoardManager) e o
    /// pool. O TIME de cada unidade é decidido pelo LADO do campo em que foi posicionada — resolvido só
    /// no <see cref="StartMatch"/>, particionando o board pela linha média. Expõe controles de teste
    /// (iniciar/parar/reiniciar/limpar), velocidade de exibição (Time.timeScale), timer máximo e um
    /// dummy com defesa/defesa mágica/vida customizáveis.
    /// </summary>
    public class SandboxController : MonoBehaviour
    {
        // Vida "infinita" do dummy imortal: alta o bastante para não morrer num teste, sem estourar float.
        private const float ImmortalHealth = 1_000_000_000f;

        [System.Serializable]
        public class SandboxPick
        {
            public CharacterData character;
            // [SerializeReference] p/ o campo aceitar NULL de verdade — sem ele, o serializador do Unity
            // instancia uma CharacterBuild vazia (nunca null), o que quebraria o fallback pro defaultBuild.
            [SerializeReference]
            [Tooltip("Build a equipar. Null → defaultBuild do personagem.")]
            public CharacterBuild build;
            [Range(1, 3)] public int rank = 1;
        }

        [Header("Referências de cena")]
        [SerializeField] private UnitPool unitPool;
        [Tooltip("X de mundo que separa aliados (à esquerda) de inimigos (à direita).")]
        [SerializeField] private float midlineX = 0f;

        [Header("Roster do Sandbox (config de build/rank por personagem)")]
        [Tooltip("Preenchido em runtime pelo painel esquerdo (SandboxRoster) a partir de todos os personagens jogáveis; pode ser pré-semeado no Inspector.")]
        [SerializeField] private List<SandboxPick> picks = new List<SandboxPick>();

        [Header("Dummy")]
        [SerializeField] private CharacterData dummyCharacter;
        [SerializeField] private float dummyDefense = 0f;
        [SerializeField] private float dummyMagicalDefense = 0f;
        [SerializeField] private float dummyHealth = 1000f;
        [SerializeField] private bool dummyImmortal = true;

        [Header("Partida")]
        [Tooltip("Duração máxima da partida em segundos (tempo de jogo, afetado pela velocidade). <= 0 = sem limite.")]
        [SerializeField] private float maxMatchSeconds = 0f;
        [Tooltip("Velocidade inicial de exibição (Time.timeScale).")]
        [SerializeField] private float initialSpeed = 1f;

        private BoardManager Board => PreparationManager.Instance != null ? PreparationManager.Instance.Board : null;

        private readonly List<UnitController> playersBuffer = new List<UnitController>();
        private readonly List<UnitController> enemiesBuffer = new List<UnitController>();
        private readonly List<FormationEntry> savedFormation = new List<FormationEntry>();

        private bool matchRunning;
        private float elapsed;
        private float speedBeforePause = 1f;

        private struct FormationEntry
        {
            public CharacterData character;
            public Vector2 position;
            public int rank;
        }

        // === LIFECYCLE ===

        private void Awake()
        {
            // O deploy pergunta a build por aqui (mesmo contrato do TowerRunData). Cada pick é único,
            // então chavear por personagem basta.
            UnitController.PickedBuildResolver = ResolveBuild;
            // O feedback de drag consulta isto para tingir o footprint quando a posição cai no lado inimigo.
            PreparationManager.EnemySidePlacement = IsEnemySide;
        }

        /// <summary>True se a posição de mundo cai no lado inimigo (direita da linha média).</summary>
        private bool IsEnemySide(Vector2 worldPos) => worldPos.x > midlineX;

        private void Start()
        {
            SetSpeed(initialSpeed);

            if (CombatController.Instance != null)
                CombatController.Instance.OnCombatEnded += HandleCombatEnded;

            if (Board != null)
            {
                Board.OnUnitAdded += HandleBoardUnitAdded;
                Board.OnUnitRemoved += HandleBoardUnitRemoved;
            }
            else
            {
                DebugManager.LogError("SandboxController: PreparationManager/Board não encontrado na cena.", DebugCategory.Combat);
            }
        }

        private void OnDestroy()
        {
            // Não vazar timeScale lento/pausado para outras cenas.
            Time.timeScale = 1f;

            if (UnitController.PickedBuildResolver == ResolveBuild)
                UnitController.PickedBuildResolver = null;

            if (PreparationManager.EnemySidePlacement == IsEnemySide)
                PreparationManager.EnemySidePlacement = null;

            if (CombatController.Instance != null)
                CombatController.Instance.OnCombatEnded -= HandleCombatEnded;

            if (Board != null)
            {
                Board.OnUnitAdded -= HandleBoardUnitAdded;
                Board.OnUnitRemoved -= HandleBoardUnitRemoved;
            }
        }

        private void Update()
        {
            if (!matchRunning || maxMatchSeconds <= 0f) return;

            elapsed += Time.deltaTime;
            if (elapsed >= maxMatchSeconds)
                TimeoutMatch();
        }

        // === BUILD RESOLVER (por personagem) ===

        private CharacterBuild ResolveBuild(CharacterData character)
        {
            if (character == null) return null;
            for (int i = 0; i < picks.Count; i++)
                if (picks[i] != null && picks[i].character == character)
                    return picks[i].build;
            return null;
        }

        private SandboxPick FindPick(CharacterData character)
        {
            for (int i = 0; i < picks.Count; i++)
                if (picks[i] != null && picks[i].character == character)
                    return picks[i];
            return null;
        }

        /// <summary>
        /// Config (build/rank) de um personagem, criando a entrada se ainda não existir. Usado pelo painel
        /// esquerdo (SandboxRoster) para ter uma linha por personagem jogável sem precisar pré-listar tudo.
        /// </summary>
        public SandboxPick GetOrCreatePick(CharacterData character)
        {
            if (character == null) return null;
            SandboxPick pick = FindPick(character);
            if (pick != null) return pick;
            pick = new SandboxPick { character = character };
            picks.Add(pick);
            return pick;
        }

        // === BOARD EVENTS ===

        private void HandleBoardUnitAdded(UnitController unit)
        {
            if (unit == null) return;

            // Idle voltado para o lado adversário: aliado (esquerda) encara a direita; inimigo (direita)
            // encara a esquerda. Vale também ao re-arrastar uma unidade para o outro lado.
            unit.SetTeam(IsEnemySide(unit.transform.position) ? Team.Enemy : Team.Player);

            CharacterData data = unit.GetCharacterData();
            if (dummyCharacter != null && data == dummyCharacter)
            {
                ApplyDummyStats(unit);
                return;
            }

            SandboxPick pick = FindPick(data);
            if (pick != null) unit.Rank = Mathf.Clamp(pick.rank, 1, 3);
        }

        private void HandleBoardUnitRemoved(UnitController unit) { /* nada por ora */ }

        // === MATCH LIFECYCLE ===

        /// <summary>Inicia a partida: particiona as unidades do board por lado e chama o combate.</summary>
        public void StartMatch()
        {
            if (matchRunning || CombatController.Instance == null || Board == null) return;

            playersBuffer.Clear();
            enemiesBuffer.Clear();

            IReadOnlyList<UnitController> boardUnits = Board.GetAllUnits();
            SnapshotFormation(boardUnits);

            for (int i = 0; i < boardUnits.Count; i++)
            {
                UnitController unit = boardUnits[i];
                if (unit == null) continue;
                bool isPlayer = !IsEnemySide(unit.transform.position);
                unit.SetTeam(isPlayer ? Team.Player : Team.Enemy);
                (isPlayer ? playersBuffer : enemiesBuffer).Add(unit);
            }

            if (playersBuffer.Count == 0 || enemiesBuffer.Count == 0)
            {
                DebugManager.LogWarning($"Sandbox: cada lado precisa de ao menos 1 unidade (aliados {playersBuffer.Count}, inimigos {enemiesBuffer.Count}).", DebugCategory.Combat);
                return;
            }

            elapsed = 0f;
            matchRunning = true;
            CombatController.Instance.StartCombat(playersBuffer, enemiesBuffer);
            DebugManager.Log($"Sandbox: partida iniciada ({playersBuffer.Count} vs {enemiesBuffer.Count}).", DebugCategory.Combat);
        }

        /// <summary>Para a partida e restaura a formação salva no board (estado de posicionamento).</summary>
        public void StopMatch()
        {
            matchRunning = false;
            CombatController.Instance?.AbortCombat();
            ClearBoard();
            RestoreFormation();
        }

        /// <summary>Reinicia: para, restaura a formação e recomeça o combate na mesma disposição.</summary>
        public void RestartMatch()
        {
            StopMatch();
            StartMatch();
        }

        private void TimeoutMatch()
        {
            matchRunning = false;
            Team winner = TimeoutResolver.DecideWinner(playersBuffer, enemiesBuffer);
            CombatController.Instance?.ForceEndCombat(winner);
            DebugManager.Log($"Sandbox: timer máximo atingido — vencedor por critério: {winner}.", DebugCategory.Combat);
        }

        private void HandleCombatEnded(Team winner)
        {
            matchRunning = false;
        }

        // === FORMATION SNAPSHOT ===

        private void SnapshotFormation(IReadOnlyList<UnitController> boardUnits)
        {
            savedFormation.Clear();
            for (int i = 0; i < boardUnits.Count; i++)
            {
                UnitController unit = boardUnits[i];
                if (unit == null) continue;
                savedFormation.Add(new FormationEntry
                {
                    character = unit.GetCharacterData(),
                    position = unit.transform.position,
                    rank = unit.Rank
                });
            }
        }

        private void RestoreFormation()
        {
            if (unitPool == null || Board == null) return;
            for (int i = 0; i < savedFormation.Count; i++)
            {
                FormationEntry entry = savedFormation[i];
                if (entry.character == null) continue;

                GameObject obj = unitPool.SpawnUnit(entry.character, entry.position, UnitConfig.Player);
                UnitController unit = obj != null ? obj.GetComponent<UnitController>() : null;
                if (unit == null) continue;

                unit.Rank = entry.rank;
                Board.AddUnit(unit); // dispara HandleBoardUnitAdded (rank/dummy stats)
            }
        }

        // === CLEAR ===

        /// <summary>Remove todas as unidades do campo (board) e as devolve ao pool.</summary>
        public void ClearField()
        {
            matchRunning = false;
            CombatController.Instance?.AbortCombat();
            ClearBoard();
            savedFormation.Clear();
        }

        private void ClearBoard()
        {
            if (Board == null || unitPool == null) return;
            IReadOnlyList<UnitController> boardUnits = Board.GetAllUnits();
            if (boardUnits.Count == 0) return;

            var snapshot = new List<UnitController>(boardUnits);
            for (int i = 0; i < snapshot.Count; i++)
            {
                UnitController unit = snapshot[i];
                if (unit == null) continue;
                Board.RemoveUnit(unit);
                unitPool.ReturnUnit(unit.gameObject);
            }
        }

        // === DUMMY ===

        private void ApplyDummyStats(UnitController dummy)
        {
            StatsModule stats = dummy.Stats;
            if (stats == null) return;

            stats.GetStatObject(StatType.Defense)?.SetBaseValue(dummyDefense);
            stats.GetStatObject(StatType.MagicalDefense)?.SetBaseValue(dummyMagicalDefense);
            stats.GetStatObject(StatType.MaxHealth)?.SetBaseValue(dummyImmortal ? ImmortalHealth : dummyHealth);
            stats.GetResourceObject(ResourceType.Health)?.SetToMax();
        }

        /// <summary>Atualiza a config do dummy e re-aplica aos dummies já em campo.</summary>
        public void ConfigureDummy(float defense, float magicalDefense, float health, bool immortal)
        {
            dummyDefense = defense;
            dummyMagicalDefense = magicalDefense;
            dummyHealth = health;
            dummyImmortal = immortal;

            if (Board == null || dummyCharacter == null) return;
            IReadOnlyList<UnitController> boardUnits = Board.GetAllUnits();
            for (int i = 0; i < boardUnits.Count; i++)
                if (boardUnits[i] != null && boardUnits[i].GetCharacterData() == dummyCharacter)
                    ApplyDummyStats(boardUnits[i]);
        }

        // === SPEED / PAUSE / TIMER (overlay) ===

        public void SetSpeed(float speed)
        {
            Time.timeScale = Mathf.Max(0f, speed);
        }

        public void SetPaused(bool paused)
        {
            if (paused)
            {
                speedBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = speedBeforePause;
            }
        }

        public void SetMaxMatchSeconds(float seconds) => maxMatchSeconds = seconds;

        // Acesso para o overlay popular/editar o roster.
        public List<SandboxPick> Picks => picks;
        public CharacterData DummyCharacter => dummyCharacter;
        public bool IsMatchRunning => matchRunning;
    }
}
