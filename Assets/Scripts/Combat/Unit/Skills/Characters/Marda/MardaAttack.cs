using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico da Marda — "Sinfonia Horrenda": projétil que causa dano MÁGICO no foco (escala com
    /// Poder Mágico) e, no golpe, tem chance de aplicar Cura Reduzida no alvo. Definido em código; o SO
    /// só expõe os números e o projétil (clipe + velocidade).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Marda/Attack")]
    public class MardaAttack : ModularProjectileAttack
    {
        [Tooltip("Multiplicador do Poder Mágico (1.0 = 100%).")]
        [SerializeField] private float magicScaling = 1.0f;
        [Tooltip("Chance base de aplicar Cura Reduzida (0..1).")]
        [SerializeField] private float healReductionChance = 0.15f;
        [Tooltip("Ponto flat de Cura Recebida aplicado (base 100; -25 = -25%).")]
        [SerializeField] private float healReceivedReduction = -25f;
        [SerializeField] private float healReductionDuration = 5f;

        private StatScaling[] scalings;
        private StatModification[] healRedMods;

        protected override void OnStrike(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[] { new StatScaling(StatType.MagicalPower, magicScaling) };
            healRedMods ??= new[] { StatModification.Flat(StatType.HealReceived, healReceivedReduction) };

            SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.Magical, ref state);
            SkillModules.Status(context, Targets.Focus(context), "marda_heal_reduction", healReductionDuration, healRedMods, ref state, chance: healReductionChance);
        }
    }
}
