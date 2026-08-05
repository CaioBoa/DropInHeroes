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
    /// Slot do time da Torre (uma posição do time). Mostra o personagem escolhido naquela posição
    /// ou um placeholder vazio. Clique remove o pick (delegando para o controller).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TeamSlot : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject emptyPlaceholder;

        private Button button;
        private int slotIndex;
        private Action<int> onClick;
        private CharacterData currentCharacter;

        public CharacterData CurrentCharacter => currentCharacter;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void Initialize(int index, Action<int> clickHandler)
        {
            slotIndex = index;
            onClick = clickHandler;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);

            SetCharacter(null);
        }

        public void SetCharacter(CharacterData data)
        {
            currentCharacter = data;
            bool isEmpty = data == null;

            if (emptyPlaceholder != null) emptyPlaceholder.SetActive(isEmpty);

            if (portrait != null)
            {
                portrait.gameObject.SetActive(!isEmpty);
                if (!isEmpty) portrait.sprite = data.cardPortrait;
            }

            if (nameText != null)
            {
                nameText.gameObject.SetActive(!isEmpty);
                if (!isEmpty) nameText.text = data.displayName;
            }

            button.interactable = !isEmpty;
        }

        private void HandleClick()
        {
            onClick?.Invoke(slotIndex);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }
    }
}
