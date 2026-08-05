using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Spawna/despawna os inimigos de cada fase a partir do TowerRunConfig (ladder), aplicando o rank,
    /// e dimensiona o pool. Mantém a lista de inimigos vivos da fase atual.
    /// </summary>
    public class EnemySpawner
    {
        private const int DefaultMaxField = 3;

        private readonly TowerRunConfig config;
        private readonly UnitPool unitPool;
        private readonly List<UnitController> enemies = new List<UnitController>();

        public EnemySpawner(TowerRunConfig config, UnitPool unitPool)
        {
            this.config = config;
            this.unitPool = unitPool;
        }

        /// <summary>Spawna os inimigos da fase e retorna a lista (vazia se a fase não tiver inimigos).</summary>
        public List<UnitController> SpawnForPhase(int phaseIndex)
        {
            enemies.Clear();
            TowerPhase phase = config.ladder.GetPhase(phaseIndex);
            if (phase == null)
            {
                DebugManager.LogError($"Phase[{phaseIndex}] é null no ladder!", DebugCategory.Combat);
                return enemies;
            }
            if (phase.enemies == null)
            {
                DebugManager.LogError($"Phase[{phaseIndex}].enemies é null!", DebugCategory.Combat);
                return enemies;
            }

            DebugManager.Log($"Spawnando {phase.enemies.Length} inimigo(s) — fase {phaseIndex + 1}, origem {config.enemySpawnOrigin}", DebugCategory.Combat);

            for (int i = 0; i < phase.enemies.Length; i++)
            {
                UnitSpawn spawn = phase.enemies[i];
                if (string.IsNullOrEmpty(spawn.characterId))
                {
                    DebugManager.LogWarning($"Enemy[{i}] tem characterId vazio — pulando", DebugCategory.Combat);
                    continue;
                }

                CharacterData charData = DataManager.GetCharacter(spawn.characterId);
                if (charData == null)
                {
                    DebugManager.LogWarning($"CharacterData '{spawn.characterId}' não encontrado no catálogo — pulando", DebugCategory.Combat);
                    continue;
                }

                Vector2 pos = config.enemySpawnOrigin + spawn.spawnPosition;
                GameObject obj = unitPool.SpawnUnit(charData, pos, UnitConfig.Enemy);
                if (obj == null)
                {
                    DebugManager.LogError($"unitPool.SpawnUnit retornou null para '{charData.displayName}'", DebugCategory.Combat);
                    continue;
                }

                UnitController controller = obj.GetComponent<UnitController>();
                if (controller == null)
                {
                    DebugManager.LogError($"GameObject spawnado sem UnitController para '{charData.displayName}'", DebugCategory.Combat);
                    continue;
                }

                controller.Rank = spawn.rank;
                enemies.Add(controller);
                DebugManager.Log($"Inimigo spawnado: {charData.displayName} em {pos} (rank {spawn.rank})", DebugCategory.Combat);
            }

            DebugManager.Log($"Total inimigos spawnados: {enemies.Count}", DebugCategory.Combat);
            return enemies;
        }

        public void DespawnAll()
        {
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] != null) unitPool.ReturnUnit(enemies[i].gameObject);
            enemies.Clear();
        }

        /// <summary>
        /// Pico exato de unidades concorrentes para dimensionar o pool sem Instantiate em runtime:
        /// (campo do jogador + invocações dos picks) + (pior fase inimiga + invocações dela).
        /// </summary>
        public int ComputePoolPreload()
        {
            int maxField = (PreparationManager.Instance != null && PreparationManager.Instance.Config != null)
                ? PreparationManager.Instance.Config.maxUnitsOnField : DefaultMaxField;

            int playerSummons = 0;
            var picks = TowerRunData.SelectedCharacters;
            if (picks != null)
                for (int i = 0; i < picks.Count; i++)
                    if (picks[i] != null) playerSummons += Mathf.Max(0, picks[i].maxSummons);
            int playerPeak = maxField + playerSummons;

            int enemyPeak = 0;
            if (config != null && config.ladder != null)
            {
                for (int p = 0; p < config.ladder.PhaseCount; p++)
                {
                    TowerPhase phase = config.ladder.GetPhase(p);
                    if (phase == null || phase.enemies == null) continue;

                    int count = 0;
                    int summons = 0;
                    for (int e = 0; e < phase.enemies.Length; e++)
                    {
                        UnitSpawn spawn = phase.enemies[e];
                        if (spawn == null || string.IsNullOrEmpty(spawn.characterId)) continue;
                        count++;
                        CharacterData cd = DataManager.GetCharacter(spawn.characterId);
                        if (cd != null) summons += Mathf.Max(0, cd.maxSummons);
                    }
                    enemyPeak = Mathf.Max(enemyPeak, count + summons);
                }
            }

            int preload = playerPeak + enemyPeak;
            DebugManager.Log($"Pool preload calculado: {preload} (jogador {playerPeak} + inimigo {enemyPeak})", DebugCategory.Pool);
            return preload;
        }
    }
}
