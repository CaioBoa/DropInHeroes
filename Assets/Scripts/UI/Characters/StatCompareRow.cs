using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropInHeroes.UI
{

    /// <summary>Linha da tabela de comparação da árvore: ícone + nome + valores Base / R1 / R2 / R3.</summary>
    public class StatCompareRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text baseValue;
        [SerializeField] private TMP_Text r1Value;
        [SerializeField] private TMP_Text r2Value;
        [SerializeField] private TMP_Text r3Value;

        public Image Icon => icon;
        public TMP_Text NameText => nameText;
        public TMP_Text BaseValue => baseValue;
        public TMP_Text R1 => r1Value;
        public TMP_Text R2 => r2Value;
        public TMP_Text R3 => r3Value;
    }
}
