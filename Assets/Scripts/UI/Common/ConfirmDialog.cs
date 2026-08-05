using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Diálogo de confirmação modal reutilizável (temático). Chame <see cref="Show"/> com título, mensagem e
    /// o callback de confirmação. Confirmar dispara o callback e fecha; Cancelar (ou clicar no overlay) fecha
    /// sem confirmar. Coloque este componente num objeto SEMPRE ATIVO e ligue <see cref="root"/> ao painel
    /// filho que liga/desliga — assim os listeners são registrados no Awake mesmo com o diálogo oculto.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [Tooltip("Painel/overlay que é ligado/desligado. Deve ser um FILHO deste objeto (que fica sempre ativo).")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelLabel;
        [Tooltip("Botão que cobre o fundo; clicar fora fecha (cancela). Opcional.")]
        [SerializeField] private Button overlayButton;

        private Action onConfirm;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
            if (overlayButton != null) overlayButton.onClick.AddListener(Hide);
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Hide);
            if (overlayButton != null) overlayButton.onClick.RemoveListener(Hide);
        }

        public void Show(string title, string message, Action confirm, string confirmText = "Apagar", string cancelText = "Cancelar")
        {
            onConfirm = confirm;
            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            if (confirmLabel != null) confirmLabel.text = confirmText;
            if (cancelLabel != null) cancelLabel.text = cancelText;
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            onConfirm = null;
            if (root != null) root.SetActive(false);
        }

        private void Confirm()
        {
            Action c = onConfirm;
            onConfirm = null;
            if (root != null) root.SetActive(false);
            c?.Invoke();
        }
    }
}
