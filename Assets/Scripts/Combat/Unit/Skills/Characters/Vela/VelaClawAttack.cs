using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico da Vela na forma Jaguareté — "Garra da Onça": dano FÍSICO no foco (escala com Ataque).
    /// Definido em código; o SO só expõe o multiplicador.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Vela/Claw Attack")]
    public class VelaClawAttack : ModularBaseAttack
    {
        [Tooltip("Multiplicador do Ataque (2.0 = 200%).")]
        [SerializeField] private float attackScaling = 2.0f;

        private StatScaling[] scalings;

        protected override void OnStrike(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[] { new StatScaling(StatType.Attack, attackScaling) };
            SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.Physical, ref state);
        }
    }
}
