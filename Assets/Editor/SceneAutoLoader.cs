#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SceneAutoLoader
{
    private const string LOADING_SCENE_PATH = "Assets/Scenes/Loading.unity";
    private const string PREVIOUS_SCENE_KEY = "SceneAutoLoader.PreviousScene";

    static SceneAutoLoader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                // Salvar cena atual antes de entrar em Play Mode
                string currentScene = EditorSceneManager.GetActiveScene().path;
                
                if (!string.IsNullOrEmpty(currentScene))
                {
                    EditorPrefs.SetString(PREVIOUS_SCENE_KEY, currentScene);
                }

                // Trocar para LoadingScene
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(LOADING_SCENE_PATH);
                }
                break;

            case PlayModeStateChange.EnteredEditMode:
                // RESTAURAR cena original quando sair do Play Mode
                string previousScene = EditorPrefs.GetString(PREVIOUS_SCENE_KEY, string.Empty);
                
                if (!string.IsNullOrEmpty(previousScene))
                {
                    EditorSceneManager.OpenScene(previousScene);
                    EditorPrefs.DeleteKey(PREVIOUS_SCENE_KEY); // Limpar após restaurar
                }
                break;
        }
    }
}
#endif