using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva da Marda — "Sinfonia Debilitante": enquanto vive, cada debuff ativo num inimigo reduz o
    /// dano que ELE causa (stat DamageBonus, dinâmico — reavaliado a cada golpe conforme os debuffs dele).
    /// Definida em código; o SO só expõe a intensidade por debuff, por rank.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Marda/Passive")]
    public class MardaPassive : ModularPassive
    {
        [Tooltip("Redução do dano CAUSADO pelo inimigo por debuff ativo nele (fração), por rank. Ex.: 0.025 / 0.05 / 0.075.")]
        [SerializeField] private float[] damageReductionPerDebuffByRank = { 0.025f, 0.05f, 0.075f };

        protected override void Compose(SkillContext context)
        {
            // DamageBonus base 100 (valor negativo = menos dano causado): a fração por debuff vira pontos.
            float perDebuff = RankTiers.ValueFor(damageReductionPerDebuffByRank, context.owner.Rank) * 100f;
            var enemies = Targets.Enemies(context);

            SkillModules.StatModifierPerCount(Persistent, enemies, StatType.DamageBonus, -perDebuff,
                enemy => enemy.GetModule<StatusModule>()?.DebuffCount ?? 0);
        }
    }
}
