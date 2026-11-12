// ClassroomSetup.cs
//
// Part of Classroom Tools. Lives in  Assets/Editor/ClassroomTools/.
//
// Every time the project opens, it checks that the project is a git repo with
// the standard Unity .gitignore, that git knows your name, and that input
// handling is set to Both. If anything is missing, it asks once and fixes it.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ClassroomTools
{
    // Runs git in the project folder (the folder that holds Assets/).
    public static class Git
    {
        public static string Root => Directory.GetCurrentDirectory();
        public static bool HasRepo => Directory.Exists(Path.Combine(Root, ".git"));

        // Commands that are safe before this project has its own .git folder.
        // Anything else would run against whatever repo sits above the project.
        static readonly string[] SafeWithoutRepo = { "--version", "init", "rev-parse --show-toplevel" };

        public static int Run(string args, out string output)
        {
            if (!HasRepo && !SafeWithoutRepo.Any(args.StartsWith))
            {
                output = "this project has no .git folder";
                return -1;
            }
            try
            {
                var psi = new ProcessStartInfo("git", args)
                {
                    WorkingDirectory = Root,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };
                using (var p = Process.Start(psi))
                {
                    var err = p.StandardError.ReadToEndAsync();
                    output = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit();
                    if (p.ExitCode != 0) output = (output + "\n" + err.Result).Trim();
                    return p.ExitCode;
                }
            }
            catch (Exception e)
            {
                output = e.Message;
                return -1;
            }
        }

        public static bool Ok(string args) => Run(args, out _) == 0;
        public static string Out(string args) => Run(args, out var o) == 0 ? o : "";

        public static bool Installed => Ok("--version");
        public static bool HasCommits => Ok("rev-parse --verify -q HEAD");
        public static bool HasChanges => Out("status --porcelain").Length > 0;
        public static bool HasIdentity => Out("config --local user.email").Length > 0;
        public static bool Ready => HasRepo && HasIdentity;

        // The message goes through a file so quotes in student text can't break the command.
        public static bool Commit(string message, string onlyPath = null)
        {
            string file = Path.Combine(Root, "Temp", "classroom-commit-message.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file, message);
            string args = "commit -q -F \"" + file + "\"" + (onlyPath == null ? "" : " -- \"" + onlyPath + "\"");
            if (Run(args, out var output) == 0) return true;
            Debug.LogWarning("[Classroom Tools] git commit failed: " + output);
            return false;
        }

        public static bool CommitAll(string message)
        {
            ClearStaleLock();
            // ponytail: git runs on the main thread, so a large project can stall the
            // editor a second or two. Move git to a background thread if students notice.
            if (Run("add -A", out var output) != 0)
            {
                Debug.LogWarning("[Classroom Tools] git add failed: " + output);
                return false;
            }
            return Commit(message);
        }

        // Classroom Tools runs git synchronously, so a lock this old was left by a killed editor.
        static void ClearStaleLock()
        {
            string lockFile = Path.Combine(Root, ".git", "index.lock");
            if (!File.Exists(lockFile) || DateTime.Now - File.GetLastWriteTime(lockFile) < TimeSpan.FromSeconds(60)) return;
            File.Delete(lockFile);
            Debug.LogWarning("[Classroom Tools] Removed a stale git lock left by a crash.");
        }
    }

    [InitializeOnLoad]
    public static class ClassroomSetup
    {
        const string RanKey = "ClassroomTools.SetupRan";
        const string ApprovedKey = "ClassroomTools.SetupApproved";
        const int InputBoth = 2;

        // In SessionState so a script reload doesn't show the setup dialog again.
        static bool Approved
        {
            get => SessionState.GetBool(ApprovedKey, false);
            set => SessionState.SetBool(ApprovedKey, value);
        }

        static ClassroomSetup()
        {
            // Runs once per editor session, not on every script compile or Play.
            if (Application.isBatchMode || DayLog.TeacherLogin || SessionState.GetBool(RanKey, false)) return;
            SessionState.SetBool(RanKey, true);
            EditorApplication.delayCall += RunInteractive;
        }

        [MenuItem("Tools/Classroom/Run Setup Again")]
        static void RunAgain()
        {
            Approved = false;
            RunInteractive();
        }

        internal static void RunInteractive()
        {
            if (!Git.Installed)
            {
                Tell("Git isn't installed on this computer, so your work can't be saved to history. Tell your teacher.");
                DayLog.StartDayIfNeeded();
                return;
            }
            if (!Git.HasRepo && Git.Run("rev-parse --show-toplevel", out var outer) == 0)
            {
                Tell("This project is inside another git repo:\n\n" + outer +
                     "\n\nMove the project folder somewhere else, or ask your teacher.");
                DayLog.StartDayIfNeeded();
                return;
            }

            var missing = Missing();
            if (missing.Count > 0 && !Approved)
            {
                string list = string.Join("\n", missing.Select(m => "- " + m));
                EditorUtility.DisplayDialog("Classroom Tools",
                    "This project needs some setup:\n\n" + list + "\n\nClick OK to do it now.", "OK");
                Approved = true;
            }

            if (!Git.HasRepo) Git.Ok("init -q");
            if (!Git.HasIdentity)
            {
                // Its Save comes back here.
                IdentityWindow.Open();
                return;
            }

            bool restart = ApplyAll();
            if (restart && EditorUtility.DisplayDialog("Classroom Tools",
                    "Input handling is now set to Both. Unity has to restart for that to work.\n\nRestart now?",
                    "Restart", "Later"))
            {
                DayLog.AllowNextQuit = true;
                EditorApplication.OpenProject(Git.Root);
                return;
            }
            DayLog.StartDayIfNeeded();
        }

        static List<string> Missing()
        {
            var list = new List<string>();
            if (!Git.HasRepo) list.Add("Turn this project into a git repo");
            string ignore = ReadGitignore();
            if (MergeGitignore(ignore) != ignore) list.Add("Add the standard Unity rules to .gitignore");
            if (!AssetDatabase.IsValidFolder("Assets/Editor")) list.Add("Create the Assets/Editor folder");
            if (InputHandling != InputBoth) list.Add("Set input handling to Both (Unity restarts)");
            return list;
        }

        // Everything after the repo and identity exist. Returns true when Unity needs a restart.
        static bool ApplyAll()
        {
            string ignore = ReadGitignore();
            string merged = MergeGitignore(ignore);
            if (merged != ignore) File.WriteAllText(GitignorePath, merged);
            if (!AssetDatabase.IsValidFolder("Assets/Editor")) AssetDatabase.CreateFolder("Assets", "Editor");
            if (!Git.HasCommits) Git.CommitAll("Set up project with Classroom Tools");
            else if (merged != ignore) Git.CommitAll("Add standard Unity rules to .gitignore");

            if (InputHandling == InputBoth) return false;
            SetInputBoth();
            Git.CommitAll("Set input handling to Both");
            return true;
        }

        // For the batchmode selftest: same steps, no dialogs, no restart.
        public static void SetUpNonInteractive(string name, string email)
        {
            if (!Git.HasRepo) Git.Ok("init -q");
            if (!Git.HasIdentity) SetIdentity(name, email);
            ApplyAll();
        }

        public static void SetIdentity(string name, string email)
        {
            Git.Ok("config --local user.name \"" + name.Replace("\"", "") + "\"");
            Git.Ok("config --local user.email \"" + email.Replace("\"", "") + "\"");
        }

        static void Tell(string message) => EditorUtility.DisplayDialog("Classroom Tools", message, "OK");

        // ---- Input handling ----------------------------------------------

        static SerializedProperty InputProperty(out SerializedObject settings)
        {
            settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            return settings.FindProperty("activeInputHandler");
        }

        public static int InputHandling => InputProperty(out _).intValue;

        static void SetInputBoth()
        {
            var prop = InputProperty(out var settings);
            prop.intValue = InputBoth;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // ---- .gitignore --------------------------------------------------

        public static string GitignorePath => Path.Combine(Git.Root, ".gitignore");
        static string ReadGitignore() => File.Exists(GitignorePath) ? File.ReadAllText(GitignorePath) : null;

        // Never removes a line. Adds only the standard rules that aren't already there.
        public static string MergeGitignore(string existing)
        {
            if (existing == null) return StandardGitignore;
            var have = new HashSet<string>(existing.Split('\n').Select(l => l.Trim()));
            var add = StandardGitignore.Split('\n').Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("#") && !have.Contains(l))
                .Distinct().ToList();
            if (add.Count == 0) return existing;
            string text = existing.Length == 0 || existing.EndsWith("\n") ? existing : existing + "\n";
            return text + "\n# Added by Classroom Tools\n" + string.Join("\n", add) + "\n";
        }

        // github.com/github/gitignore Unity.gitignore (CC0), fetched 2026-10-28.
        public const string StandardGitignore = @"# This .gitignore file should be placed at the root of your Unity project directory
#
# Get latest from https://github.com/github/gitignore/blob/main/Unity.gitignore
#
# Recommended: add any editor/OS/tool-specific ignore rules from the Global/ templates as needed.
# See: https://github.com/github/gitignore/tree/main/Global
#
.utmp/
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
*.log

# By default unity supports Blender asset imports, *.blend1 blender files do not need to be commited to version control.
*.blend1
*.blend1.meta

# MemoryCaptures can get excessive in size.
# They also could contain extremely sensitive data
/[Mm]emoryCaptures/

# Recordings can get excessive in size
/[Rr]ecordings/

# Uncomment this line if you wish to ignore the asset store tools plugin
# /[Aa]ssets/AssetStoreTools*

# Autogenerated Jetbrains Rider plugin
/[Aa]ssets/Plugins/Editor/JetBrains*
# Jetbrains Rider personal-layer settings
*.DotSettings.user

# Visual Studio cache directory
.vs/

# Gradle cache directory
.gradle/

# Autogenerated VS/MD/Consulo solution and project files
ExportedObj/
.consulo/
*.csproj
*.unityproj
*.sln
*.slnx
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# Unity3D generated meta files
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Unity3D generated file on crash reports
sysinfo.txt

# Mono auto generated files
mono_crash.*

# Builds
*.apk
*.aab
*.unitypackage
*.unitypackage.meta
*.app

# Crashlytics generated file
crashlytics-build.properties

# TestRunner generated files
InitTestScene*.unity*

# Addressables default ignores, before user customizations
/ServerData
/[Aa]ssets/StreamingAssets/aa*
/[Aa]ssets/AddressableAssetsData/link.xml*
/[Aa]ssets/Addressables_Temp*
# By default, Addressables content builds will generate addressables_content_state.bin
# files in platform-specific subfolders, for example:
# /Assets/AddressableAssetsData/OSX/addressables_content_state.bin
/[Aa]ssets/AddressableAssetsData/*/*.bin*

# Visual Scripting auto-generated files
/[Aa]ssets/Unity.VisualScripting.Generated/VisualScripting.Flow/UnitOptions.db
/[Aa]ssets/Unity.VisualScripting.Generated/VisualScripting.Flow/UnitOptions.db.meta
/[Aa]ssets/Unity.VisualScripting.Generated/VisualScripting.Core/Property Providers
/[Aa]ssets/Unity.VisualScripting.Generated/VisualScripting.Core/Property Providers.meta

# Auto-generated scenes by play mode tests
/[Aa]ssets/[Ii]nit[Tt]est[Ss]cene*.unity*

# UI Toolkit auto generated files
/UIElementsSchema/

# Auto-generated cache in Assets folder
/[Aa]ssets/[Ss]ceneDependencyCache*
";
    }

    // Asks for the student's name and school email once per project. Closing it
    // without saving brings it back: without a name there are no commits.
    public class IdentityWindow : EditorWindow
    {
        string studentName = "";
        string email = "";
        bool saved;

        public static void Open()
        {
            var w = CreateInstance<IdentityWindow>();
            w.titleContent = new GUIContent("Who are you?");
            w.minSize = w.maxSize = new Vector2(380, 150);
            w.ShowUtility();
        }

        void OnDestroy()
        {
            if (!saved) EditorApplication.delayCall += Open;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Git puts your name on every save. Type it once.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            studentName = EditorGUILayout.TextField("First and last name", studentName);
            email = EditorGUILayout.TextField("School email", email);
            EditorGUILayout.Space();
            bool ok = studentName.Trim().Length > 0 && email.Contains("@");
            using (new EditorGUI.DisabledScope(!ok))
            {
                if (GUILayout.Button("Save"))
                {
                    ClassroomSetup.SetIdentity(studentName.Trim(), email.Trim());
                    saved = true;
                    Close();
                    // Not called from here: it may open a modal Start Day from inside this window's GUI.
                    EditorApplication.delayCall += ClassroomSetup.RunInteractive;
                }
            }
        }
    }
}
