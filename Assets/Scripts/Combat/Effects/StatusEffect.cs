using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>Qual número (no máximo um) o indicador do status exibe.</summary>
    public enum StatusIndicator { None, Duration, Stacks }

    /// <summary>
    /// Uma modificação de stat aplicada por um efeito. percent: -0.30 = -30%.
    /// </summary>
    [System.Serializable]
    public struct StatModification
    {
        public StatType stat;
        public float flat;
        public float percent;

        public StatModification(StatType stat, float flat, float percent)
        {
            this.stat = stat;
            this.flat = flat;
            this.percent = percent;
        }

        public static StatModification Percent(StatType stat, float percent) => new StatModification(stat, 0f, percent);
        public static StatModification Flat(StatType stat, float flat) => new StatModification(stat, flat, 0f);
    }

    /// <summary>
    /// Efeito de status em tempo real, construído a partir de um <see cref="StatusTypeDef"/> (o TIPO)
    /// mais os valores parametrizados na aplicação. Duration em segundos; Duration &lt;= 0 = permanente
    /// até o fim do combate. As Modifications são aplicadas no início e revertidas na saída.
    /// </summary>
    public class StatusEffect
    {
        public string Id;
        public float Duration;
        public float Remaining;
        public StatModification[] Modifications;

        /// <summary>Classificação única (Positive/Negative/Neutral): dita cor do widget e regras universais.</summary>
        public StatusKind Kind;

        /// <summary>Família de comportamento — o mecanismo único que o StatusModule executa para este efeito.</summary>
        public StatusBehavior Behavior;

        // Ícone próprio do status (vindo do tipo). Se null e houver Modifications, o widget deriva o
        // ícone do stat modificado (Modifications[0]) via StatDefinitionCatalog.
        public Sprite Icon;

        // Qual número o indicador mostra (no máximo um): duração restante OU stacks.
        public StatusIndicator Indicator = StatusIndicator.Duration;

        // Quantidade de aplicações empilhadas (incrementado ao re-aplicar o mesmo Id). Apenas
        // exibição — não multiplica os modificadores (o rescale por stack faz isso).
        public int Stacks = 1;

        // Dano por tempo (DoT). Se DamagePerTick > 0, o StatusModule aplica esse dano VERDADEIRO e FIXO
        // (sem mitigação) a cada TickInterval segundos enquanto ativo — ex.: a queimadura "Incendiar".
        public float DamagePerTick;
        public float TickInterval = 1f;
        public float TickTimer; // segundos até o próximo tick (inicializado no apply pelo StatusModule)

        // Taunt: se definido, o portador é forçado a focar o alvo resolvido por esta query. Para a família
        // Taunt o StatusModule preenche com {Applier} (o provocador) quando não especificado.
        public TargetQuery? ForcedFocus;

        // Quem aplicou o efeito (definido por StatusModule.ApplyStatus). Resolve TargetSide.Applier.
        public UnitController AppliedBy;

        // === Traços NATIVOS do tipo (copiados do StatusTypeDef na aplicação) ===

        /// <summary>Tipo (fonte de ícone/nome/descrição e das regras de expiração/stack).</summary>
        public StatusTypeDef TypeDef;

        /// <summary>Condição: imune a limpar-debuff/dispel genéricos; só sai pelas <see cref="Expiries"/>.</summary>
        public bool IsCondition;

        /// <summary>Como o MESMO tipo se comporta ao ser reaplicado (ver <see cref="StackMode"/>).</summary>
        public StackMode StackMode = StackMode.Overwrite;

        /// <summary>Instances: máx. de instâncias do tipo. Accumulate: máx. de stacks. <= 0 = ilimitado.</summary>
        public int MaxStacks = 1;

        /// <summary>Regras de saída/perda de stack (gatilho → ação). Vazio = até o fim do combate.</summary>
        public StatusExpiry[] Expiries;

        /// <summary>Magnitude por STACK (base do Accumulate). Igual a Modifications no apply; usada para reescalar.</summary>
        public StatModification[] PerStackModifications;

        /// <summary>DoT por STACK (base do Accumulate). O DamagePerTick efetivo = este × Stacks.</summary>
        public float PerStackDamagePerTick;

        // Chave ÚNICA por instância (não pelo tipo Id): usada para os modifiers de stat, para que várias
        // instâncias do mesmo tipo (StackMode.Instances) não colidam suas chaves.
        public string InstanceKey;

        private static int instanceCounter;

        private StatusEffect(string id, float duration, StatModification[] modifications)
        {
            Id = id;
            Duration = duration;
            Remaining = duration;
            Modifications = modifications;
            PerStackModifications = modifications;
            InstanceKey = id + "#" + (++instanceCounter);
        }

        /// <summary>
        /// Fonte ÚNICA de construção: monta um efeito do <paramref name="type"/> com os traços nativos
        /// copiados e os valores desta aplicação (<paramref name="duration"/>, <paramref name="mods"/>,
        /// <paramref name="dps"/>). dps &lt;= 0 = sem DoT.
        /// </summary>
        public static StatusEffect Build(StatusTypeDef type, float duration, StatModification[] mods, float dps)
        {
            float tick = type.tickInterval > 0f ? type.tickInterval : 1f;
            return new StatusEffect(type.id, duration, mods)
            {
                TypeDef = type,
                Kind = type.kind,
                Behavior = type.behavior,
                Icon = type.icon,
                Indicator = type.stackMode == StackMode.Accumulate ? StatusIndicator.Stacks : StatusIndicator.Duration,
                IsCondition = type.isCondition,
                StackMode = type.stackMode,
                MaxStacks = type.maxStacks,
                Expiries = type.expiries,
                DamagePerTick = dps > 0f ? dps * tick : 0f,
                TickInterval = tick
            };
        }

        // Flags derivadas da família (comportamentos exclusivos): evitam bools redundantes no dado.
        public bool IsDebuff => Kind == StatusKind.Negative;
        public bool IsStatModifier => Behavior == StatusBehavior.StatModifier;
        public bool IsDamageOverTime => Behavior == StatusBehavior.DamageOverTime;
        public bool IsControl => Behavior == StatusBehavior.Control;
        public bool IsStealth => Behavior == StatusBehavior.Stealth;
        public bool IsTaunt => Behavior == StatusBehavior.Taunt;

        /// <summary>Tem regra de expiração por tempo? (senão, Remaining não é decrementado.)</summary>
        public bool HasDurationExpiry
        {
            get
            {
                if (Expiries == null) return false;
                for (int i = 0; i < Expiries.Length; i++)
                    if (Expiries[i].trigger == StatusExpiryTrigger.Duration) return true;
                return false;
            }
        }
    }
}
