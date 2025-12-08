using UnityEngine;

[System.Serializable]
public class EnemyData
{
    public string characterId;
    public Vector3 spawnPosition;
    public int level;
    
}

[CreateAssetMenu(fileName = "NewBattle", menuName = "Game/Data/Battle")]
public class BattleData : ScriptableObject, IGameData
{
    [Header("Metadata")]
    [SerializeField] private string id;
    [SerializeField] private DataCategory category = DataCategory.Battle;
    
    [Header("Dialogue Content")]
    public EnemyData[] enemies;
    
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