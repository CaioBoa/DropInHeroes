using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Salto INTELIGENTE (onCast): em vez de pular para o alvo atual, salta para a posição que maximiza
    /// o número de unidades do lado escolhido dentro do raio da AoE (ver <see cref="AoeClusterTargeting"/>).
    /// Pense no dano em área que vem depois como <see cref="RadiusMode.AroundSelf"/>: a unidade pousa no
    /// melhor centro e o dano naturalmente pega o máximo de alvos. Usado por supremos de salto em área
    /// (Rikurby, Guliver). <see cref="aoeRadius"/> deve casar com o raio do dano em área subsequente.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Leap To Best AoE Position")]
    public class LeapToBestAoePositionEffect : SkillEffect
    {
        [Tooltip("Duração do salto, em segundos.")]
        [SerializeField] private float leapDuration = 0.3f;
        [Tooltip("Raio da AoE a otimizar. DEVE casar com o raio do dano em área (AroundSelf) que roda depois.")]
        [SerializeField] private float aoeRadius = 3f;
        [Tooltip("Lado agrupado (Enemies = pousar no meio dos inimigos; Allies = no meio dos aliados).")]
        [SerializeField] private TargetSide clusterSide = TargetSide.Enemies;

        // Buffer transitório reutilizado — o efeito é sub-asset compartilhado, mas Apply é síncrono e
        // não-reentrante (Unity single-thread), e nada é retido entre chamadas.
        private static readonly List<Vector2> points = new List<Vector2>();

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            if (context.owner == null) return;
            var combat = context.owner.GetModule<CombatModule>();
            if (combat == null) return;

            IReadOnlyList<UnitController> pool = clusterSide == TargetSide.Allies ? context.allies : context.enemies;

            points.Clear();
            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                {
                    UnitController u = pool[i];
                    if (u == null || !u.IsTargetable) continue;
                    StatsModule st = u.Stats;
                    if (st == null || st.IsDead) continue;
                    points.Add(u.transform.position);
                }

            Vector3 ownerPos = context.owner.transform.position;

            if (points.Count == 0)
            {
                // Sem alvos válidos: cai no comportamento do salto simples (vai até o alvo atual, se houver).
                if (context.target != null)
                    combat.LeapTo(context.target.transform.position, leapDuration);
                return;
            }

            Vector2 best = AoeClusterTargeting.BestCoveragePoint(points, aoeRadius, ownerPos);
            combat.LeapTo(new Vector3(best.x, best.y, ownerPos.z), leapDuration);
        }
    }
}
