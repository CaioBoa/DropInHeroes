using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva que roda uma lista de <see cref="SkillEffect"/> UMA VEZ no início do combate (ao nascer e
    /// ao reviver), cada efeito mirando pela própria <see cref="TargetQuery"/>. É a versão-passiva do
    /// padrão já usado pelos artefatos (<see cref="ArtifactData.onBattleStartEffects"/>, rodado pelo
    /// <see cref="LoadoutModule"/>): genérico e reutilizável — ex.: conceder Vigor + escudo a todos os
    /// aliados no início do combate. Sem lógica por-personagem. Self-describing: a descrição da passiva +
    /// a de cada efeito aparecem no tooltip.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Combat Start Passive")]
    public class CombatStartPassive : PassiveSkill, ISkillEffectSource
    {
        [Header("Efeitos rodados 1x no início do combate (cada um mira via sua TargetQuery)")]
        [SerializeField] private SkillEffect[] onBattleStartEffects;

        public IEnumerable<SkillEffect> DescribableEffects => onBattleStartEffects;

        // Buffer reutilizado — a passiva de topo é clonada por unidade pelo SkillsModule, então é seguro.
        private readonly List<UnitController> buffer = new List<UnitController>();

        protected override void OnApply(SkillContext context)
        {
            if (onBattleStartEffects == null || onBattleStartEffects.Length == 0) return;

            var state = new EffectRunState();
            for (int i = 0; i < onBattleStartEffects.Length; i++)
            {
                SkillEffect fx = onBattleStartEffects[i];
                if (fx == null) continue;
                buffer.Clear();
                fx.ResolveTargets(context, buffer);
                fx.Apply(context, buffer, ref state);
                state.previousTargetCount = buffer.Count;
            }
        }
    }
}
