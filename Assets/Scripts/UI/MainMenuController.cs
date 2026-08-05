using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button towerButton;
        [SerializeField] private Button charactersButton;
        [SerializeField] private Button sandboxButton;
        [SerializeField] private Button settingsButton;

        private void Start()
        {
            if (towerButton != null) towerButton.onClick.AddListener(OnTowerClicked);
            if (charactersButton != null) charactersButton.onClick.AddListener(OnCharactersClicked);
            if (sandboxButton != null) sandboxButton.onClick.AddListener(OnSandboxClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDestroy()
        {
            if (towerButton != null) towerButton.onClick.RemoveListener(OnTowerClicked);
            if (charactersButton != null) charactersButton.onClick.RemoveListener(OnCharactersClicked);
            if (sandboxButton != null) sandboxButton.onClick.RemoveListener(OnSandboxClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        private void OnTowerClicked()
        {
            DebugManager.Log("Iniciando modo Torre (Picks)...", DebugCategory.UI);
            LoadingRequest.Configure(SceneNames.TowerPicks);
            SceneManager.LoadScene(SceneNames.Loading);
        }

        private void OnCharactersClicked()
        {
            DebugManager.Log("Abrindo tela de Personagens...", DebugCategory.UI);
            LoadingRequest.Configure(SceneNames.Characters);
            SceneManager.LoadScene(SceneNames.Loading);
        }

        private void OnSandboxClicked()
        {
            DebugManager.Log("Abrindo modo Sandbox...", DebugCategory.UI);
            LoadingRequest.Configure(SceneNames.Sandbox);
            SceneManager.LoadScene(SceneNames.Loading);
        }

        private void OnSettingsClicked()
        {
            DebugManager.LogWarning("Configurações ainda não implementadas", DebugCategory.UI);
        }
    }
}
