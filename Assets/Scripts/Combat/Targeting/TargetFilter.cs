using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Filtro componível de candidatos. Unidades mortas são sempre descartadas (ver
    /// <see cref="TargetSelection.IsAlive"/>).
    /// </summary>
    [System.Serializable]
    public struct TargetFilter
    {
        [Tooltip("Se diferente de None, mantém só unidades que possuem ALGUMA destas tags.")]
        public UnitTag requireTags;
        [Tooltip("Descarta unidades que possuem QUALQUER uma destas tags.")]
        public UnitTag excludeTags;
        [Tooltip("Respeita furtividade: descarta unidades não-focáveis.")]
        public bool onlyTargetable;

        public bool Passes(UnitController unit)
        {
            if (!TargetSelection.IsAlive(unit)) return false;
            if (requireTags != UnitTag.None && (unit.Tags & requireTags) == 0) return false;
            if (excludeTags != UnitTag.None && (unit.Tags & excludeTags) != 0) return false;
            if (onlyTargetable && !unit.IsTargetable) return false;
            return true;
        }
    }
}
