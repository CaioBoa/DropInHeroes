using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Catálogo centralizado de TODOS os dados do jogo
/// Single Source of Truth
/// </summary>
[CreateAssetMenu(fileName = "GameDataCatalog", menuName = "Game/System/Data Catalog")]
public class GameDataCatalog : ScriptableObject
{
    [Header("Data Registries")]
    [SerializeField] private DataRegistry<CharacterData> characters = new DataRegistry<CharacterData>();
    [SerializeField] private DataRegistry<DialogueData> dialogues = new DataRegistry<DialogueData>();
    [SerializeField] private DataRegistry<BattleData> battles = new DataRegistry<BattleData>();
    // Adicione mais conforme necessário:
    // [SerializeField] private DataRegistry<ItemData> items = new DataRegistry<ItemData>();
    // [SerializeField] private DataRegistry<SkillData> skills = new DataRegistry<SkillData>();
    

    public void Initialize()
    {   
        characters.Initialize();
        dialogues.Initialize();
        battles.Initialize();
        // items.Initialize();
        // skills.Initialize();
    }

    // === CHARACTER ACCESS ===
    public CharacterData GetCharacter(string id) => characters.GetByID(id);
    public List<CharacterData> GetAllCharacters() => characters.GetAll();
    public List<CharacterData> QueryCharacters(System.Func<CharacterData, bool> predicate) => characters.Query(predicate);

    // === DIALOGUE ACCESS ===
    public DialogueData GetDialogue(string id) => dialogues.GetByID(id);
    public List<DialogueData> GetAllDialogues() => dialogues.GetAll();
    public List<DialogueData> QueryDialogues(System.Func<DialogueData, bool> predicate) => dialogues.Query(predicate);

    // === BATTLE ACCESS ===
    public BattleData GetBattle(string id) => battles.GetByID(id);
    public List<BattleData> GetAllBattles() => battles.GetAll();
    public List<BattleData> QueryBattles(System.Func<BattleData, bool> predicate) => battles.Query(predicate);
    
#if UNITY_EDITOR
    [ContextMenu("Auto-Scan All Data")]
    public void AutoScanAllData()
    {
        characters.AutoScanProject();
        dialogues.AutoScanProject();
        battles.AutoScanProject();
        // items.AutoScanProject();
        // skills.AutoScanProject();
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[GameDataCatalog] Auto-scan completo!");
    }
    
    [ContextMenu("Validate Data Integrity")]
    public void ValidateDataIntegrity()
    {
        Debug.Log("=== VALIDANDO INTEGRIDADE DE DADOS ===");
        
        List<string> allErrors = new List<string>();
        
        // Validar Characters
        var characterErrors = characters.ValidateIntegrity();
        if (characterErrors.Count > 0)
        {
            Debug.LogWarning($"[Characters] {characterErrors.Count} erros encontrados:");
            allErrors.AddRange(characterErrors);
        }

        // Validar Dialogues
        var dialogueErrors = dialogues.ValidateIntegrity();
        if (dialogueErrors.Count > 0)
        {
            Debug.LogWarning($"[Dialogues] {dialogueErrors.Count} erros encontrados:");
            allErrors.AddRange(dialogueErrors);
        }

        // Validar Battles
        var battleErrors = battles.ValidateIntegrity();
        if (battleErrors.Count > 0)
        {
            Debug.LogWarning($"[Battles] {battleErrors.Count} erros encontrados:");
            allErrors.AddRange(battleErrors);
        }
        
        if (allErrors.Count == 0)
        {
            Debug.Log("✓ Todos os dados estão válidos!");
        }
        else
        {
            foreach (var error in allErrors)
            {
                Debug.LogError($"  - {error}");
            }
        }
    }
#endif
}