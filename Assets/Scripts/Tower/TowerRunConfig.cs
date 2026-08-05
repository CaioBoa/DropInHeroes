using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Tunables da run de Torre. Tudo que for parametrizável (timers, HP, ranks)
    /// vive aqui para ajuste rápido pelo Inspector.
    /// maxUnitsOnField fica em PreparationConfig — não duplicar aqui.
    /// </summary>
    [CreateAssetMenu(fileName = "TowerRunConfig", menuName = "Game/Tower/Run Config")]
    public class TowerRunConfig : ScriptableObject
    {
        [Header("Ladder")]
        public TowerLadderData ladder;

        [Header("Enemy Spawn")]
        [Tooltip("Origem fixa para spawn de inimigos. spawnPosition de cada UnitSpawn é relativa a essa origem.")]
        public Vector2 enemySpawnOrigin = Vector2.zero;

        [Header("Timers (segundos)")]
        public float prepTimeSeconds = 30f;
        public float battleTimeSeconds = 45f;

        [Header("HP")]
        public int playerStartingHP = 3;
        public int towerStartingHP = 3;

        [Header("Shop")]
        public int shopOptionsCount = 3;

        [Header("Ranks")]
        [Tooltip("Rank máximo da bench (upgrades na loja). Rank agora escala apenas a passiva da unidade.")]
        public int maxRank = 3;
    }
}
