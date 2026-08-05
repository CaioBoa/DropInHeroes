using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Dados que uma <see cref="TargetQuery"/> precisa para resolver alvos. O <c>owner</c> é a
    /// perspectiva (de quem parte a seleção). Construído a partir de um <see cref="SkillContext"/>
    /// (skills) ou direto dos estados de combate (<see cref="FocusModule"/>).
    /// </summary>
    public struct TargetContext
    {
        public UnitController owner;
        public UnitController currentTarget;
        public UnitController applier;
        public IReadOnlyList<UnitController> allies;
        public IReadOnlyList<UnitController> enemies;

        public static TargetContext FromSkill(in SkillContext ctx) => new TargetContext
        {
            owner = ctx.owner,
            currentTarget = ctx.target,
            applier = null,
            allies = ctx.allies,
            enemies = ctx.enemies
        };

        public static TargetContext FromUnit(UnitController owner, IReadOnlyList<UnitController> enemies,
            IReadOnlyList<UnitController> allies, UnitController currentTarget, UnitController applier)
            => new TargetContext
            {
                owner = owner,
                currentTarget = currentTarget,
                applier = applier,
                allies = allies,
                enemies = enemies
            };
    }
}
