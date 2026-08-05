using System;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Regra de combate: aplica/remove um efeito a unidades que casam com um critério (tags).
    /// O <see cref="CombatRulesRegistry"/> aplica regras automaticamente a unidades que entram em
    /// combate — inclusive invocações criadas depois. Base para itens, relíquias e regras globais.
    /// </summary>
    public abstract class CombatRule
    {
        public abstract bool Matches(UnitController unit);
        public abstract void Apply(UnitController unit);
        public abstract void Remove(UnitController unit);

        protected static string NewId(string prefix) => prefix + "_" + Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Aplica um modificador de stat (flat/percent) a unidades de certas tags.
    /// Ex.: +0.5 de CritRate em todas as invocações → "item: invocações causam crítico".
    /// </summary>
    public sealed class StatModifierRule : CombatRule
    {
        private readonly UnitTag affected;
        private readonly StatType stat;
        private readonly float flat;
        private readonly float percent;
        private readonly string id;

        public StatModifierRule(UnitTag affected, StatType stat, float flat = 0f, float percent = 0f)
        {
            this.affected = affected;
            this.stat = stat;
            this.flat = flat;
            this.percent = percent;
            id = NewId("rule_stat");
        }

        public override bool Matches(UnitController unit) => unit != null && (unit.Tags & affected) != 0;

        public override void Apply(UnitController unit)
            => unit.GetModule<StatsModule>()?.GetStatObject(stat)?.AddModifier(id, flat, percent);

        public override void Remove(UnitController unit)
            => unit.GetModule<StatsModule>()?.GetStatObject(stat)?.RemoveModifier(id);
    }

    /// <summary>
    /// Aplica um multiplicador de dano (entrada ou saída) a unidades de certas tags, condicionado às
    /// tags da OUTRA ponta. Ex.: "unidades sofrem 30% menos dano DE invocações" →
    /// affected=Hero, direction=Incoming, conditionTags=Summon, multiplier=0.7.
    /// </summary>
    public sealed class DamageModifierRule : CombatRule
    {
        public enum Direction { Incoming, Outgoing }

        private readonly UnitTag affected;
        private readonly Direction direction;
        private readonly UnitTag conditionTags; // tags da outra ponta p/ o multiplicador valer (None = sempre)
        private readonly float multiplier;
        private readonly string id;

        public DamageModifierRule(UnitTag affected, Direction direction, UnitTag conditionTags, float multiplier)
        {
            this.affected = affected;
            this.direction = direction;
            this.conditionTags = conditionTags;
            this.multiplier = multiplier;
            id = NewId("rule_dmg");
        }

        public override bool Matches(UnitController unit) => unit != null && (unit.Tags & affected) != 0;

        public override void Apply(UnitController unit)
        {
            var stats = unit.GetModule<StatsModule>();
            if (stats == null) return;
            if (direction == Direction.Incoming)
                stats.AddIncomingDamageModifier(id, ctx => Conditioned(ctx.sourceTags));
            else
                stats.AddOutgoingDamageModifier(id, ctx => Conditioned(ctx.targetTags));
        }

        public override void Remove(UnitController unit)
        {
            var stats = unit.GetModule<StatsModule>();
            if (stats == null) return;
            if (direction == Direction.Incoming) stats.RemoveIncomingDamageModifier(id);
            else stats.RemoveOutgoingDamageModifier(id);
        }

        private float Conditioned(UnitTag otherTags)
            => (conditionTags == UnitTag.None || (otherTags & conditionTags) != 0) ? multiplier : 1f;
    }
}
