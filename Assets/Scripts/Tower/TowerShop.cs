using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Loja popup que abre no início de cada prep. Sorteia N opções dentre os picks do jogador
    /// (com repetição), aguarda escolha do jogador, fecha. Aguardar via OpenAndAwaitChoice.
    /// </summary>
    public class TowerShop : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject popupRoot;

        [Header("Options Container")]
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private ShopOptionButton optionPrefab;

        private readonly List<ShopOptionButton> spawnedButtons = new List<ShopOptionButton>();
        private readonly List<int> shuffleIndices = new List<int>();
        private TaskCompletionSource<CharacterData> pendingChoice;

        private void Awake()
        {
            // Awake (não Start): garante que o popup já está oculto antes de qualquer
            // Start de outro componente. Evita race com TowerRunController.Start.
            if (popupRoot != null) popupRoot.SetActive(false);

            // Remove instâncias de preview deixadas no editor (não rastreadas pelo pool de opções),
            // para que não dupliquem com as opções geradas em runtime.
            if (optionsContainer != null)
                for (int i = optionsContainer.childCount - 1; i >= 0; i--)
                    Destroy(optionsContainer.GetChild(i).gameObject);
        }

        /// <summary>
        /// Abre o popup com até N opções DISTINTAS sorteadas do pool fornecido (sem reposição)
        /// e aguarda o jogador escolher uma. O caller é responsável por filtrar o pool
        /// (ex.: remover personagens já no rank máximo).
        /// Retorna null se mal configurado ou pool vazio.
        /// </summary>
        public Task<CharacterData> OpenAndAwaitChoice(IReadOnlyList<CharacterData> pool, int optionsCount)
        {
            if (!ValidateRefs())
                return Task.FromResult<CharacterData>(null);

            if (pool == null || pool.Count == 0)
            {
                DebugManager.LogWarning("TowerShop: pool vazio — pulando loja", DebugCategory.UI);
                return Task.FromResult<CharacterData>(null);
            }

            // Ativar ANTES de instanciar opções: filho de parent inativo nasce inativo
            // e o Awake do ShopOptionButton não roda → button null → NullRef em Setup.
            popupRoot.SetActive(true);
            int actualCount = BuildOptions(pool, optionsCount);
            DebugManager.Log($"Loja aberta com {actualCount} opções (de {pool.Count} disponíveis)", DebugCategory.UI);

            pendingChoice = new TaskCompletionSource<CharacterData>();
            return pendingChoice.Task;
        }

        private bool ValidateRefs()
        {
            if (popupRoot == null) { DebugManager.LogError("TowerShop: popupRoot não atribuído!", DebugCategory.UI); return false; }
            if (optionsContainer == null) { DebugManager.LogError("TowerShop: optionsContainer não atribuído!", DebugCategory.UI); return false; }
            if (optionPrefab == null) { DebugManager.LogError("TowerShop: optionPrefab não atribuído!", DebugCategory.UI); return false; }
            return true;
        }

        private int BuildOptions(IReadOnlyList<CharacterData> pool, int requestedCount)
        {
            // Sample sem reposição: shuffle parcial dos índices, take min(requested, pool.Count).
            shuffleIndices.Clear();
            for (int i = 0; i < pool.Count; i++) shuffleIndices.Add(i);
            for (int i = shuffleIndices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffleIndices[i], shuffleIndices[j]) = (shuffleIndices[j], shuffleIndices[i]);
            }

            int actualCount = Mathf.Min(requestedCount, pool.Count);

            for (int i = 0; i < actualCount; i++)
            {
                ShopOptionButton button;
                if (i < spawnedButtons.Count)
                {
                    button = spawnedButtons[i];
                    button.gameObject.SetActive(true);
                }
                else
                {
                    button = Instantiate(optionPrefab, optionsContainer);
                    spawnedButtons.Add(button);
                }

                button.Setup(pool[shuffleIndices[i]], OnOptionClicked);
            }

            for (int i = actualCount; i < spawnedButtons.Count; i++)
            {
                spawnedButtons[i].gameObject.SetActive(false);
            }

            return actualCount;
        }

        private void OnOptionClicked(CharacterData chosen)
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            pendingChoice?.TrySetResult(chosen);
            pendingChoice = null;
        }
    }
}
