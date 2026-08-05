using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico do Guliver — "Corte Heróico": dano FÍSICO no alvo atual, escalando com Ataque e
    /// Vida Máxima. Definido em código; o SO só expõe os multiplicadores (1.0 = 100%).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Guliver/Attack")]
    public class GuliverAttack : ModularBaseAttack
    {
        [Tooltip("Multiplicador do Ataque (1.0 = 100%).")]
        [SerializeField] private float attackScaling = 1.0f;
        [Tooltip("Multiplicador da Vida Máxima (0.05 = 5%).")]
        [SerializeField] private float healthScaling = 0.05f;

        private StatScaling[] scalings;

        protected override void OnStrike(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[]
            {
                new StatScaling(StatType.Attack, attackScaling),
                new StatScaling(StatType.MaxHealth, healthScaling)
            };
            SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.Physical, ref state);
        }
    }
}
