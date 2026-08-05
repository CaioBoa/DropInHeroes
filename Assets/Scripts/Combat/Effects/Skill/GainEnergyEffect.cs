using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Soma energia (recurso) aos alvos resolvidos. Reutilizável — ex.: artefato BaixoPaul (on-hit,
    /// +1 ao próprio) e EnergyShield (início de combate, +N a todos os aliados). Alvo sem energia
    /// (ex.: sem capacidade de supremo) é no-op (o recurso clampa em 0).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Gain Energy")]
    public class GainEnergyEffect : SkillEffect
    {
        [Tooltip("Energia somada a cada alvo.")]
        [SerializeField] private float amount = 1f;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                StatsModule st = targets[i].Stats;
                if (st == null || st.IsDead) continue;
                st.GetResourceObject(ResourceType.Energy)?.Add(amount);
            }
        }
    }
}
