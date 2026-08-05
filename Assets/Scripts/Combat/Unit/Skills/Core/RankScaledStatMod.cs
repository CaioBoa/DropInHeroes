using UnityEngine;
using DropInHeroes.Data;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Modificador de stat cujo valor escala por RANK (ex.: Ataque +20/35/50%). Usado pelas condições-bônus
    /// de forma da <see cref="VelaPassive"/> — reutilizável por qualquer buff que escale por rank.
    /// </summary>
    [System.Serializable]
    public struct RankScaledStatMod
    {
        public StatType stat;
        [Tooltip("true → fração multiplicativa (0.30 = +30%); false → valor bruto na convenção do stat (base 100 p/ lifesteal/crit…).")]
        public bool asPercent;
        [Tooltip("Valor por rank (índice 0 = rank 1). Ex.: {0.30,0.40,0.50} ou {15,20,25}.")]
        public float[] valuesByRank;
    }
}
