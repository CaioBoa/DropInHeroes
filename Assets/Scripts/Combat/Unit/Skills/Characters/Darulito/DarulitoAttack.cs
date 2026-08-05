using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico do Darulito — "Mordida do Bronquiossauro": dano FÍSICO no foco, escalando com Ataque,
    /// Defesa e Defesa Mágica (tank que bate com a própria resistência). Definido em código; SO só números.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Darulito/Attack")]
    public class DarulitoAttack : ModularBaseAttack
    {
        [Tooltip("Multiplicador do Ataque (1.0 = 100%).")]
        [SerializeField] private float attackScaling = 1.0f;
        [Tooltip("Multiplicador da Defesa (0.25 = 25%).")]
        [SerializeField] private float defenseScaling = 0.25f;
        [Tooltip("Multiplicador da Defesa Mágica (0.25 = 25%).")]
        [SerializeField] private float magicDefenseScaling = 0.25f;

        private StatScaling[] scalings;

        protected override void OnStrike(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[]
            {
                new StatScaling(StatType.Attack, attackScaling),
                new StatScaling(StatType.Defense, defenseScaling),
                new StatScaling(StatType.MagicalDefense, magicDefenseScaling)
            };
            SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.Physical, ref state);
        }
    }
}
