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
    public AnimationClip dragAnimation;
    public AnimationClip attackAnimation;
    public AnimationClip deathAnimation;
    public AnimationClip deathIdleAnimation;
    public AnimationClip supremeAnimation;
    public AnimationClip stunAnimation;
    public AnimationClip victoryAnimation;

    [Header("Portraits & Name")]
    public Sprite defaultSprite;
    public Sprite dialoguePortrait;
    public Sprite cardPortrait;
    public string displayName;

    [Header("Base Stats")]
    public float baseAttack = 10f;
    public float baseDefense = 0f;
    public float baseSpeed = 3f;
    public float baseRange = 1.5f;
    public float baseMaxHealth = 100f;
    public float baseMaxEnergy = 50f;
    public float baseEnergyRegeneration = 5f;

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