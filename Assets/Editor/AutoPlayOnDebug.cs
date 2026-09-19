using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AutoPlayOnDebug
{
    static AutoPlayOnDebug()
    {
        // Hook into the Editor's frame update loop
        EditorApplication.update += CheckDebugger;
    }

    private static void CheckDebugger()
    {
        // Check if a debugger (like VS Code) just attached, and we aren't already playing
        if (System.Diagnostics.Debugger.IsAttached && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("🔌 VS Code Debugger detected! Automatically starting game...");
            EditorApplication.isPlaying = true;
        }
    }
}
