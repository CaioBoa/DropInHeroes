using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico da Vela na forma Aba — "Lança Tupi": dano FÍSICO no foco (escala com Ataque) com chance,
    /// no golpe, de romper a armadura do alvo (reduz a Defesa). Definido em código; o SO só expõe os números.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Vela/Spear Attack")]
    public class VelaSpearAttack : ModularBaseAttack
    {
        [Tooltip("Multiplicador do Ataque (1.0 = 100%).")]
        [SerializeField] private float attackScaling = 1.0f;

        [Header("Romper armadura (chance no golpe)")]
        [Tooltip("Chance base de reduzir a Defesa do alvo (0..1).")]
        [SerializeField] private float armorBreakChance = 0.15f;
        [Tooltip("Redução percentual da Defesa (-0.30 = -30%).")]
        [SerializeField] private float defenseReductionPercent = -0.3f;
        [SerializeField] private float armorBreakDuration = 4f;

        private StatScaling[] scalings;
        private StatModification[] armorBreakMods;

        protected override void OnStrike(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[] { new StatScaling(StatType.Attack, attackScaling) };
            armorBreakMods ??= new[] { StatModification.Percent(StatType.Defense, defenseReductionPercent) };

            SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.Physical, ref state);
            SkillModules.Status(context, Targets.Focus(context), "armor_reduction", armorBreakDuration, armorBreakMods, ref state, chance: armorBreakChance);
        }
    }
}
