using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public static class TypeAdvantage
    {
        private static readonly HashSet<(CharacterType, CharacterType)> advantages = new()
        {
            (CharacterType.Favela, CharacterType.Classic),
            (CharacterType.Favela, CharacterType.Fairy),
            (CharacterType.Aura, CharacterType.Fairy),
            (CharacterType.Aura, CharacterType.Hero),
            (CharacterType.Revolution, CharacterType.Favela),
            (CharacterType.Revolution, CharacterType.Order),
            (CharacterType.Revolution, CharacterType.Art),
            (CharacterType.Art, CharacterType.Order),
            (CharacterType.Order, CharacterType.Dark),
            (CharacterType.Order, CharacterType.Favela),
            (CharacterType.Fairy, CharacterType.Revolution),
            (CharacterType.Fairy, CharacterType.Dark),
            (CharacterType.Chaos, CharacterType.Revolution),
            (CharacterType.Chaos, CharacterType.Aura),
            (CharacterType.Chaos, CharacterType.Chill),
            (CharacterType.Chill, CharacterType.Dark),
            (CharacterType.Dark, CharacterType.Hero),
            (CharacterType.Dark, CharacterType.Chaos),
            (CharacterType.Dark, CharacterType.Aura),
            (CharacterType.Hero, CharacterType.Revolution),
            (CharacterType.Hero, CharacterType.Chaos),
            (CharacterType.Classic, CharacterType.Chaos),
        };

        public static bool HasAdvantage(CharacterType attacker, CharacterType defender)
        {
            return advantages.Contains((attacker, defender));
        }

        public static int CountAdvantages(CharacterType[] attackerTypes, CharacterType[] defenderTypes)
        {
            int count = 0;
            foreach (var atk in attackerTypes)
                foreach (var def in defenderTypes)
                    if (HasAdvantage(atk, def)) count++;
            return count;
        }

        public static float GetMultiplier(CharacterType[] attackerTypes, CharacterType[] defenderTypes)
        {
            int count = CountAdvantages(attackerTypes, defenderTypes);
            return 1f + (count * 0.05f);
        }
    }
}
