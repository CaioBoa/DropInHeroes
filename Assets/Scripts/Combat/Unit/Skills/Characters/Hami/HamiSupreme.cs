using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo do Hami — "Onda Infernal": no impacto, o dono ganha Penetração Mágica temporária (ignora
    /// parte da Defesa Mágica) e causa dano MÁGICO em linha (onda) nos inimigos, com VFX de onda.
    /// Definido em código; o SO só expõe os números e o clipe.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Hami/Supreme")]
    public class HamiSupreme : ModularSupreme
    {
        [Tooltip("Penetração Mágica concedida durante a habilidade (base 100; 50 = ignora 50% da Def. Mágica).")]
        [SerializeField] private float penetrationBonus = 50f;
        [Tooltip("Multiplicador do Ataque no dano (5.0 = 500%).")]
        [SerializeField] private float damageScaling = 5.0f;
        [SerializeField] private float lineWidth = 2f;
        [SerializeField] private float lineLength = 7f;
        [SerializeField] private AnimationClip waveVfxClip;

        private StatScaling[] scalings;

        protected override void OnImpact(SkillContext context, ref EffectRunState state)
        {
            scalings ??= new[] { new StatScaling(StatType.Attack, damageScaling) };

            SkillStat(context, StatType.MagicalPenetration, penetrationBonus); // temporário — o dano lê já modificado
            SkillModules.Vfx(context, Targets.Line(context, lineWidth, lineLength, enemies: true), waveVfxClip, ref state,
                scale: 0.7f, sortingOffset: 1, followTarget: true, duration: 1.2f, fadeOut: 0.3f);
            SkillModules.Damage(context, Targets.Line(context, lineWidth, lineLength, enemies: true), scalings, DamageType.Magical, ref state);
        }
    }
}
