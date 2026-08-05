using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva do Pedro — "Caralio": ao início do combate (e a cada revive), concede Vigor e um escudo a
    /// todos os aliados. Definida em código; o SO só expõe os números. Efeitos one-shot (o Vigor expira
    /// sozinho, o escudo é consumido) — sem reversão pela passiva.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Pedro/Passive")]
    public class PedroPassive : ModularPassive
    {
        [Header("Vigor (buff nos aliados)")]
        [SerializeField] private float vigorAttack = 0.3f;
        [SerializeField] private float vigorSpeed = 0.3f;
        [Tooltip("Duração do Vigor por rank (rank 1 = índice 0). Ex.: 6 / 9 / 14.")]
        [SerializeField] private float[] vigorDurationByRank = { 6f, 9f, 14f };

        [Header("Escudo (multiplicadores nos stats do dono)")]
        [SerializeField] private float shieldMagicScaling = 5.0f;  // 500% Poder Mágico
        [SerializeField] private float shieldAttackScaling = 2.0f;  // 200% Ataque

        private StatModification[] vigorMods;
        private StatScaling[] shieldScalings;

        protected override void Compose(SkillContext context)
        {
            vigorMods ??= new[] { StatModification.Percent(StatType.Attack, vigorAttack), StatModification.Percent(StatType.Speed, vigorSpeed) };
            shieldScalings ??= new[] { new StatScaling(StatType.MagicalPower, shieldMagicScaling), new StatScaling(StatType.Attack, shieldAttackScaling) };

            var allies = Targets.Allies(context, includeSelf: true);
            var state = new EffectRunState(); // aliados não esquivam — FilterDodged é no-op

            SkillModules.Status(context, allies, "vigor", 0f, vigorMods, ref state, durationByRank: vigorDurationByRank);
            SkillModules.Shield(context, allies, "pedro_caralio", shieldScalings, ref state);
        }
    }
}
