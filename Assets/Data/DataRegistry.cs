using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Registry genérico para qualquer tipo de dado
/// Fornece indexação automática e busca O(1)
/// </summary>
[System.Serializable]
public class DataRegistry<T> where T : ScriptableObject, IGameData
{
    [SerializeField] private List<T> allData = new List<T>();
    
    private Dictionary<string, T> idIndex;
    private Dictionary<DataCategory, List<T>> categoryIndex;

    public void Initialize()
    {
        Debug.Log($"[DataRegistry] Inicializando registro de {typeof(T).Name} com {allData.Count} itens");
        BuildIndices();
    }

    private void BuildIndices()
    {
        // Índice por ID
        idIndex = new Dictionary<string, T>();
        
        // Índice por categoria
        categoryIndex = new Dictionary<DataCategory, List<T>>();
        
        foreach (var data in allData)
        {
            if (data == null) continue;
            
            // Adicionar ao índice de ID
            if (!string.IsNullOrEmpty(data.ID))
            {
                if (idIndex.ContainsKey(data.ID))
                {
                    Debug.LogError($"[DataRegistry] ID duplicado encontrado: '{data.ID}' em {typeof(T).Name}");
                }
                else
                {
                    idIndex.Add(data.ID, data);
                }
            }
            
            // Adicionar ao índice de categoria
            if (!categoryIndex.ContainsKey(data.Category))
            {
                categoryIndex[data.Category] = new List<T>();
            }
            categoryIndex[data.Category].Add(data);
        }
    }

    // Busca por ID - O(1)
    public T GetByID(string id)
    {
        if (idIndex.TryGetValue(id, out T data))
        {
            return data;
        }
        
        Debug.LogWarning($"[DataRegistry] {typeof(T).Name} com ID '{id}' não encontrado");
        return null;
    }

    // Busca por categoria - O(1)
    public List<T> GetByCategory(DataCategory category)
    {
        if (categoryIndex.TryGetValue(category, out List<T> dataList))
        {
            return new List<T>(dataList);
        }
        
        return new List<T>();
    }

    // Busca por predicado (query customizada)
    public List<T> Query(System.Func<T, bool> predicate)
    {
        return allData.Where(predicate).ToList();
    }

    // Obter todos
    public List<T> GetAll()
    {
        return new List<T>(allData);
    }

    // Verificar existência
    public bool Contains(string id)
    {
        return idIndex.ContainsKey(id);
    }

    // Contar itens
    public int Count => allData.Count;

#if UNITY_EDITOR
    // Adicionar data (Editor only)
    public void AddData(T data)
    {
        if (!allData.Contains(data))
        {
            allData.Add(data);
        }
    }

    // Remover data (Editor only)
    public void RemoveData(T data)
    {
        allData.Remove(data);
    }

    // Auto-scan no projeto (Editor only)
    public void AutoScanProject()
    {
        allData.Clear();
        
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            T data = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            
            if (data != null)
            {
                allData.Add(data);
            }
        }
        
        Debug.Log($"[DataRegistry] Auto-scan encontrou {allData.Count} {typeof(T).Name}");
    }

    // Validar integridade
    public List<string> ValidateIntegrity()
    {
        List<string> errors = new List<string>();
        HashSet<string> seenIDs = new HashSet<string>();
        
        foreach (var data in allData)
        {
            if (data == null)
            {
                errors.Add("Referência nula encontrada");
                continue;
            }
            
            // Validar ID
            if (string.IsNullOrEmpty(data.ID))
            {
                errors.Add($"{data.name}: ID vazio");
            }
            else if (seenIDs.Contains(data.ID))
            {
                errors.Add($"{data.name}: ID duplicado '{data.ID}'");
            }
            else
            {
                seenIDs.Add(data.ID);
            }
        }
        
        return errors;
    }
#endif
}