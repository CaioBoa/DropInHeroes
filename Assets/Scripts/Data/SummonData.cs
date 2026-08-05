using UnityEngine;
using System.Collections.Generic;
using DropInHeroes.Combat;
using DropInHeroes.Utils;

namespace DropInHeroes.Data
{

    /// <summary>
    /// CharacterData de INVOCAÇÃO: pré-configura o que toda invocação exige (tag Summon, capacidades
    /// reduzidas) e substitui o orçamento de pontos por stats base FIXOS — invocação NUNCA puxa o piso
    /// geral nem pontos de personagem (ver <see cref="StatBudget"/>). Stat ausente na lista = 0; o
    /// restante vem da derivação do dono (<see cref="summonStatProfile"/>) no momento da invocação.
    /// Campos de personagem sem sentido aqui (pontos, builds) são ocultados pelo editor custom.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSummon", menuName = "Game/Data/Summon")]
    public class SummonData : CharacterData
    {
        [Header("Base Stats fixos (invocação — sem piso geral, sem pontos)")]
        [Tooltip("Valores BRUTOS próprios da invocação. Stat ausente = 0. Somam com a derivação do dono.")]
        [SerializeField] private List<StatValueEntry> fixedStats = new List<StatValueEntry>();

        [Header("Derivação do dono")]
        [Tooltip("Perfil que deriva stats do INVOCADOR no spawn (snapshot). Vazio = só os stats fixos.")]
        [SerializeField] private SummonStatProfile summonStatProfile;

        /// <summary>Perfil de derivação a partir do dono (null = sem derivação).</summary>
        public SummonStatProfile SummonStatProfile => summonStatProfile;

        /// <summary>Valor bruto fixo de um stat desta invocação (0 se não listado).</summary>
        public float GetFixedStat(StatType type)
        {
            for (int i = 0; i < fixedStats.Count; i++)
                if (fixedStats[i].type == type) return fixedStats[i].value;
            return 0f;
        }

    #if UNITY_EDITOR
        private void Reset()
        {
            tags = UnitTag.Summon;
            capabilities = UnitCapability.Move | UnitCapability.Attack;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            tags |= UnitTag.Summon; // invariante do tipo: invocação é sempre Summon
        }
    #endif
    }

    /// <summary>Valor bruto fixo de um stat de invocação (sem conversão por pontos).</summary>
    [System.Serializable]
    public struct StatValueEntry
    {
        public StatType type;
        public float value;
    }
}
