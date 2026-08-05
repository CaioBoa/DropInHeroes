using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo da Marda: no impacto, tenta reduzir Ataque, Velocidade e Poder Mágico de todos os
    /// inimigos (chance ajustada por Efetividade×Resistência), com um VFX de debuff. Definido em código;
    /// o SO só expõe os números e o clipe de VFX.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Marda/Supreme")]
    public class MardaSupreme : ModularSupreme
    {
        [Tooltip("Intensidade de cada debuff (fração; -0.40 = -40%).")]
        [SerializeField] private float debuffPercent = -0.40f;
        [Tooltip("Chance base de cada debuff (0..1).")]
        [SerializeField] private float chance = 0.85f;
        [SerializeField] private float duration = 10f;
        [SerializeField] private AnimationClip debuffVfxClip;

        private StatModification[] atkMods;
        private StatModification[] spdMods;
        private StatModification[] matkMods;

        protected override void OnImpact(SkillContext context, ref EffectRunState state)
        {
            atkMods ??= new[] { StatModification.Percent(StatType.Attack, debuffPercent) };
            spdMods ??= new[] { StatModification.Percent(StatType.Speed, debuffPercent) };
            matkMods ??= new[] { StatModification.Percent(StatType.MagicalPower, debuffPercent) };

            SkillModules.Status(context, Targets.Enemies(context), "marda_atk_down", duration, atkMods, ref state, chance: chance);
            SkillModules.Status(context, Targets.Enemies(context), "marda_spd_down", duration, spdMods, ref state, chance: chance);
            SkillModules.Vfx(context, Targets.Enemies(context), debuffVfxClip, ref state, alpha: 0.4f, scale: 0.5f, followTarget: true, zOffset: 100f, duration: 2f, fadeOut: 0.5f);
            SkillModules.Status(context, Targets.Enemies(context), "marda_matk_down", duration, matkMods, ref state, chance: chance);
        }
    }
}
