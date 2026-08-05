using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Redireciona ao DONO uma fração do dano recebido pelo aliado de MENOR Vida Máxima (excluindo o
    /// próprio dono) — via <see cref="StatsModule.AddDamageShare"/>. Ex.: artefato Divine (25%), rodado
    /// como efeito de início de combate. Resolve uma vez (a Vida Máx é lida no momento). Alvos = os
    /// aliados (use TargetQuery {Allies}); a escolha do frágil e a exclusão do dono são feitas aqui.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Redirect Ally Damage")]
    public class RedirectAllyDamageEffect : SkillEffect
    {
        [Tooltip("Fração do dano do aliado redirecionada ao dono (0.25 = 25%).")]
        [Range(0f, 1f)] [SerializeField] private float fraction = 0.25f;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            StatsModule protector = context.ownerStats;
            if (protector == null || context.owner == null) return;

            // Id único por portador — dois portadores de Divine não colidem no mesmo aliado.
            string shareId = "redirect_" + context.owner.GetInstanceID();

            // Limpa o redirecionamento de TODOS os aliados antes de reatribuir: reaplicar em outra rodada
            // pode ter outro frágil, e o ex-frágil não deve manter o share. Torna o efeito idempotente.
            UnitController frailest = null;
            float minMaxHp = float.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                UnitController u = targets[i];
                if (u == null) continue;
                u.Stats?.RemoveDamageShare(shareId);
                if (u == context.owner) continue; // exclui o portador
                StatsModule st = u.Stats;
                if (st == null || st.IsDead) continue;
                if (st.MaxHealth < minMaxHp) { minMaxHp = st.MaxHealth; frailest = u; }
            }

            if (frailest == null) return;
            frailest.Stats?.AddDamageShare(shareId, protector, fraction);
        }
    }
}
