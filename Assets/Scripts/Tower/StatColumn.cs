using TMPro;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Coluna de stat exibida no painel de detalhes (label em cima, valor embaixo).
    /// Múltiplas colunas são dispostas horizontalmente pelo StatsContainer.
    /// </summary>
    public class StatColumn : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI valueText;

        public void Set(string label, float value)
        {
            if (labelText != null) labelText.text = label;
            if (valueText != null) valueText.text = value.ToString("0.##");
        }
    }
}
