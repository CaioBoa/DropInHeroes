using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico do Hami — "Sopro Arcano": projétil que causa dano MÁGICO no foco (escala com Poder
    /// Mágico) e, no golpe, tem chance de aplicar Incendiar (DoT). Definido em código; o SO só expõe os
    /// números e o projétil.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Hami/Attack")]
    public class HamiAttack : ModularProjectileAttack
    {
        [Tooltip("Multiplicador do Poder Mágico (1.0 = 100%).")]
        [SerializeField] private float magicScaling = 1.0f;
        [Tooltip("Chance base de aplicar Incendiar (0..1).")]
        [SerializeField] private float incendiarChance = 0.15f;
        [Tooltip("Dano verdadeiro por segundo do Incendiar.")]
        [SerializeField] private float incendiarDps = 15f;
        [SerializeField] private float incendiarDuration = 5f;

        private StatScaling[] scalings;

        protected override void OnStrike(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[] { new StatScaling(StatType.MagicalPower, magicScaling) };
            SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.Magical, ref state);
            SkillModules.Status(context, Targets.Focus(context), "incendiar", incendiarDuration, null, ref state, dps: incendiarDps, chance: incendiarChance);
        }
    }
}
