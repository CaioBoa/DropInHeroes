using System.Collections.Generic;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Critério de desempate quando uma batalha estoura o tempo: vence quem tem mais unidades vivas;
    /// empate no número, vence a maior soma de HP; persistindo o empate, o jogador.
    /// </summary>
    public static class TimeoutResolver
    {
        public static Team DecideWinner(IList<UnitController> playerUnits, IList<UnitController> enemyUnits)
        {
            int playerAlive = CountAlive(playerUnits);
            int enemyAlive = CountAlive(enemyUnits);
            if (playerAlive != enemyAlive)
                return playerAlive > enemyAlive ? Team.Player : Team.Enemy;

            return SumHP(playerUnits) >= SumHP(enemyUnits) ? Team.Player : Team.Enemy;
        }

        private static int CountAlive(IList<UnitController> units)
        {
            int count = 0;
            for (int i = 0; i < units.Count; i++)
            {
                var stats = units[i] != null ? units[i].GetModule<StatsModule>() : null;
                if (stats != null && !stats.IsDead) count++;
            }
            return count;
        }

        private static float SumHP(IList<UnitController> units)
        {
            float sum = 0f;
            for (int i = 0; i < units.Count; i++)
            {
                var stats = units[i] != null ? units[i].GetModule<StatsModule>() : null;
                if (stats != null && !stats.IsDead) sum += stats.CurrentHealth;
            }
            return sum;
        }
    }
}
