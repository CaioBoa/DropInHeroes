using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Aplica um status (tipo na tabela, por <see cref="statusId"/>) a cada alvo, com os VALORES
    /// parametrizados aqui (duração, magnitude, DoT, chance). Encaminha para o ponto único
    /// <see cref="StatusModule.ApplyStatus"/>, que rola a chance (Efetividade × Resistência), escala a
    /// duração de controle e empilha conforme o tipo. Self-describing via <c>description</c> (herdado),
    /// referenciando os campos abaixo.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Apply Status")]
    public class ApplyStatusEffect : SkillEffect
    {
        [Header("Tipo")]
        [Tooltip("Id do tipo de status na tabela (StatusCatalog). Fonte dos traços nativos do tipo.")]
        [SerializeField] private string statusId;

        [Header("Parâmetros da aplicação")]
        [Tooltip("Duração em segundos. <= 0 = permanente até o fim do combate.")]
        [SerializeField] private float duration = 5f;
        [Tooltip("Duração POR RANK (rank 1 = índice 0), via RankTiers — sobrescreve 'duration' quando preenchido. " +
                 "Vazio = usa 'duration'. Use em passivas cujo rank escala a duração do status (ex.: Vigor 6/9/14s).")]
        [SerializeField] private float[] durationByRank;
        [Tooltip("Modificadores desta aplicação (buff/debuff de stat). Vazio = sem modificadores (ex.: DoT/controle puro).")]
        [SerializeField] private StatModification[] modifications;
        [Tooltip("Dano verdadeiro por segundo (DoT) desta aplicação. <= 0 = sem DoT.")]
        [SerializeField] private float damagePerSecond = -1f;

        [Header("Aplicação")]
        [Tooltip("Chance base (0..1). A chance final considera a Efetividade do atacante e a Resistência do alvo. " +
                 "Ignorada em aplicação amiga (mesmo time nunca é resistido).")]
        [Range(0f, 1f)] [SerializeField] private float chance = 1f;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            if (targets.Count == 0 || string.IsNullOrEmpty(statusId)) return;

            // Duração base: por rank (RankTiers) quando durationByRank está preenchido; senão o campo fixo.
            float baseDuration = (durationByRank != null && durationByRank.Length > 0)
                ? RankTiers.ValueFor(durationByRank, context.owner != null ? context.owner.Rank : 1, duration)
                : duration;

            for (int i = 0; i < targets.Count; i++)
            {
                UnitController target = targets[i];
                var targetStatus = target.GetModule<StatusModule>();
                var targetStats = target.GetModule<StatsModule>();
                if (targetStatus == null || targetStats == null || targetStats.IsDead) continue;

                targetStatus.ApplyStatus(statusId, baseDuration, modifications, damagePerSecond, chance, context.owner);
            }
        }
    }
}
