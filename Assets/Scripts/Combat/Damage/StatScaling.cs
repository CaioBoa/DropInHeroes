using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    [System.Serializable]
    public struct StatScaling
    {
        public StatType stat;
        public float multiplier;

        public StatScaling(StatType stat, float multiplier)
        {
            this.stat = stat;
            this.multiplier = multiplier;
        }

        /// <summary>
        /// Cria um escalonamento a partir de uma porcentagem do stat (10 = 10% → multiplicador 0.10).
        /// É a forma usada pelas skills, que expõem o valor em % no Inspector.
        /// </summary>
        public static StatScaling FromPercent(StatType stat, float percent) => new StatScaling(stat, percent * 0.01f);
    }
}
