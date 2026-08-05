using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Data;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Painel de um jogador num canto da tela de picks. Perfil (placeholder), nome e o time em mini-retratos.
    /// Preparado para multiplayer (4 cantos): um é o jogador local, os outros ficam "Aguardando jogador".
    /// </summary>
    public class PlayerCornerPanel : MonoBehaviour
    {
        [SerializeField] private Image profileImage;
        [SerializeField] private TMP_Text playerNameText;
        [Tooltip("Mini-retratos do time (tamanho = tamanho do time).")]
        [SerializeField] private Image[] teamPortraits;
        [Tooltip("Overlay 'Aguardando jogador' (canto sem jogador — futuro MP).")]
        [SerializeField] private GameObject waitingOverlay;
        [Tooltip("Destaque do jogador local.")]
        [SerializeField] private GameObject localHighlight;

        public void SetupLocal(string playerName)
        {
            if (waitingOverlay != null) waitingOverlay.SetActive(false);
            if (localHighlight != null) localHighlight.SetActive(true);
            if (playerNameText != null) playerNameText.text = playerName;
            SetTeam(null);
        }

        public void SetupWaiting()
        {
            if (waitingOverlay != null) waitingOverlay.SetActive(true);
            if (localHighlight != null) localHighlight.SetActive(false);
            if (playerNameText != null) playerNameText.text = "Aguardando jogador";
            SetTeam(null);
        }

        public void SetTeam(IReadOnlyList<CharacterData> team)
        {
            if (teamPortraits == null) return;
            for (int i = 0; i < teamPortraits.Length; i++)
            {
                if (teamPortraits[i] == null) continue;
                CharacterData c = team != null && i < team.Count ? team[i] : null;
                Sprite s = c != null ? (c.cardPortrait != null ? c.cardPortrait : c.defaultSprite) : null;
                teamPortraits[i].sprite = s;
                teamPortraits[i].enabled = s != null;
            }
        }
    }
}
