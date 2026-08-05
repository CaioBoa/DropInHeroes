using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public enum Team
    {
        Player = 1,
        Enemy = 2
    }

    /// <summary>
    /// Configuração para inicialização de unidades
    /// </summary>
    public struct UnitConfig
    {
        public Team team;
        public bool enableDrag;
        public bool enableFootprint;
        public bool isSummon;

        public static UnitConfig Player => new UnitConfig
        {
            team = Team.Player,
            enableDrag = true,
            enableFootprint = true,
            isSummon = false
        };

        public static UnitConfig Enemy => new UnitConfig
        {
            team = Team.Enemy,
            enableDrag = false,
            enableFootprint = false,
            isSummon = false
        };

        /// <summary>
        /// Config para invocações. O que a invocação pode fazer (mover/atacar/energia/supremo) vem
        /// das <see cref="UnitCapability"/> da CharacterData dela — não do spawn.
        /// </summary>
        public static UnitConfig Summon(Team team) => new UnitConfig
        {
            team = team,
            enableDrag = false,
            enableFootprint = false,
            isSummon = true
        };
    }
}
