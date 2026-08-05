using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Botão de uma opção da loja. Mostra portrait + nome do personagem.
    /// Click delega via callback para a TowerShop.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ShopOptionButton : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI nameText;

        private Button button;
        private CharacterData characterData;
        private Action<CharacterData> onClick;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
        }

        public void Setup(CharacterData data, Action<CharacterData> clickHandler)
        {
            // Lazy-init defensivo: se este componente foi instanciado em um parent inativo,
            // Awake ainda não rodou. RequireComponent garante que o Button existe.
            if (button == null) button = GetComponent<Button>();

            characterData = data;
            onClick = clickHandler;

            if (portrait != null) portrait.sprite = data != null ? data.cardPortrait : null;
            if (nameText != null) nameText.text = data != null ? data.displayName : string.Empty;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            onClick?.Invoke(characterData);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }
    }
}
