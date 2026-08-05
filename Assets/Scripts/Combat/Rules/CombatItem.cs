using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Base de item/relíquia (dado): produz <see cref="CombatRule"/>s que são registradas no
    /// <see cref="CombatRulesRegistry"/> no início do combate. O fluxo de "quem tem qual item"
    /// (inventário/equip) é responsabilidade de um sistema futuro — ver CombatController.RegisterItem.
    /// </summary>
    public abstract class CombatItem : ScriptableObject
    {
        [Tooltip("Nome de exibição do item.")]
        public string displayName;
        [TextArea(1, 3)] public string description;

        public abstract IEnumerable<CombatRule> CreateRules();
    }

    /// <summary>
    /// Item que dá um modificador de stat a uma categoria de unidade.
    /// Ex.: invocações causam crítico → affectedTags=Summon, stat=CritRate, flat=0.5.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Items/Tag Stat Item")]
    public class TagStatItem : CombatItem
    {
        public UnitTag affectedTags = UnitTag.Summon;
        public StatType stat = StatType.CritRate;
        public float flat = 0f;
        [Tooltip("Percentual: 0.30 = +30%.")]
        public float percent = 0f;

        public override IEnumerable<CombatRule> CreateRules()
        {
            yield return new StatModifierRule(affectedTags, stat, flat, percent);
        }
    }

    /// <summary>
    /// Item que dá um multiplicador de dano condicional a uma categoria de unidade.
    /// Ex.: heróis sofrem menos dano de invocações → affectedTags=Hero, direction=Incoming,
    /// conditionTags=Summon, multiplier=0.7.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Items/Tag Damage Item")]
    public class TagDamageItem : CombatItem
    {
        public UnitTag affectedTags = UnitTag.Hero;
        public DamageModifierRule.Direction direction = DamageModifierRule.Direction.Incoming;
        [Tooltip("Tags da outra ponta para o multiplicador valer (None = sempre).")]
        public UnitTag conditionTags = UnitTag.Summon;
        [Tooltip("Multiplicador: 0.7 = -30% de dano; 1.5 = +50%.")]
        public float multiplier = 1f;

        public override IEnumerable<CombatRule> CreateRules()
        {
            yield return new DamageModifierRule(affectedTags, direction, conditionTags, multiplier);
        }
    }
}
