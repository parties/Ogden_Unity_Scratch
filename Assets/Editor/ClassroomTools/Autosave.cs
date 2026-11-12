// Autosave.cs
//
// Part of Classroom Tools. Lives in  Assets/Editor/ClassroomTools/.
//
// Every time you press Play, it saves your open scenes and assets first,
// so a crash or a frozen game can't eat your work.
// Same as the portal's standalone Autosave.cs, but in the ClassroomTools
// namespace so a project with both still compiles (it just saves twice).

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClassroomTools
{
    [InitializeOnLoad]
    public static class Autosave
    {
        static Autosave()
        {
            EditorApplication.playModeStateChanged += SaveOnPlay;
        }

        public static void SaveOnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Auto-saved scenes and assets before entering Play Mode.");
        }
    }
}
