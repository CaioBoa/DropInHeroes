using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using DropInHeroes.Core;
using DropInHeroes.Utils;

namespace DropInHeroes.Editor
{

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

            RenameSelected(oldName => oldName.Contains(findText) ? oldName.Replace(findText, replaceText) : oldName);
        }

        private void AddPrefixSuffix()
        {
            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
            {
                EditorUtility.DisplayDialog("Erro", "Adicione um prefixo ou sufixo", "OK");
                return;
            }

            RenameSelected(oldName => prefix + oldName + suffix);
        }

        private void ApplyNumbering()
        {
            int n = startNumber;
            RenameSelected(oldName => $"{oldName}_{n++:D2}"); // D2 = 2 dígitos (01, 02, 03...)
        }

        private void RemoveTrailingNumbers()
        {
            // Remove números e underscore no final (ex: "sprite_01" -> "sprite")
            RenameSelected(oldName => System.Text.RegularExpressions.Regex.Replace(oldName, @"_\d+$", ""));
        }

        /// <summary>
        /// Renomeia cada asset selecionado aplicando 'transform' ao nome (sem extensão). Pula o asset
        /// quando o transform retorna nulo/vazio ou o mesmo nome. Envolve em StartAssetEditing para
        /// evitar reimportações intermediárias. Centraliza o boilerplate das quatro ações.
        /// </summary>
        private static void RenameSelected(Func<string, string> transform)
        {
            int count = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (UnityEngine.Object obj in Selection.objects)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path)) continue;

                    string oldName = Path.GetFileNameWithoutExtension(path);
                    string newName = transform(oldName);
                    if (string.IsNullOrEmpty(newName) || newName == oldName) continue;

                    string error = AssetDatabase.RenameAsset(path, newName);
                    if (string.IsNullOrEmpty(error)) count++;
                    else Debug.LogError($"Erro ao renomear {oldName}: {error}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Concluído", $"{count} arquivos renomeados", "OK");
        }
    }
}