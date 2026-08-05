using System;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Capacidades de combate de uma unidade (dado). Permite invocações com funções reduzidas
    /// sem código novo: muitas não atacam nem usam supremo; algumas sim. Heróis usam
    /// <see cref="All"/> por padrão.
    /// </summary>
    [Flags]
    public enum UnitCapability
    {
        None = 0,
        /// <summary>Move-se pelo NavMesh em direção ao alvo.</summary>
        Move = 1 << 0,
        /// <summary>Executa a skill básica / ataca quando em alcance.</summary>
        Attack = 1 << 1,
        /// <summary>Pode usar o supremo — e, por consequência, regenera/acumula energia em combate.</summary>
        UseSupreme = 1 << 2,

        All = Move | Attack | UseSupreme,
    }
}
