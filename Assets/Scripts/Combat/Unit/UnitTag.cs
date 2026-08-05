using System;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Categorias de unidade (dado). Usadas por regras de combate, itens e modificadores
    /// condicionais (ex.: "dano de invocação", "reduz dano DE invocações"). É um flags enum —
    /// uma unidade pode ter mais de uma categoria.
    /// </summary>
    [Flags]
    public enum UnitTag
    {
        None = 0,
        Hero = 1 << 0,
        Summon = 1 << 1,
        // bit 2 livre: a antiga Totem foi removida — Summon já cobre invocações/totens.
        Boss = 1 << 3,
    }
}
