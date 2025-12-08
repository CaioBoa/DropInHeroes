using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string text;
    public string leftCharacterId; // Retrato do personagem à esquerda (opcional)
    public string rightCharacterId; // Retrato do personagem à direita (opcional)
    public bool isLeftSpeaking; // Indica se o personagem da esquerda está falando
    public bool isSingleDialogue; // Indica se é um diálogo solo (sem personagem à direita)
    
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/Data/Dialogue")]
public class DialogueData : ScriptableObject, IGameData
{
    [Header("Metadata")]
    [SerializeField] private string id;
    [SerializeField] private DataCategory category = DataCategory.Dialogue;
    
    [Header("Dialogue Content")]
    public DialogueLine[] lines;
    
    // Interface implementation
    public string ID => id;
    public DataCategory Category => category;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = name.ToLower().Replace(" ", "_");
        }
    }
#endif
}