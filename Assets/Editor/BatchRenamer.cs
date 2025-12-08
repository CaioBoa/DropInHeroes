using UnityEngine;
using UnityEditor;
using System.IO;

public class BatchRenamer : EditorWindow
{
    private string findText = "";
    private string replaceText = "";
    private string prefix = "";
    private string suffix = "";
    private bool addNumbers = false;
    private int startNumber = 0;

    [MenuItem("Tools/Batch Renamer")]
    public static void ShowWindow()
    {
        GetWindow<BatchRenamer>("Batch Renamer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Rename Tool", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Selecionados: {Selection.objects.Length} arquivos", MessageType.Info);
        
        EditorGUILayout.Space();
        GUILayout.Label("Find and Replace", EditorStyles.boldLabel);
        findText = EditorGUILayout.TextField("Encontrar:", findText);
        replaceText = EditorGUILayout.TextField("Substituir por:", replaceText);
        
        if (GUILayout.Button("Replace"))
        {
            FindAndReplace();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.Label("Prefix/Suffix", EditorStyles.boldLabel);
        prefix = EditorGUILayout.TextField("Prefixo:", prefix);
        suffix = EditorGUILayout.TextField("Sufixo:", suffix);
        
        if (GUILayout.Button("Add Prefix/Suffix"))
        {
            AddPrefixSuffix();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.Label("Numbering", EditorStyles.boldLabel);
        addNumbers = EditorGUILayout.Toggle("Adicionar Números", addNumbers);
        startNumber = EditorGUILayout.IntField("Começar em:", startNumber);
        
        if (GUILayout.Button("Apply Numbers"))
        {
            ApplyNumbering();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Remove Numbers (at end)"))
        {
            RemoveTrailingNumbers();
        }
    }

    private void FindAndReplace()
    {
        if (string.IsNullOrEmpty(findText))
        {
            EditorUtility.DisplayDialog("Erro", "Campo 'Encontrar' não pode estar vazio", "OK");
            return;
        }
        
        int count = 0;
        
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;
            
            string oldName = Path.GetFileNameWithoutExtension(path);
            
            if (oldName.Contains(findText))
            {
                string newName = oldName.Replace(findText, replaceText);
                string result = AssetDatabase.RenameAsset(path, newName);
                
                if (string.IsNullOrEmpty(result))
                {
                    count++;
                }
                else
                {
                    Debug.LogError($"Erro ao renomear {oldName}: {result}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Concluído", $"{count} arquivos renomeados", "OK");
    }

    private void AddPrefixSuffix()
    {
        if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
        {
            EditorUtility.DisplayDialog("Erro", "Adicione um prefixo ou sufixo", "OK");
            return;
        }
        
        int count = 0;
        
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;
            
            string oldName = Path.GetFileNameWithoutExtension(path);
            string newName = prefix + oldName + suffix;
            
            string result = AssetDatabase.RenameAsset(path, newName);
            
            if (string.IsNullOrEmpty(result))
            {
                count++;
            }
            else
            {
                Debug.LogError($"Erro ao renomear {oldName}: {result}");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Concluído", $"{count} arquivos renomeados", "OK");
    }

    private void ApplyNumbering()
    {
        int count = 0;
        int currentNumber = startNumber;
        
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;
            
            string oldName = Path.GetFileNameWithoutExtension(path);
            string newName = $"{oldName}_{currentNumber:D2}"; // D2 = 2 dígitos (01, 02, 03...)
            
            string result = AssetDatabase.RenameAsset(path, newName);
            
            if (string.IsNullOrEmpty(result))
            {
                count++;
                currentNumber++;
            }
            else
            {
                Debug.LogError($"Erro ao renomear {oldName}: {result}");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Concluído", $"{count} arquivos renomeados", "OK");
    }

    private void RemoveTrailingNumbers()
    {
        int count = 0;
        
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;
            
            string oldName = Path.GetFileNameWithoutExtension(path);
            
            // Remove números e underscore no final (ex: "sprite_01" -> "sprite")
            string newName = System.Text.RegularExpressions.Regex.Replace(oldName, @"_\d+$", "");
            
            if (newName != oldName)
            {
                string result = AssetDatabase.RenameAsset(path, newName);
                
                if (string.IsNullOrEmpty(result))
                {
                    count++;
                }
                else
                {
                    Debug.LogError($"Erro ao renomear {oldName}: {result}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Concluído", $"{count} arquivos renomeados", "OK");
    }
}