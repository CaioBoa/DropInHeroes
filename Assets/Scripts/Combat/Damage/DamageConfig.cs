using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public struct DamageConfig
    {
        // Físico → Defesa; Mágico → Defesa Mágica; Verdadeiro → ignora defesas (redução ainda vale);
        // Puro → ignora toda mitigação do alvo. Ver DamageType.
        public DamageType damageType;
        public bool cannotCrit;
        public bool alwaysCrit;
        public bool extinction;
        public bool ignoreDamageShare;
        public bool ignoreTypeAdvantage;

        public static DamageConfig Default => new DamageConfig();
    }
}
