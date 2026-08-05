using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo da Vela na forma Jaguareté — "Jaguareté Su": na conjuração aumenta a própria Precisão e salta
    /// sobre o inimigo mais ferido, FIXANDO esse alvo no salto; no impacto crava as presas no MESMO alvo com
    /// dano físico altíssimo e VFX de bote. Definido em código; o SO só expõe os números e o clipe.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Vela/Bite Supreme")]
    public class VelaBiteSupreme : ModularSupreme
    {
        [Header("Precisão do bote")]
        [Tooltip("Precisão concedida antes do salto (base 100; 100 = +100%).")]
        [SerializeField] private float accuracyBonus = 100f;
        [SerializeField] private float accuracyDuration = 3f;

        [Header("Salto")]
        [SerializeField] private float leapDuration = 0.3f;
        [SerializeField] private float landingGap = 1f;

        [Header("Bote")]
        [Tooltip("Multiplicador do Ataque no dano (14.0 = 1400%).")]
        [SerializeField] private float damageScaling = 14.0f;
        [SerializeField] private AnimationClip biteVfxClip;

        private StatModification[] accuracyMods;
        private StatScaling[] scalings;
        // Alvo do bote fixado no salto (o inimigo mais ferido na conjuração); o impacto reusa o MESMO.
        private readonly List<UnitController> biteTarget = new List<UnitController>(1);

        protected override void OnCast(SkillContext context)
        {
            accuracyMods ??= new[] { StatModification.Flat(StatType.Accuracy, accuracyBonus) };

            var state = new EffectRunState();
            SkillModules.Status(context, Targets.Self(context), "accuracy_up", accuracyDuration, accuracyMods, ref state);

            biteTarget.Clear();
            List<UnitController> lowest = Targets.LowestHealth(context, 1, enemies: true);
            if (lowest.Count == 0) return;
            biteTarget.Add(lowest[0]);
            SkillModules.LeapToTarget(context, lowest[0], leapDuration, landingGap);
        }

        protected override void OnImpact(SkillContext context, ref EffectRunState state)
        {
            if (biteTarget.Count == 0) return;
            scalings ??= new[] { new StatScaling(StatType.Attack, damageScaling) };

            SkillModules.Vfx(context, biteTarget, biteVfxClip, ref state,
                scale: 1.2f, sortingOffset: 2, followTarget: true, duration: 0.8f, fadeOut: 0.2f);
            SkillModules.Damage(context, biteTarget, scalings, DamageType.Physical, ref state);
        }
    }
}
