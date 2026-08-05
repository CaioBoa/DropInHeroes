using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Combat;

namespace DropInHeroes.UI
{
    /// <summary>
    /// Célula de stat autorada no painel CharacterInfo: declara qual <see cref="StatType"/> exibe e
    /// expõe o ícone e o texto de valor. O <see cref="CharacterInfoPanel"/> lê as células sob o grid
    /// e as preenche (ícone/cor/formato vêm do StatDefinitionCatalog). Editável 100% no editor —
    /// arraste/dimensione no Inspector e escolha o stat de cada célula.
    /// </summary>
    public class StatCellView : MonoBehaviour
    {
        [SerializeField] private StatType type;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text value;

        public StatType Type => type;
        public Image Icon => icon;
        public TMP_Text Value => value;
    }
}
