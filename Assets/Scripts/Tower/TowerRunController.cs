using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Orquestra a run completa de Torre (state machine principal):
    /// Setup → [Prep+Shop → Battle → Resolve] × até N fases → End.
    /// </summary>
    public class TowerRunController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private TowerRunConfig config;

        [Header("References")]
        [SerializeField] private UnitPool unitPool;
        [SerializeField] private TowerShop shop;
        [SerializeField] private TowerRunHUD hud;
        [SerializeField] private GameOverPanel gameOverPanel;

        [Header("Bench UI")]
        [SerializeField] private BenchSlotUI[] benchSlotUIs = new BenchSlotUI[5];

        private TowerBench bench;
        private List<CharacterData> shopPoolBuffer = new List<CharacterData>();
        private int currentPhaseIndex; // 0-based
        private int playerHP;
        private int towerHP;
        private bool isHandlingConfirm;

        // Colaboradores (criados em InitializeRun): cada um cuida de uma responsabilidade.
        private EnemySpawner enemySpawner;
        private PhaseTimer phaseTimer;
        private Action<Team> pendingCombatHandler;

        private void Awake()
        {
            // Forçar esconder o painel de fim de jogo o mais cedo possível
            if (gameOverPanel != null) gameOverPanel.Hide();
        }

        private async void Start()
        {
            DebugManager.Log("TowerRunController: Start() iniciado", DebugCategory.Combat);
        
            try 
            {
                if (DataManager.Instance != null)
                {
                    DebugManager.Log("TowerRunController: Aguardando DataManager...", DebugCategory.Combat);
                    await DataManager.Instance.WaitForInitialization();
                }

                if (!ValidateReferences()) 
                {
                    DebugManager.LogError("TowerRunController: Validação falhou! Abortando run.", DebugCategory.Combat);
                    return;
                }

                DebugManager.Log("TowerRunController: Inicializando Run...", DebugCategory.Combat);
                InitializeRun();

                await RunSequence();
            }
            catch (OperationCanceledException)
            {
                DebugManager.LogWarning("TowerRunController: Run cancelada", DebugCategory.Combat);
            }
            catch (Exception e)
            {
                DebugManager.LogError($"TowerRunController: Erro fatal durante run: {e.Message}\n{e.StackTrace}", DebugCategory.Combat);
            }
        }

        private bool ValidateReferences()
    {
            if (config == null) { DebugManager.LogError("TowerRunConfig não atribuído!", DebugCategory.Combat); return false; }
            if (config.ladder == null) { DebugManager.LogError("Ladder não atribuído no TowerRunConfig!", DebugCategory.Combat); return false; }
            if (unitPool == null) { DebugManager.LogError("UnitPool não atribuído!", DebugCategory.Combat); return false; }
            if (shop == null) { DebugManager.LogError("TowerShop não atribuído!", DebugCategory.Combat); return false; }
            if (PreparationManager.Instance == null) { DebugManager.LogError("PreparationManager não encontrado na cena!", DebugCategory.Combat); return false; }
            if (CombatController.Instance == null) { DebugManager.LogError("CombatController não encontrado na cena!", DebugCategory.Combat); return false; }

            var picks = TowerRunData.SelectedCharacters;
            if (picks == null || picks.Count == 0)
            {
                DebugManager.LogError($"TowerRunData vazio! Count: {(picks == null ? "null" : picks.Count.ToString())}", DebugCategory.Combat);
                return false;
            }
            return true;
        }

        private void InitializeRun()
        {
            // Garantir que o painel esteja escondido
            if (gameOverPanel != null) gameOverPanel.Hide();

            DebugManager.Log($"TowerRunController: InitializeRun. Phases: {config.ladder.PhaseCount}, HP: {config.playerStartingHP}/{config.towerStartingHP}, Characters: {TowerRunData.Count}", DebugCategory.Combat);

            enemySpawner = new EnemySpawner(config, unitPool);
            phaseTimer = new PhaseTimer(hud);

            unitPool.Initialize(enemySpawner.ComputePoolPreload());

            playerHP = config.playerStartingHP;
            towerHP = config.towerStartingHP;
            currentPhaseIndex = 0;

            bench = new TowerBench(TowerRunData.Count, config.maxRank);
            bench.OnEntryChanged += HandleBenchEntryChanged;

            BindBenchSlots();

            UpdateHUD();
            // Desinscreve antes de inscrever: estes alvos (hud, CombatController, Board) sobrevivem
            // à run; se InitializeRun rodar de novo sem recriar o objeto, sem isto duplicaria handlers.
            if (hud != null)
            {
                hud.SetTimer(-1f);
                hud.OnStartCombatPressed -= HandleStartCombatPressed;
                hud.OnStartCombatPressed += HandleStartCombatPressed;
            }

            CombatController.Instance.OnCombatEnded -= HandleCombatEnded;
            CombatController.Instance.OnCombatEnded += HandleCombatEnded;

            var board = PreparationManager.Instance.Board;
            if (board != null)
            {
                board.OnUnitAdded -= HandleBoardUnitAdded;
                board.OnUnitAdded += HandleBoardUnitAdded;
                board.OnUnitRemoved -= HandleBoardUnitRemoved;
                board.OnUnitRemoved += HandleBoardUnitRemoved;
            }
            else
            {
                DebugManager.LogError("TowerRunController: BoardManager não encontrado no PreparationManager!", DebugCategory.Combat);
            }
        }

        private void OnDestroy()
        {
            phaseTimer?.Cancel();
            if (bench != null) bench.OnEntryChanged -= HandleBenchEntryChanged;
            if (CombatController.Instance != null) CombatController.Instance.OnCombatEnded -= HandleCombatEnded;
            if (hud != null) hud.OnStartCombatPressed -= HandleStartCombatPressed;
            if (PreparationManager.Instance != null && PreparationManager.Instance.Board != null)
    {
                PreparationManager.Instance.Board.OnUnitAdded -= HandleBoardUnitAdded;
                PreparationManager.Instance.Board.OnUnitRemoved -= HandleBoardUnitRemoved;
            }
        }

        private void HandleStartCombatPressed()
        {
            if (isHandlingConfirm) return;
            isHandlingConfirm = true;

            DebugManager.Log("TowerRunController: Botão Iniciar Combate pressionado!", DebugCategory.Combat);
            PreparationManager.Instance.ConfirmPositioning();
        }

        private void HandleBoardUnitAdded(UnitController unit)
    {
            if (unit == null) return;
            BenchEntry entry = bench.FindEntry(unit.GetCharacterData());
            if (entry == null) return;
            entry.DeployedController = unit;
            RefreshSlotForEntry(entry);
        }

        private void HandleBoardUnitRemoved(UnitController unit)
        {
            if (unit == null) return;
            BenchEntry entry = bench.FindEntryByController(unit);
            if (entry == null) return;
            entry.DeployedController = null;
            RefreshSlotForEntry(entry);
        }

        private void RefreshSlotForEntry(BenchEntry entry)
        {
            for (int i = 0; i < benchSlotUIs.Length; i++)
            {
                if (benchSlotUIs[i] != null && benchSlotUIs[i].Entry == entry)
                {
                    benchSlotUIs[i].Refresh();
                    return;
                }
            }
        }

        // === STATE MACHINE ===

        private async Task RunSequence()
        {
            int totalPhases = config.ladder.PhaseCount;
            DebugManager.Log($"TowerRunController: RunSequence - totalPhases={totalPhases}, playerHP={playerHP}, towerHP={towerHP}, currentPhaseIndex={currentPhaseIndex}", DebugCategory.Combat);

            while (currentPhaseIndex < totalPhases && playerHP > 0 && towerHP > 0)
            {
                UpdateHUD();
                DebugManager.Log($"--- Fase {currentPhaseIndex + 1}/{totalPhases} ---", DebugCategory.Combat);

                await RunPrepPhase();
                Team winner = await RunBattlePhase();
                ResolvePhase(winner);

                currentPhaseIndex++;
            }

            DebugManager.Log("TowerRunController: Loop finalizado, chamando EndRun.", DebugCategory.Combat);
            EndRun();
        }

        // === PREP ===

        private async Task RunPrepPhase()
        {
            RespawnPreviousFormation();

            if (hud != null) hud.SetState("Loja");

            BuildShopPool();
            if (shopPoolBuffer.Count > 0)
            {
                DebugManager.Log($"Abrindo loja ({shopPoolBuffer.Count} disponíveis)...", DebugCategory.UI);
                CharacterData chosen = await shop.OpenAndAwaitChoice(shopPoolBuffer, config.shopOptionsCount);
                if (chosen != null)
                {
                    bench.BuyOrUpgrade(chosen);
                    DebugManager.Log($"Compra: {chosen.displayName}", DebugCategory.UI);
                }
            }
            else
            {
                DebugManager.Log("Bench inteiro no rank máximo — loja pulada", DebugCategory.UI);
            }

            if (hud != null) 
            {
                hud.SetState("Preparacao");
                hud.SetStartCombatButtonVisible(true);
            }

            Task timerTask = phaseTimer.Run(config.prepTimeSeconds);
            Task confirmTask = PreparationManager.Instance.StartPreparationPhase();

            isHandlingConfirm = false; // Resetar flag no início da espera
            Task completed = await Task.WhenAny(timerTask, confirmTask);

            if (hud != null) hud.SetStartCombatButtonVisible(false);

            if (completed == timerTask && !confirmTask.IsCompleted)
            {
                // Timer expirou: auto-confirma
                DebugManager.Log("Timer de preparação expirou — auto-confirmando", DebugCategory.Combat);
                PreparationManager.Instance.ConfirmPositioning();
                await confirmTask;
            }
            else
            {
                phaseTimer.Cancel();
            }

            if (hud != null) hud.SetTimer(-1f);
        }

        // === BATTLE ===

        private async Task<Team> RunBattlePhase()
        {
            SnapshotPlayerFormation();

            if (hud != null) hud.SetState("Batalha");

            var playerUnits = PreparationManager.Instance.Board.GetAllUnits();
            if (playerUnits == null || playerUnits.Count == 0)
            {
                DebugManager.LogWarning("Nenhuma unidade do jogador no board — derrota imediata", DebugCategory.Combat);
                return Team.Enemy;
            }

            var enemies = enemySpawner.SpawnForPhase(currentPhaseIndex);
            if (enemies.Count == 0)
            {
                DebugManager.LogWarning("Nenhum inimigo na fase — vitória imediata", DebugCategory.Combat);
                return Team.Player;
            }

            var combatTcs = new TaskCompletionSource<Team>();
            Action<Team> handler = w => combatTcs.TrySetResult(w);
            pendingCombatHandler = handler;

            // Snapshot para tiebreak antes do combate alterar HPs
            var playerSnapshot = new List<UnitController>(playerUnits);
            var enemySnapshot = new List<UnitController>(enemies);

            CombatController.Instance.StartCombat(playerUnits, enemies);

            Task timerTask = phaseTimer.Run(config.battleTimeSeconds);
            Task<Team> combatTask = combatTcs.Task;

            Task completed = await Task.WhenAny(timerTask, combatTask);

            Team winner;
            if (completed == timerTask && !combatTask.IsCompleted)
            {
                winner = TimeoutResolver.DecideWinner(playerSnapshot, enemySnapshot);
                DebugManager.Log($"Timeout de batalha — vencedor por critério: {winner}", DebugCategory.Combat);
                CombatController.Instance.ForceEndCombat(winner);
                await combatTask;
            }
            else
            {
                winner = await combatTask;
                phaseTimer.Cancel();
            }

            pendingCombatHandler = null;
            if (hud != null) hud.SetTimer(-1f);

            return winner;
        }

        private void HandleCombatEnded(Team winner)
        {
            pendingCombatHandler?.Invoke(winner);
        }

        // === RESOLVE ===

        private void ResolvePhase(Team winner)
        {
            if (winner == Team.Player) towerHP--;
            else playerHP--;

            enemySpawner.DespawnAll();
            DespawnAllPlayerUnits();

            // Resetar zoom/UI mesmo quando a fase termina sem combate real (sem inimigos),
            // pois nesse caso CombatController.OnCombatEnded não dispara.
            PreparationManager.Instance.ResetTransition();

            UpdateHUD();
            DebugManager.Log($"Fase {currentPhaseIndex + 1} resolvida. Vencedor: {winner}. Player {playerHP} / Tower {towerHP}", DebugCategory.Combat);
        }

        /// <summary>
        /// Pool da loja: picks (TowerRunData) cuja entry no bench ainda permite compra
        /// — entry inexistente (ainda não comprado) OU existente com rank abaixo do teto.
        /// </summary>
        private void BuildShopPool()
        {
            shopPoolBuffer.Clear();
            var picks = TowerRunData.SelectedCharacters;
            for (int i = 0; i < picks.Count; i++)
            {
                var character = picks[i];
                var entry = bench.FindEntry(character);
                if (entry == null || entry.Rank < config.maxRank)
                    shopPoolBuffer.Add(character);
            }
        }

        private void EndRun()
        {
            DebugManager.Log($"TowerRunController: EndRun chamado. Player HP: {playerHP}, Tower HP: {towerHP}, Phase: {currentPhaseIndex + 1}", DebugCategory.Combat);
            bool playerWon = playerHP > 0;
            DebugManager.Log(playerWon ? "RUN: VITÓRIA!" : "RUN: DERROTA", DebugCategory.Combat);
            if (hud != null) hud.SetState(playerWon ? "Vitoria!" : "Derrota");

            // Resumo: vitórias = quantas vidas a torre perdeu; derrotas = quantas o jogador perdeu.
            int roundsWon = config.towerStartingHP - towerHP;
            int roundsLost = config.playerStartingHP - playerHP;

            if (gameOverPanel != null)
                gameOverPanel.Show(playerWon, FloorReachedName(), roundsWon, roundsLost, playerHP);
        }

        // Nome do último andar enfrentado (vencido = topo alcançado; derrotado = onde caiu).
        private string FloorReachedName()
        {
            int lastIndex = Mathf.Clamp(currentPhaseIndex - 1, 0, config.ladder.PhaseCount - 1);
            TowerPhase phase = config.ladder.GetPhase(lastIndex);
            return phase != null ? phase.displayName : $"Andar {currentPhaseIndex}";
        }

        // === BENCH / DEPLOY ===

        private void BindBenchSlots()
        {
            for (int i = 0; i < benchSlotUIs.Length; i++)
            {
                if (benchSlotUIs[i] == null) continue;
                benchSlotUIs[i].Bind(bench.GetEntry(i));
            }
        }

        private void HandleBenchEntryChanged(BenchEntry entry)
        {
            // Atualiza UI do slot correspondente
            for (int i = 0; i < benchSlotUIs.Length; i++)
            {
                if (benchSlotUIs[i] != null && benchSlotUIs[i].Entry == entry)
                {
                    benchSlotUIs[i].Refresh();
                }
            }

            // Atualiza o rank do controller já desplegado; a passiva re-lê o rank no início do combate.
            if (entry.DeployedController != null && entry.Rank > 0)
            {
                entry.DeployedController.Rank = entry.Rank;
            }
        }

        /// <summary>
        /// Antes da batalha: salva posição final do prep de cada unidade no board
        /// para que o respawn na próxima prep recrie a formação intacta.
        /// </summary>
        private void SnapshotPlayerFormation()
        {
            var boardUnits = PreparationManager.Instance.Board.GetAllUnits();
            int count = 0;
            for (int i = 0; i < boardUnits.Count; i++)
            {
                var unit = boardUnits[i];
                if (unit == null) continue;
                var entry = bench.FindEntryByController(unit);
                if (entry == null) continue;
                entry.LastBoardPosition = unit.transform.position;
                entry.WasDeployed = true;
                count++;
            }
            if (count > 0) DebugManager.Log($"Formação capturada: {count} unidade(s)", DebugCategory.Combat);
        }

        /// <summary>
        /// Início de cada prep: re-spawna do pool (state zerado) cada unidade
        /// que estava deployada na fase anterior, na posição salva.
        /// </summary>
        private void RespawnPreviousFormation()
        {
            int count = 0;
            for (int i = 0; i < bench.Entries.Count; i++)
            {
                var entry = bench.Entries[i];
                if (entry.IsEmpty || !entry.WasDeployed || entry.DeployedController != null) continue;

                GameObject obj = unitPool.SpawnUnit(entry.Character, entry.LastBoardPosition, UnitConfig.Player);
                UnitController controller = obj != null ? obj.GetComponent<UnitController>() : null;
                if (controller == null) continue;

                controller.Rank = entry.Rank;
                PreparationManager.Instance.Board.AddUnit(controller);
                count++;
            }
            if (count > 0) DebugManager.Log($"Re-spawn da formação anterior: {count} unidade(s)", DebugCategory.Combat);
        }

        /// <summary>
        /// Pós-combate: devolve TODAS as unidades do jogador ao pool (reset completo
        /// de HP/stats/animator/etc). DeployedController é zerado via OnUnitRemoved.
        /// </summary>
        private void DespawnAllPlayerUnits()
        {
            var boardUnits = PreparationManager.Instance.Board.GetAllUnits();
            if (boardUnits.Count == 0) return;
            var snapshot = new List<UnitController>(boardUnits);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var unit = snapshot[i];
                if (unit == null) continue;
                PreparationManager.Instance.Board.RemoveUnit(unit);
                unitPool.ReturnUnit(unit.gameObject);
            }
            DebugManager.Log($"Reset de {snapshot.Count} unidade(s) do jogador entre fases", DebugCategory.Pool);
        }

        // === HUD ===

        private void UpdateHUD()
        {
            if (hud == null) return;
            hud.SetPhase(Mathf.Min(currentPhaseIndex + 1, config.ladder.PhaseCount), config.ladder.PhaseCount);
            hud.SetPlayerHP(playerHP);
            hud.SetTowerHP(towerHP);
        }
    }
}
