using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DropInHeroes.Core;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Tela de fim de run da Torre: título de vitória/derrota, resumo (andar alcançado,
    /// rounds vencidos/perdidos, vidas restantes) e botões que finalizam o fluxo (jogar
    /// de novo / menu). O <see cref="TowerRunController"/> delega aqui via <see cref="Show"/>;
    /// os botões trocam de cena pelo fluxo de Loading.
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        [Header("Título")]
        [SerializeField] private TMP_Text titleText;

        [Header("Resumo (valores)")]
        [SerializeField] private TMP_Text floorValue;
        [SerializeField] private TMP_Text winsValue;
        [SerializeField] private TMP_Text lossesValue;
        [SerializeField] private TMP_Text livesValue;

        [Header("Botões")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button menuButton;

        [Header("Resultado")]
        [SerializeField] private string victoryText = "VITÓRIA!";
        [SerializeField] private string defeatText = "DERROTA";
        [Tooltip("Cor do título quando o jogador vence.")]
        [SerializeField] private Color victoryColor = new Color(0.45f, 0.80f, 0.40f, 1f);
        [Tooltip("Cor do título quando o jogador perde.")]
        [SerializeField] private Color defeatColor = new Color(0.86f, 0.36f, 0.36f, 1f);

        private void OnEnable()
        {
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgain);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenu);
        }

        private void OnDisable()
        {
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgain);
            if (menuButton != null) menuButton.onClick.RemoveListener(OnMenu);
        }

        /// <summary>Preenche o resumo e exibe a tela. Ativar dispara o wiring dos botões (OnEnable).</summary>
        public void Show(bool playerWon, string floorReached, int roundsWon, int roundsLost, int livesLeft)
        {
            if (titleText != null)
            {
                titleText.text = playerWon ? victoryText : defeatText;
                titleText.color = playerWon ? victoryColor : defeatColor;
            }
            if (floorValue != null) floorValue.text = floorReached;
            if (winsValue != null) winsValue.text = roundsWon.ToString();
            if (lossesValue != null) lossesValue.text = roundsLost.ToString();
            if (livesValue != null) livesValue.text = livesLeft.ToString();

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void OnPlayAgain()
        {
            LoadingRequest.Configure(SceneNames.TowerPicks);
            SceneManager.LoadScene(SceneNames.Loading);
        }

        private void OnMenu()
        {
            LoadingRequest.Configure(SceneNames.MainMenu);
            SceneManager.LoadScene(SceneNames.Loading);
        }
    }
}
