using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    [System.Serializable]
    public class StatDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Tooltip("Quanto de valor BRUTO vale 1 ponto de orçamento deste stat. Ex.: 10 → 1 ponto = 10 de Ataque. Stats gerais (que não são orçamento) ficam em 1.")]
        public float statPointValue = 1f;
        [Tooltip("Formata o valor como porcentagem no widget (ex.: 0.05 → \"5%\").")]
        public bool isPercent;
        [Tooltip("Cor associada ao stat (opcional) — para tintar textos/valores. Nem todo stat precisa.")]
        public bool hasColor;
        public Color color = Color.white;
        public bool hasMinValue;
        public float minValue;
        public bool hasMaxValue;
        public float maxValue;

        public float? MinValue => hasMinValue ? minValue : null;
        public float? MaxValue => hasMaxValue ? maxValue : null;
        public Color? TintColor => hasColor ? color : (Color?)null;
    }
}
