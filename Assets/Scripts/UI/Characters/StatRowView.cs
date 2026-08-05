using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Linha de stat do Overview: fundo (listras alternadas), ícone + nome + valor BASE e valor da
    /// BUILD (rank 3) lado a lado. O <see cref="CharacterMenuPanel"/> instancia e preenche; ícone/
    /// cor/formato vêm do catálogo.
    /// </summary>
    public class StatRowView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text baseValue;
        [SerializeField] private TMP_Text buildValue;

        public Image Background => background;
        public Image Icon => icon;
        public TMP_Text NameText => nameText;
        public TMP_Text BaseValue => baseValue;
        public TMP_Text BuildValue => buildValue;
    }
}
