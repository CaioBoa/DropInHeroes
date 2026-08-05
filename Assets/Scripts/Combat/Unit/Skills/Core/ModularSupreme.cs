using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Base de SUPREMO escrito como COMPOSIÇÃO DE MÓDULOS. Duas âncoras: <see cref="OnCast"/> na
    /// conjuração (antes da animação — ex.: salto) e <see cref="OnImpact"/> no frame de impacto da
    /// animação. A subclasse compõe módulos (SkillModules.*) sobre alvos (Targets.*). Energia vem de
    /// <see cref="SupremeSkill.energyCost"/>; os SkillModifiers valem durante a aplicação dos efeitos.
    /// </summary>
    public abstract class ModularSupreme : SupremeSkill
    {
        [Header("Animação")]
        [Tooltip("Referência de velocidade: a animação toca em 1× quando a Velocidade da unidade é este valor.")]
        [SerializeField] private float animationSpeedReference = 12f;

        /// <summary>Acerto garantido (pula a esquiva) — decisão de código da subclasse.</summary>
        protected virtual bool AlwaysHits => false;

        private readonly Dictionary<UnitController, bool> dodgeRolls = new Dictionary<UnitController, bool>();

        // Stats temporários concedidos ao dono durante a aplicação dos efeitos (ex.: +Penetração Mágica
        // na Onda do Hami). A subclasse os aplica com SkillStat(...) no início do OnImpact; revertidos no finally.
        private readonly List<KeyValuePair<StatType, string>> tempStats = new List<KeyValuePair<StatType, string>>();

        public override float Execute(SkillContext context)
        {
            context.allowCrit = true;
            OnCast(context);

            float mult = SpeedMultiplier(context, animationSpeedReference);
            return context.ownerVisual?.PlaySupremeAnimation(mult) ?? DefaultSkillDuration;
        }

        public override void OnHit(SkillContext context)
        {
            context.allowCrit = true;
            tempStats.Clear();
            try
            {
                var state = new EffectRunState();
                if (!AlwaysHits) { dodgeRolls.Clear(); state.dodgeRolls = dodgeRolls; }
                OnImpact(context, ref state);
            }
            finally { PopTempStats(context); }
        }

        /// <summary>
        /// Concede ao DONO um stat temporário SÓ durante esta aplicação (revertido no finally). Chame no
        /// início do <see cref="OnImpact"/>, antes dos módulos que devem ler o stat modificado (ex.: dano
        /// com +50 de Penetração Mágica). flat na convenção do stat (base 100 = 50 → 50%); percent = fração.
        /// </summary>
        protected void SkillStat(SkillContext context, StatType stat, float flat, float percent = 0f)
        {
            Stat s = context.ownerStats?.GetStatObject(stat);
            if (s == null) return;
            string id = "sstat_" + System.Guid.NewGuid().ToString("N");
            s.AddModifier(id, flat, percent);
            tempStats.Add(new KeyValuePair<StatType, string>(stat, id));
        }

        private void PopTempStats(SkillContext context)
        {
            for (int i = 0; i < tempStats.Count; i++)
                context.ownerStats?.GetStatObject(tempStats[i].Key)?.RemoveModifier(tempStats[i].Value);
            tempStats.Clear();
        }

        /// <summary>Efeitos de conjuração (antes da animação), ex.: salto. Sem esquiva. Vazio por padrão.</summary>
        protected virtual void OnCast(SkillContext context) { }

        /// <summary>Efeitos no frame de impacto da animação. A subclasse compõe os módulos.</summary>
        protected abstract void OnImpact(SkillContext context, ref EffectRunState state);
    }
}
