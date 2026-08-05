using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico do Little Rick (invocação do Rikurby) — dano FÍSICO no foco (escala com Ataque).
    /// Definido em código; o SO só expõe o multiplicador.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Rikurby/Little Rick Attack")]
    public class LittleRickAttack : ModularBaseAttack
    {
        [Tooltip("Multiplicador do Ataque (1.0 = 100%).")]
        [SerializeField] private float attackScaling = 1.0f;

        private StatScaling[] scalings;

        protected override void OnStrike(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[] { new StatScaling(StatType.Attack, attackScaling) };
            SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.Physical, ref state);
        }
    }
}
