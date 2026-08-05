using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Configuração de uma fase da Torre: composição inimiga.
    /// Origem de spawn é fixa globalmente em TowerRunConfig.
    /// </summary>
    [System.Serializable]
    public class TowerPhase
    {
        public string displayName = "Fase";
        public UnitSpawn[] enemies;
    }
}
