using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class Autosave
{
    static Autosave()
    {
        EditorApplication.playModeStateChanged += SaveOnPlay;
    }

    private static void SaveOnPlay(PlayModeStateChange state)
    {
        // Check if we are currently exiting Edit Mode to enter Play Mode
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            UnityEngine.Debug.Log("Autosaving scenes and assets before Play Mode...");
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
