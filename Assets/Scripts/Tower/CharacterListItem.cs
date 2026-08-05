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
    /// Item da lista de personagens na tela de picks da Torre.
    /// Recebe a CharacterData via Setup e dispara o callback no clique.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CharacterListItem : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject claimedOverlay;

        private Button button;
        private CharacterData characterData;
        private Action<CharacterData> onClick;

        public CharacterData CharacterData => characterData;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void Setup(CharacterData data, Action<CharacterData> clickHandler)
        {
            characterData = data;
            onClick = clickHandler;

            if (portrait != null) portrait.sprite = data != null ? data.cardPortrait : null;
            if (nameText != null) nameText.text = data != null ? data.displayName : string.Empty;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);

            SetClaimed(false);
        }

        public void SetClaimed(bool isClaimed)
        {
            if (claimedOverlay != null) claimedOverlay.SetActive(isClaimed);
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
