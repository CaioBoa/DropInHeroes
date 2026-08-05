using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Uma contribuição de UM stat do DONO para um stat derivado da invocação. percent em base 100
    /// (60 = 60% do stat do dono). Somam-se quando há mais de uma (ex.: Ataque do LilRih = X% do
    /// Ataque + Y% do Poder Mágico do dono).
    /// </summary>
    [System.Serializable]
    public struct SummonStatContribution
    {
        public StatType from;
        [Tooltip("Fração do stat do dono, base 100 (60 = 60%).")]
        public float percent;
    }

    /// <summary>
    /// Regra de derivação de UM stat da invocação a partir do dono. O valor derivado é a soma das
    /// contribuições (stat do dono × %). <see cref="replaceOwnBase"/> decide como ele se combina com
    /// a base própria da invocação (a do CharacterData dela):
    /// - false (padrão): base final = base própria + derivado (ex.: Vida/Ataque somam ao piso próprio);
    /// - true: base final = derivado, ignorando a base própria (ex.: COPIAR crit do dono = [Crit×100%];
    ///   ZERAR poder mágico = sem contribuições → 0).
    /// </summary>
    [System.Serializable]
    public struct SummonStatRule
    {
        public StatType stat;
        [Tooltip("true = substitui a base própria pelo derivado (copiar/zerar); false = soma ao próprio.")]
        public bool replaceOwnBase;
        public SummonStatContribution[] fromOwner;
    }

    /// <summary>
    /// Perfil DATA-DRIVEN de derivação de stats de uma invocação a partir do dono, avaliado no momento
    /// da invocação (snapshot). Referenciado pela CharacterData da invocação; aplicado pelo
    /// <see cref="SummonModule"/> no spawn, ANTES das regras de item. A invocação NÃO herda o bloco de
    /// stats do dono: só o que este perfil declara é derivado; o resto vem da base própria dela.
    /// Ex. (Little Rick): Ataque = próprio + 60% Ataque + 40% Poder Mágico; Vida = próprio + 60% Vida +
    /// 40% Poder Mágico; CritRate/CritDamage = copiar do dono; Poder Mágico = 0.
    /// </summary>
    [CreateAssetMenu(fileName = "SummonStatProfile", menuName = "Game/Data/Summon Stat Profile")]
    public class SummonStatProfile : ScriptableObject
    {
        [Tooltip("Uma regra por stat derivado. Stats não listados ficam com a base própria da invocação.")]
        [SerializeField] private SummonStatRule[] rules;

        /// <summary>
        /// Deriva os stats da invocação a partir dos stats do dono (snapshot). Usa <see cref="Stat.BaseValue"/>
        /// como base própria — deve rodar depois de ApplyCharacterStats e antes de qualquer modificador de item.
        /// </summary>
        public void Apply(StatsModule summonStats, StatsModule ownerStats)
        {
            if (summonStats == null || ownerStats == null || rules == null) return;

            for (int i = 0; i < rules.Length; i++)
            {
                SummonStatRule rule = rules[i];
                Stat stat = summonStats.GetStatObject(rule.stat);
                if (stat == null) continue;

                float derived = 0f;
                if (rule.fromOwner != null)
                    for (int c = 0; c < rule.fromOwner.Length; c++)
                        derived += ownerStats.GetStat(rule.fromOwner[c].from) * (rule.fromOwner[c].percent * 0.01f);

                stat.SetBaseValue(rule.replaceOwnBase ? derived : stat.BaseValue + derived);
            }

            summonStats.GetResourceObject(ResourceType.Health)?.SetToMax();
        }
    }
}
