using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Combat;

namespace DropInHeroes.UI
{

    /// <summary>Botão de opção de artefato na tela de Personagens. Setup liga a ArtifactData e dispara o callback.</summary>
    [RequireComponent(typeof(Button))]
    public class ArtifactOptionButton : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [Tooltip("Texto de explicação do efeito do artefato, ao lado do nome.")]
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject selectedOverlay;

        private Button button;
        private ArtifactData artifact;
        private Action<ArtifactData> onClick;

        public ArtifactData Artifact => artifact;

        private void Awake() => button = GetComponent<Button>();

        /// <summary>data null = opção "Sem artefato" (remover); description já vem pronto do <see cref="ArtifactTextBuilder"/>.</summary>
        public void Setup(ArtifactData data, string description, Action<ArtifactData> clickHandler)
        {
            artifact = data;
            onClick = clickHandler;
            if (icon != null) { icon.sprite = data != null ? data.icon : null; icon.enabled = data != null && data.icon != null; }
            if (nameText != null) nameText.text = data != null ? data.displayName : "Sem artefato";
            if (descriptionText != null) descriptionText.text = description;
            if (button == null) button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(artifact));
            SetSelected(false);
        }

        public void SetSelected(bool on)
        {
            if (selectedOverlay != null) selectedOverlay.SetActive(on);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }
    }
}
