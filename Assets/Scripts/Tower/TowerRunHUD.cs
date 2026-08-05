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
    /// HUD da run de Torre: número da fase, HP do jogador, HP da torre e timer atual.
    /// Atualizado pelo TowerRunController.
    /// </summary>
    public class TowerRunHUD : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI playerHPText;
        [SerializeField] private TextMeshProUGUI towerHPText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI stateText;

        [Header("Controls")]
        [SerializeField] private Button startCombatButton;

        public System.Action OnStartCombatPressed;

        private void Awake()
        {
            if (startCombatButton != null)
            {
                startCombatButton.onClick.AddListener(() => OnStartCombatPressed?.Invoke());
                startCombatButton.gameObject.SetActive(false);
            }
        }

        public void SetPhase(int current, int total)
        {
            if (phaseText != null) phaseText.text = $"FASE {current}/{total}";
        }

        public void SetPlayerHP(int hp)
        {
            if (playerHPText != null) playerHPText.text = $"JOGADOR: <color=#FF4444>{hp}</color>";
        }

        public void SetTowerHP(int hp)
        {
            if (towerHPText != null) towerHPText.text = $"TORRE: <color=#44FF44>{hp}</color>";
        }

        // Estado do último timer renderizado — evita reconstruir string + mesh TMP por frame quando
        // o segundo visível (e o bucket de cor) não mudou. -1 = limpo.
        private int lastTimerValue = int.MinValue;
        private bool lastTimerCritical;

        public void SetTimer(float secondsRemaining)
        {
            if (timerText == null) return;

            if (secondsRemaining < 0f)
            {
                if (lastTimerValue == -1) return;
                lastTimerValue = -1;
                timerText.text = string.Empty;
                return;
            }

            int value = Mathf.RoundToInt(secondsRemaining);
            bool critical = secondsRemaining <= 5f;
            if (value == lastTimerValue && critical == lastTimerCritical) return;

            lastTimerValue = value;
            lastTimerCritical = critical;
            timerText.text = $"<color={(critical ? "#FF0000" : "#FFFFFF")}>{value}</color>";
        }

        public void SetState(string label)
        {
            if (stateText != null) stateText.text = label.ToUpper();
        }

        public void SetStartCombatButtonVisible(bool visible)
        {
            if (startCombatButton != null) startCombatButton.gameObject.SetActive(visible);
        }
    }
}
