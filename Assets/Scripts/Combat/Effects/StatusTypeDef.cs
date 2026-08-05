using System;
using UnityEngine;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Classificação única do status — funde o antigo <c>kind</c> (Buff/Debuff) com <c>category</c>.
    /// Dita a cor do widget e as regras universais: Negative conta como debuff (<see cref="StatusModule.DebuffCount"/>,
    /// purificável); Positive conta como buff (dispelável); Neutral não é nenhum.
    /// </summary>
    public enum StatusKind { Positive, Negative, Neutral }

    /// <summary>
    /// Família de comportamento de um tipo de status — o ÚNICO mecanismo que ele executa no
    /// <see cref="StatusModule"/>. Um tipo tem exatamente uma família; combinações se fazem aplicando
    /// dois status. Cada família é tratada por um bloco padronizado do módulo.
    /// </summary>
    public enum StatusBehavior
    {
        /// <summary>Buff/debuff de stat: aplica/reverte a <c>magnitude</c> (modificadores) por instância.</summary>
        StatModifier,
        /// <summary>Dano verdadeiro por tick enquanto ativo (queimadura, veneno). A magnitude é dano/seg.</summary>
        DamageOverTime,
        /// <summary>Controle (stun/root): imobiliza o portador; duração escala com Control/Tenacity.</summary>
        Control,
        /// <summary>Furtividade (targeting): o portador não pode ser focado enquanto ativo.</summary>
        Stealth,
        /// <summary>Provocação (targeting): força o portador a focar quem aplicou o status.</summary>
        Taunt
    }

    /// <summary>
    /// Linha da tabela de tipos de status (<see cref="StatusCatalog"/>) — a fonte única dos traços
    /// NATIVOS e imutáveis do tipo: identidade, classificação (<see cref="StatusKind"/>), família de
    /// comportamento (<see cref="StatusBehavior"/>), empilhamento e regras de expiração. Os VALORES
    /// parametrizáveis (duração, magnitude, chance) NÃO ficam aqui — vêm da SKILL/passiva que aplica,
    /// via <see cref="StatusModule.ApplyStatus"/>.
    /// </summary>
    [Serializable]
    public class StatusTypeDef
    {
        [Header("Identidade")]
        public string id;
        public string displayName;
        [TextArea(1, 3)] public string description;
        [Tooltip("Ícone do status. Se nulo, deriva do stat modificado (via StatDefinitionCatalog).")]
        public Sprite icon;

        [Header("Classificação")]
        [Tooltip("Positive = buff; Negative = debuff (conta em DebuffCount e é purificável); Neutral = nenhum.")]
        public StatusKind kind = StatusKind.Negative;
        [Tooltip("Família de comportamento: o mecanismo único que este tipo executa no StatusModule.")]
        public StatusBehavior behavior = StatusBehavior.StatModifier;
        [Tooltip("Condição: imune a limpar-debuff / dispel-buff genéricos. Só sai pelas regras de expiração.")]
        public bool isCondition;
        [Tooltip("Status específico da unidade-matriz — não faria sentido/uso em outra unidade (ex.: contadores/condições de forma). Metadado de organização/tooling; sem efeito mecânico.")]
        public bool isUnique;

        [Header("Empilhamento")]
        public StackMode stackMode = StackMode.Overwrite;
        [Tooltip("Instances: máx. de instâncias simultâneas. Accumulate: máx. de stacks. <= 0 = ilimitado.")]
        public int maxStacks = 1;

        [Header("DamageOverTime")]
        [Tooltip("Intervalo (s) entre ticks de dano. Usado apenas por behavior = DamageOverTime.")]
        public float tickInterval = 1f;

        [Header("Expiração")]
        [Tooltip("Regras de saída/perda de stack. Vazio = permanente até o fim do combate. " +
                 "Ex.: [Duration→RemoveEffect]; [OwnerSupremeCast→RemoveEffect]; [OwnerDamageTaken→RemoveStack].")]
        public StatusExpiry[] expiries = Array.Empty<StatusExpiry>();

        /// <summary>Há uma regra de expiração por tempo? (define se a duração parametrizada é usada.)</summary>
        public bool HasDurationExpiry
        {
            get
            {
                if (expiries == null) return false;
                for (int i = 0; i < expiries.Length; i++)
                    if (expiries[i].trigger == StatusExpiryTrigger.Duration) return true;
                return false;
            }
        }
    }
}
