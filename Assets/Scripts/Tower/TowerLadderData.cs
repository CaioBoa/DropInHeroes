using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Sequência de fases que compõe uma run de Torre.
    /// Editar no Inspector para definir composições inimigas por fase.
    /// </summary>
    [CreateAssetMenu(fileName = "TowerLadder", menuName = "Game/Tower/Tower Ladder")]
    public class TowerLadderData : ScriptableObject
    {
        [SerializeField] private TowerPhase[] phases = new TowerPhase[5];

        public int PhaseCount => phases != null ? phases.Length : 0;

        public TowerPhase GetPhase(int index)
        {
            if (phases == null || index < 0 || index >= phases.Length) return null;
            return phases[index];
        }
    }
}
