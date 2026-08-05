using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Como ordenar/escolher entre as unidades candidatas de uma <see cref="TargetQuery"/> ou do
    /// <see cref="FocusModule"/>. <c>All</c> não ordena (usado para área/multi-alvo).
    /// </summary>
    public enum TargetCriterion
    {
        All,
        Closest,
        Farthest,
        LowestCurrentHealth,
        HighestCurrentHealth,
        LowestMaxHealth,
        HighestMaxHealth,
        Random
    }
}
