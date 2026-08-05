using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Salto (gap-closer) do dono até o alvo, parando a uma distância dele. Usado como efeito de
    /// "conjuração" (onCast) antes da animação do supremo. Centraliza o salto antes duplicado em
    /// Guliver e Rikurby.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Leap To Target")]
    public class LeapToTargetEffect : SkillEffect
    {
        [Tooltip("Duração do salto até o alvo, em segundos.")]
        [SerializeField] private float leapDuration = 0.3f;
        [Tooltip("Distância em que o dono para antes do alvo (para não sobrepor).")]
        [SerializeField] private float landingGap = 1f;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            if (context.owner == null) return;

            UnitController target = targets.Count > 0 ? targets[0] : context.target;
            if (target == null) return;

            var combat = context.owner.GetModule<CombatModule>();
            if (combat == null) return;

            Vector3 ownerPos = context.owner.transform.position;
            Vector3 targetPos = target.transform.position;
            Vector3 toTarget = targetPos - ownerPos;
            float dist = toTarget.magnitude;

            Vector3 landing = dist > landingGap
                ? targetPos - toTarget.normalized * landingGap
                : ownerPos;

            combat.LeapTo(landing, leapDuration);
        }
    }
}
