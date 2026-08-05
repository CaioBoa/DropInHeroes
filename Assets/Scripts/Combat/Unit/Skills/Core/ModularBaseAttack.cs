using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Base de ataque básico escrito como COMPOSIÇÃO DE MÓDULOS. Esqueleto fixo: dispara a animação
    /// (cadência global = Speed / <see cref="AttackSpeedReference"/>), e no frame de impacto envolve o
    /// golpe com os SkillModifiers, chama <see cref="OnStrike"/> (a subclasse compõe dano/efeitos) e
    /// SEMPRE aplica os on-hit registrados na unidade (<see cref="OnHitModule"/>). Ataque SEM dano =
    /// um <see cref="OnStrike"/> que não chama <c>SkillModules.Damage</c>. Entrega melee vs projétil e
    /// acerto garantido são decisão de código da subclasse.
    /// </summary>
    public abstract class ModularBaseAttack : BaseAttackSkill
    {
        public const float AttackSpeedReference = 3f;
        private const float MinAttackInterval = 0.1f; // teto de ~10 ataques/s

        /// <summary>Acerto garantido (pula a esquiva) — decisão de código da subclasse.</summary>
        protected virtual bool AlwaysHits => false;

        // Cache de esquiva por golpe (reusado; a skill é clonada por unidade).
        private readonly Dictionary<UnitController, bool> dodgeRolls = new Dictionary<UnitController, bool>();

        public override float Execute(SkillContext context)
        {
            float interval = AttackInterval(context.ownerStats?.Speed ?? DefaultSpeed);
            return context.ownerVisual?.PlayAttackAnimationForDuration(interval) ?? interval;
        }

        /// <summary>Intervalo entre ataques (segundos) — função só da Velocidade (cadência global).</summary>
        public static float AttackInterval(float speed)
            => Mathf.Max(MinAttackInterval, AttackSpeedReference / Mathf.Max(0.01f, speed));

        public override void OnHit(SkillContext context) => DoStrike(context);

        /// <summary>
        /// O golpe em si (dano + efeitos próprios via <see cref="OnStrike"/> + riders on-hit). Chamado
        /// direto no melee (no hit-frame) e na CHEGADA do projétil no ranged (<see cref="ModularProjectileAttack"/>).
        /// </summary>
        protected void DoStrike(SkillContext context)
        {
            PushSkillModifiers(context);
            try
            {
                var state = new EffectRunState();
                if (!AlwaysHits) { dodgeRolls.Clear(); state.dodgeRolls = dodgeRolls; }
                context.allowCrit = true;   // ataque básico pode critar
                context.isOnHit = false;    // o golpe em si conta como "ataque recebido"

                OnStrike(context, ref state);

                // Mecânica ON-HIT: riders de todas as fontes (skill/passiva/artefatos) — base de todo golpe.
                context.owner?.GetModule<OnHitModule>()?.ApplyAll(context, ref state);
            }
            finally { PopSkillModifiers(context); }
        }

        /// <summary>Compõe o golpe (dano intrínseco + efeitos próprios). A subclasse chama SkillModules.*.</summary>
        protected abstract void OnStrike(SkillContext context, ref EffectRunState state);
    }
}
