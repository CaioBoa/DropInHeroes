using UnityEngine;
using UnityEngine.Serialization;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Definição de uma unidade a ser spawnada, válida para qualquer time
    /// (jogador ou inimigo). Rank é universal — toda unidade tem rank.
    /// </summary>
    [System.Serializable]
    public class UnitSpawn
    {
        public string characterId;
        public Vector2 spawnPosition;
        [FormerlySerializedAs("level")] public int rank = 1;
    }
}
