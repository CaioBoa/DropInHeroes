using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// De onde uma <see cref="TargetQuery"/> parte para selecionar unidades. A perspectiva é sempre
    /// a unidade que executa a query (o <c>owner</c> do <see cref="TargetContext"/>).
    /// </summary>
    public enum TargetSide
    {
        CurrentTarget, // o alvo atual da unidade
        Self,          // a própria unidade que executa a query
        Allies,        // a equipe da unidade (inclui ela mesma)
        Enemies,       // a equipe oposta
        AllUnits,      // ambas as equipes
        Applier        // quem aplicou o status que dispara a query (taunt)
    }
}
