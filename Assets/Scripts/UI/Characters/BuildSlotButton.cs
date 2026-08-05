using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropInHeroes.UI
{

    /// <summary>Botão de build no seletor: nome + aviso (build incompleta) + destaque de seleção.</summary>
    public class BuildSlotButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject warningIcon;
        [SerializeField] private GameObject selectedOverlay;
        [SerializeField] private Button button;

        public void Setup(string text, bool incomplete, bool isSelected, Action onClick)
        {
            if (label != null) label.text = text;
            if (warningIcon != null) warningIcon.SetActive(incomplete);
            if (selectedOverlay != null) selectedOverlay.SetActive(isSelected);
            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke());
            }
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }
    }
}
