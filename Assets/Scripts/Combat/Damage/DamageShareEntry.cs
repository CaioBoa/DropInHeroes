using System;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Entrada de damage share: redireciona uma fração do dano sofrido para um protetor.
    /// O protetor é resolvido no momento do dano (permite alvos dinâmicos, ex.: "a invocação
    /// aliada com mais vida").
    /// </summary>
    public struct DamageShareEntry
    {
        public string id;
        public Func<StatsModule> protectorResolver;
        public float value;
    }
}
