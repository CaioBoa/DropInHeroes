using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Data/Character")]
public class CharacterData : ScriptableObject, IGameData
{
    [Header("Metadata (Governance)")]
    [SerializeField] private string id;
    [SerializeField] private DataCategory category = DataCategory.Character;
    
    [Header("Visual")]
    public AnimationClip idleAnimation;
    public AnimationClip runAnimation;

    [Header("Portraits & Name")]
    public Sprite defaultSprite;
    public Sprite dialoguePortrait;
    public string displayName;
    
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