using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo da Vela na forma Aba — "Ñandejára tapenderovasa": no impacto da animação, cura todos os
    /// aliados (escala com Poder Mágico), purifica seus debuffs e toca a VFX da bênção sobre si. Definido em
    /// código; o SO só expõe os números e o clipe. Requer evento de impacto na animação (efeitos no gesto).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Vela/Blessing Supreme")]
    public class VelaBlessingSupreme : ModularSupreme
    {
        [Tooltip("Multiplicador do Poder Mágico na cura (4.0 = 400%).")]
        [SerializeField] private float healScaling = 4.0f;
        [Tooltip("Máximo de debuffs removidos por aliado.")]
        [SerializeField] private int cleanseCount = 99;
        [SerializeField] private AnimationClip blessingVfxClip;

        private StatScaling[] healScalings;

        protected override void OnImpact(SkillContext context, ref EffectRunState state)
        {
            healScalings ??= new[] { new StatScaling(StatType.MagicalPower, healScaling) };

            List<UnitController> allies = Targets.Allies(context, includeSelf: true);
            SkillModules.Heal(context, allies, healScalings, ref state);
            SkillModules.Cleanse(context, allies, cleanseCount);
            SkillModules.Vfx(context, Targets.Self(context), blessingVfxClip, ref state,
                scale: 2f, sortingOffset: -2, followTarget: true, duration: 1.5f, fadeOut: 0.3f);
        }
    }
}
