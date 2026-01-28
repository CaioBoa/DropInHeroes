using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UnitPool unitPool;
    [SerializeField] private GameObject battleUI;
    [SerializeField] private Button startCombatButton;

    [Header("Enemy Spawn")]
    [SerializeField] private Transform enemySpawnPoint;

    [Header("Test")]
    [SerializeField] private BattleData testBattleData;

    private bool isBattleActive = false;
    private BattleData currentBattle;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (startCombatButton != null)
        {
            startCombatButton.onClick.AddListener(OnStartCombatClicked);
            startCombatButton.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (startCombatButton != null)
        {
            startCombatButton.onClick.RemoveListener(OnStartCombatClicked);
        }
    }

    private void OnStartCombatClicked()
    {
        PreparationManager.Instance?.ConfirmPositioning();
    }

    public void Initialize()
    {
        // Inicializar pool de unidades
        if (unitPool != null)
        {
            unitPool.Initialize();
        }

        // Desativar UI de batalha inicialmente
        if (battleUI != null)
        {
            battleUI.SetActive(false);
        }

        DebugManager.Log("BattleManager inicializado e pronto!", DebugCategory.Combat);
    }

    /// <summary>
    /// Método para vincular ao botão (usa testBattleData do Inspector)
    /// </summary>
    public async void StartBattleButton()
    {
        await StartBattle(testBattleData);
    }

    public async Task<bool> StartBattle(BattleData battleData)
    {
        if (isBattleActive)
        {
            DebugManager.LogWarning("Batalha já está ativa!", DebugCategory.Combat);
            return false;
        }

        if (battleData == null)
        {
            DebugManager.LogError("BattleData é null!", DebugCategory.Combat);
            return false;
        }

        currentBattle = battleData;
        isBattleActive = true;

        // 1. Desativar mundo
        DeactivateOverworld();

        // 2. Ativar scene de batalha (já está carregada!)
        ActivateBattleScene();

        // 3. Spawnar inimigos
        SpawnEnemies();

        // 4. Iniciar fase de preparação
        if (startCombatButton != null)
            startCombatButton.gameObject.SetActive(true);

        await PreparationManager.Instance.StartPreparationPhase();

        if (startCombatButton != null)
            startCombatButton.gameObject.SetActive(false);

        // 5. Executar batalha
        bool playerWon = await ExecuteBattle();

        // 6. Despawnar inimigos
        DespawnEnemies();

        DeactivateBattleScene();
        ActivateOverworld();

        isBattleActive = false;
        currentBattle = null;

        return playerWon;
    }

    private async Task<bool> ExecuteBattle()
    {
        // Obter unidades do player do BoardManager
        var playerUnits = PreparationManager.Instance?.Board?.GetAllUnits();
        if (playerUnits == null || playerUnits.Count == 0)
        {
            DebugManager.LogWarning("Nenhuma unidade do player no board!", DebugCategory.Combat);
            return false;
        }

        // Obter UnitControllers dos inimigos spawnados
        var enemyControllers = spawnedEnemies
            .Select(e => e.GetComponent<UnitController>())
            .Where(c => c != null)
            .ToList();

        if (enemyControllers.Count == 0)
        {
            DebugManager.LogWarning("Nenhum inimigo spawnado!", DebugCategory.Combat);
            return true; // Player ganha se não houver inimigos
        }

        // Configurar callback para quando combate terminar
        var tcs = new TaskCompletionSource<Team>();
        void OnCombatEnded(Team winner) => tcs.TrySetResult(winner);

        CombatController.Instance.OnCombatEnded += OnCombatEnded;
        CombatController.Instance.StartCombat(playerUnits, enemyControllers);

        // Aguardar fim do combate
        Team combatWinner = await tcs.Task;

        CombatController.Instance.OnCombatEnded -= OnCombatEnded;

        bool playerWon = combatWinner == Team.Player;
        DebugManager.Log($"Resultado: {(playerWon ? "VITÓRIA!" : "DERROTA!")}", DebugCategory.Combat);
        return playerWon;
    }

    // === SCENE MANAGEMENT ===
    private void DeactivateOverworld()
    {
        Scene gameScene = SceneManager.GetSceneByName("Game");
        if (gameScene.isLoaded)
        {
            foreach (GameObject rootObj in gameScene.GetRootGameObjects())
            {
                // Não desativar managers persistentes
                if (!rootObj.CompareTag("PersistentManager"))
                {
                    rootObj.SetActive(false);
                }
            }
        }
    }

    private void ActivateBattleScene()
    {
        Scene battleScene = SceneManager.GetSceneByName("Battle");
        if (battleScene.isLoaded)
        {
            foreach (GameObject rootObj in battleScene.GetRootGameObjects())
            {
                rootObj.SetActive(true);
            }
        }

        if (battleUI != null)
        {
            battleUI.SetActive(true);
        }
    }

    private void DeactivateBattleScene()
    {
        Scene battleScene = SceneManager.GetSceneByName("Battle");
        if (battleScene.isLoaded)
        {
            foreach (GameObject rootObj in battleScene.GetRootGameObjects())
            {
                rootObj.SetActive(false);
            }
        }
    }

    private void ActivateOverworld()
    {
        Scene gameScene = SceneManager.GetSceneByName("Game");
        if (gameScene.isLoaded)
        {
            foreach (GameObject rootObj in gameScene.GetRootGameObjects())
            {
                rootObj.SetActive(true);
            }
        }
    }

    // === ENEMY SPAWNING ===

    private void SpawnEnemies()
    {
        if (currentBattle?.enemies == null || currentBattle.enemies.Length == 0)
        {
            DebugManager.LogWarning("Nenhum inimigo definido no BattleData!", DebugCategory.Combat);
            return;
        }

        if (unitPool == null)
        {
            DebugManager.LogError("UnitPool não atribuído!", DebugCategory.Combat);
            return;
        }

        Vector2 spawnOffset = enemySpawnPoint != null
            ? (Vector2)enemySpawnPoint.position
            : Vector2.zero;

        if (enemySpawnPoint == null)
        {
            DebugManager.LogWarning("EnemySpawnPoint não definido, usando (0,0)", DebugCategory.Combat);
        }

        foreach (EnemyData enemyData in currentBattle.enemies)
        {
            CharacterData charData = DataManager.GetCharacter(enemyData.characterId);

            if (charData == null)
            {
                DebugManager.LogWarning($"CharacterData não encontrado: {enemyData.characterId}", DebugCategory.Combat);
                continue;
            }

            Vector2 finalPosition = spawnOffset + enemyData.spawnPosition;

            GameObject enemy = unitPool.SpawnUnit(charData, finalPosition, UnitConfig.Enemy);
            spawnedEnemies.Add(enemy);

            DebugManager.Log($"Spawned enemy: {charData.displayName} at {finalPosition}", DebugCategory.Combat);
        }

        DebugManager.Log($"Total enemies spawned: {spawnedEnemies.Count}", DebugCategory.Combat);
    }

    private void DespawnEnemies()
    {
        if (unitPool == null) return;

        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                unitPool.ReturnUnit(enemy);
            }
        }

        spawnedEnemies.Clear();
        DebugManager.Log("Todos os inimigos retornados ao pool", DebugCategory.Combat);
    }
}
