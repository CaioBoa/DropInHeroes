using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo do Totem da Libertação (invocação do Darulito) — "Totem Boliviano": causa dano MÁGICO aos
    /// inimigos ao redor do totem e purifica debuffs dos aliados. Os efeitos rodam no OnCast (imediato), pois
    /// o totem só tem idle (autocast sem hit-frame). Definido em código; o SO só expõe os números.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Darulito/Totem Boliviano")]
    public class TotemBolivianoSupreme : ModularSupreme
    {
        [Header("Dano mágico em área (ao redor do totem)")]
        [SerializeField] private float radius = 3f;
        [Tooltip("Multiplicador do Poder Mágico (4.0 = 400%).")]
        [SerializeField] private float magicScaling = 4.0f;

        [Header("Purificação (aliados)")]
        [Tooltip("Máximo de debuffs removidos por aliado.")]
        [SerializeField] private int cleanseCount = 2;

        private StatScaling[] scalings;

        // Sem hit-frame no totem: os efeitos disparam na conjuração (equivale ao onCast do formato antigo).
        protected override void OnCast(SkillContext context)
        {
            scalings ??= new[] { new StatScaling(StatType.MagicalPower, magicScaling) };
            var state = new EffectRunState(); // dodgeRolls null → acerto garantido (AoE do totem)
            SkillModules.Damage(context, Targets.AroundSelf(context, radius, enemies: true), scalings, DamageType.Magical, ref state);
            SkillModules.Cleanse(context, Targets.Allies(context, includeSelf: true), cleanseCount);
        }

        protected override void OnImpact(SkillContext context, ref EffectRunState state) { }
    }
}
