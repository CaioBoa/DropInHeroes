using UnityEngine;
using System.Collections.Generic;
using DropInHeroes.Combat;
using DropInHeroes.Utils;

namespace DropInHeroes.Data
{

    /// <summary>
    /// Catálogo centralizado dos dados do jogo (Tower mode).
    /// Single Source of Truth para CharacterData.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDataCatalog", menuName = "Game/System/Data Catalog")]
    public class GameDataCatalog : ScriptableObject
    {
        [Header("Data Registries")]
        [SerializeField] private DataRegistry<CharacterData> characters = new DataRegistry<CharacterData>();
        [SerializeField] private DataRegistry<ArtifactData> artifacts = new DataRegistry<ArtifactData>();
        [SerializeField] private DataRegistry<StatTreeData> statTrees = new DataRegistry<StatTreeData>();

        [Header("Status")]
        [Tooltip("Tabela única de tipos de status (substitui os antigos assets StatusDefinition).")]
        [SerializeField] private StatusCatalog statusCatalog;

        public void Initialize()
        {
            characters.Initialize();
            artifacts.Initialize();
            statTrees.Initialize();
            if (statusCatalog != null) statusCatalog.Initialize();
        }

        public CharacterData GetCharacter(string id) => characters.GetByID(id);
        public List<CharacterData> GetAllCharacters() => characters.GetAll();
        public List<CharacterData> QueryCharacters(System.Func<CharacterData, bool> predicate) => characters.Query(predicate);

        public ArtifactData GetArtifact(string id) => artifacts.GetByID(id);
        public List<ArtifactData> GetAllArtifacts() => artifacts.GetAll();

        public StatTreeData GetStatTree(string id) => statTrees.GetByID(id);
        // A árvore é única e compartilhada por todos os personagens: o primeiro asset indexado.
        public StatTreeData GetSharedStatTree() => statTrees.Count > 0 ? statTrees.GetAll()[0] : null;

        public StatusTypeDef GetStatus(string id) => statusCatalog != null ? statusCatalog.Get(id) : null;
        public IReadOnlyList<StatusTypeDef> GetAllStatuses() => statusCatalog != null ? statusCatalog.All : null;

    #if UNITY_EDITOR
        [ContextMenu("Auto-Scan All Data")]
        public void AutoScanAllData()
        {
            characters.AutoScanProject();
            artifacts.AutoScanProject();
            statTrees.AutoScanProject();
            // statusCatalog é uma tabela única (não auto-escaneável): edite as linhas no próprio asset.
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[GameDataCatalog] Auto-scan completo!");
        }

        [ContextMenu("Validate Data Integrity")]
        public void ValidateDataIntegrity()
        {
            Debug.Log("=== VALIDANDO INTEGRIDADE DE DADOS ===");
            var errors = characters.ValidateIntegrity();
            errors.AddRange(artifacts.ValidateIntegrity());
            errors.AddRange(statTrees.ValidateIntegrity());
            if (errors.Count == 0)
            {
                Debug.Log("✓ Todos os dados estão válidos!");
                return;
            }

            Debug.LogWarning($"{errors.Count} erro(s) encontrado(s):");
            foreach (var error in errors)
            {
                Debug.LogError($"  - {error}");
            }
        }
    #endif
    }
}
