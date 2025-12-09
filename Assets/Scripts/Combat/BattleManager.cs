using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UnitPool unitPool;
    [SerializeField] private GameObject battleUI;

    private bool isBattleActive = false;
    private BattleData currentBattle;

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

        Debug.Log("[BattleManager] Inicializado e pronto!");
    }

    public async Task<bool> StartBattle(BattleData battleData)
    {
        if (isBattleActive)
        {
            Debug.LogWarning("[BattleManager] Batalha já está ativa!");
            return false;
        }

        if (battleData == null)
        {
            Debug.LogError("[BattleManager] BattleData é null!");
            return false;
        }

        currentBattle = battleData;
        isBattleActive = true;

        // 1. Desativar mundo
        DeactivateOverworld();

        // 2. Ativar scene de batalha (já está carregada!)
        ActivateBattleScene();

        // 4. Executar batalha
        bool playerWon = await ExecuteBattle();

        DeactivateBattleScene();
        ActivateOverworld();

        isBattleActive = false;
        currentBattle = null;

        return playerWon;
    }

    private async Task<bool> ExecuteBattle()
    {
        // Placeholder para lógica de batalha
        Debug.Log("[BattleManager] Batalha em andamento...");

        await Task.Delay(10000);  // Simular batalha de 10 segundos

        bool playerWon = Random.value > 0.3f;  // 70% chance de vitória (teste)

        Debug.Log($"[BattleManager] Resultado: {(playerWon ? "VITÓRIA!" : "DERROTA!")}");
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
}
