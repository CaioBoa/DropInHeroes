using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Como um status se comporta quando o MESMO tipo é reaplicado na unidade. Nativo do tipo
    /// (definido no <see cref="StatusTypeDef"/>), não parametrizável por aplicação.
    /// </summary>
    public enum StackMode
    {
        /// <summary>Uma instância só. Reaplicar do mesmo tipo SUBSTITUI (renova valor e duração).
        /// Ex.: um Aumento de Ataque comum de 40% sobrescreve um de 30% do mesmo tipo.</summary>
        Overwrite,

        /// <summary>N instâncias independentes coexistem, cada uma com sua duração/valor próprios.
        /// Ex.: 3 Incendiar simultâneos, cada um com seu DoT e sua contagem de tempo.</summary>
        Instances,

        /// <summary>Uma instância que ACUMULA stacks; a magnitude (modificadores/DoT) escala com o
        /// número de stacks (linear). Ex.: um veneno que empilha um valor crescente.</summary>
        Accumulate
    }

    /// <summary>
    /// Gatilho que faz um status expirar ou perder stack. Todos referem-se a eventos do PORTADOR do
    /// status (reaproveita o <see cref="PassiveHooks"/> da unidade). Nativo do tipo.
    /// </summary>
    public enum StatusExpiryTrigger
    {
        /// <summary>Passagem de tempo. O VALOR (segundos) é parametrizado na aplicação.</summary>
        Duration,
        /// <summary>O portador conjurou o supremo.</summary>
        OwnerSupremeCast,
        /// <summary>O portador executou um ataque básico (após o golpe).</summary>
        OwnerAttack,
        /// <summary>O portador causou dano.</summary>
        OwnerDamageDealt,
        /// <summary>O portador sofreu dano.</summary>
        OwnerDamageTaken,
        /// <summary>O portador foi acertado por um ataque ofensivo (não DoT/compartilhado).</summary>
        OwnerAttacked,
        /// <summary>O portador recebeu cura.</summary>
        OwnerHealReceived
    }

    /// <summary>O que o gatilho faz ao status: remover por inteiro ou tirar stacks.</summary>
    public enum StatusExpiryAction
    {
        RemoveEffect,
        RemoveStack
    }

    /// <summary>
    /// Regra modular de expiração de um status (gatilho → ação). Uma definição pode ter várias
    /// (ex.: Duração para sumir + "ao sofrer dano" para perder 1 stack). O gatilho e a ação são
    /// nativos do tipo; só o valor de Duração vem parametrizado da aplicação.
    /// </summary>
    [System.Serializable]
    public struct StatusExpiry
    {
        public StatusExpiryTrigger trigger;
        public StatusExpiryAction action;
        [Tooltip("Quantos stacks remover quando action = RemoveStack. <= 0 é tratado como 1.")]
        public int stackAmount;

        public int StackAmountOrOne => stackAmount > 0 ? stackAmount : 1;
    }
}
