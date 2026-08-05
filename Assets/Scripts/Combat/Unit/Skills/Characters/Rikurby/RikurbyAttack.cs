using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico do Rikurby — dano FÍSICO no foco (escala com Ataque). Definido em código; o SO só
    /// expõe o multiplicador.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Rikurby/Attack")]
    public class RikurbyAttack : ModularBaseAttack
    {
        [Tooltip("Multiplicador do Ataque (1.5 = 150%).")]
        [SerializeField] private float attackScaling = 1.5f;

        private StatScaling[] scalings;

        protected override void OnStrike(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[] { new StatScaling(StatType.Attack, attackScaling) };
            SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.Physical, ref state);
        }
    }
}
