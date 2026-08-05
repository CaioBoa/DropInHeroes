using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public struct DamageResult
    {
        public float rawDamage;
        public float finalDamage;
        public bool isCritical;
        public bool isExtinction;
        public bool ignoreDamageShare;
    }
}
