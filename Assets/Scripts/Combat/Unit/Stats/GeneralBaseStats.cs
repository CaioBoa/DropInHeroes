using UnityEngine;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Piso universal de stats compartilhado por TODAS as unidades (ex.: crit, accuracy, velocidade,
    /// energia). O valor BRUTO final de cada unidade = este piso + (pontos da unidade × statPointValue
    /// do stat). Asset único em Resources — editado só aqui para ajustar o baseline global.
    /// </summary>
    [CreateAssetMenu(fileName = "GeneralBaseStats", menuName = "Game/System/General Base Stats")]
    public class GeneralBaseStats : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public StatType type;
            public float value;
        }

        [Tooltip("Valor base universal por stat (comum a todas as unidades). Stats de orçamento (Atk/HP/...) ficam em 0 aqui e vêm dos pontos por unidade.")]
        [SerializeField] private List<Entry> baseValues = new List<Entry>();

        private Dictionary<StatType, float> lookup;

        public float Get(StatType type)
        {
            // Reconstrói se o dicionário voltou a null após reimport/reload do asset.
            if (lookup == null)
            {
                lookup = new Dictionary<StatType, float>();
                for (int i = 0; i < baseValues.Count; i++) lookup[baseValues[i].type] = baseValues[i].value;
            }
            return lookup.TryGetValue(type, out var v) ? v : 0f;
        }
    }
}
