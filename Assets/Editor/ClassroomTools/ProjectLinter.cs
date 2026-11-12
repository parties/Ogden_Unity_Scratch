// ProjectLinter.cs
//
// Part of Classroom Tools. Lives in  Assets/Editor/ClassroomTools/.
//
// Tools > Classroom > Project Linter lists files that are outside their folder
// (scenes outside Scenes/, scripts outside Scripts/, and so on) and moves them
// for you. Unity's own move keeps every link in your scenes and prefabs working.
// Imported packs (a folder with a License.txt) go whole into Packs/, where the
// class portal puts its packs too. Everything inside Packs/ is left as it is.
// Each tile palette gets its own folder in Palettes/, with the tiles it uses.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ClassroomTools
{
    public class Problem
    {
        public string Category;
        public string Path;    // what is wrong
        public string Target;  // where it goes
    }

    public static class ProjectLinter
    {
        public const string FoldersCategory = "Missing folders";
        const string EditorCategory = "Editor scripts";
        const string PalettesCategory = "Palettes";
        const string TilesCategory = "Tiles";
        public const string UnusedTilesCategory = "Unused tiles";
        const string PacksCategory = "Packs";
        const string PalettesHome = "Assets/Palettes";
        const string PacksHome = "Assets/Packs";
        // ponytail: the first Classroom Tools put packs in ThirdParty/. Drop this once no project has one.
        const string OldPacksHome = "Assets/ThirdParty";

        static readonly (string category, string home, string[] extensions)[] Rules =
        {
            ("Scenes", "Assets/Scenes", new[] { ".unity" }),
            ("Scripts", "Assets/Scripts", new[] { ".cs" }),
            ("Prefabs", "Assets/Prefabs", new[] { ".prefab" }),
            ("Materials", "Assets/Materials", new[] { ".mat" }),
            ("Sprites", "Assets/Sprites", new[] { ".png", ".jpg", ".jpeg", ".psd" }),
            ("Audio", "Assets/Audio", new[] { ".wav", ".mp3", ".ogg", ".aif", ".aiff" }),
            ("Animations", "Assets/Animations", new[] { ".anim", ".controller", ".overridecontroller" }),
        };

        // Unity gives these folder names special meaning at any depth. Never touched.
        static readonly HashSet<string> SpecialFolders = new HashSet<string> { "Editor", "Plugins", "Resources", "StreamingAssets", "Gizmos" };

        // Top-level folders that belong to Unity or to someone else. Never scanned.
        static readonly HashSet<string> HandsOff = new HashSet<string> { "Packs", "Settings", "TextMesh Pro" };

        public static IEnumerable<string> CategoryOrder =>
            new[] { FoldersCategory }.Concat(Rules.Select(r => r.category)).Concat(new[] { EditorCategory, PalettesCategory, TilesCategory, UnusedTilesCategory, PacksCategory });

        public static List<Problem> Scan()
        {
            var problems = new List<Problem>();
            foreach (string home in Rules.Select(r => r.home).Append(PalettesHome))
                if (!AssetDatabase.IsValidFolder(home))
                    problems.Add(new Problem { Category = FoldersCategory, Path = home, Target = home });

            var packs = new HashSet<string>();
            foreach (string folder in AssetDatabase.GetSubFolders("Assets"))
            {
                string name = System.IO.Path.GetFileName(folder);
                if (IsAllowedTopFolder(name) || !IsPack(folder)) continue;
                packs.Add(name);
                problems.Add(new Problem { Category = PacksCategory, Path = folder, Target = PacksHome + "/" + name });
            }
            if (AssetDatabase.IsValidFolder(OldPacksHome))
            {
                packs.Add(System.IO.Path.GetFileName(OldPacksHome));
                foreach (string folder in AssetDatabase.GetSubFolders(OldPacksHome))
                    problems.Add(new Problem { Category = PacksCategory, Path = folder, Target = PacksHome + "/" + System.IO.Path.GetFileName(folder) });
            }

            // A palette belongs in Palettes/<name>/, and the tiles it uses go in the same folder.
            var palettes = new HashSet<string>();
            var tileHome = new Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets("t:GridPalette"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/") || !palettes.Add(path)) continue;
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                if (!folder.StartsWith(PalettesHome + "/"))
                {
                    folder = PalettesHome + "/" + name;
                    problems.Add(new Problem { Category = PalettesCategory, Path = path, Target = folder + "/" + name + ".prefab" });
                }
                foreach (string dep in AssetDatabase.GetDependencies(path, false))
                    if (IsTile(dep) && !tileHome.ContainsKey(dep)) tileHome[dep] = folder;
            }

            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/") || AssetDatabase.IsValidFolder(path)) continue;
                string[] parts = path.Split('/');
                string[] folders = parts.Skip(1).Take(parts.Length - 2).ToArray();
                if (folders.Any(SpecialFolders.Contains) || palettes.Contains(path)) continue;
                bool notOurs = folders.Length > 0 && (HandsOff.Contains(folders[0]) || packs.Contains(folders[0]));

                // Tiles are checked inside packs too: Unity saves them next to the PNGs.
                if (IsTile(path) && !path.StartsWith(PalettesHome + "/"))
                {
                    if (tileHome.TryGetValue(path, out string tileFolder))
                        problems.Add(new Problem { Category = TilesCategory, Path = path, Target = tileFolder + "/" + parts[parts.Length - 1] });
                    else if (!notOurs)
                        problems.Add(new Problem { Category = UnusedTilesCategory, Path = path, Target = null });
                    continue;
                }
                if (notOurs) continue;

                string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                var rule = Rules.FirstOrDefault(r => r.extensions.Contains(ext));
                if (rule.category == null) continue;

                string category = rule.category, home = rule.home;
                if (ext == ".cs" && IsEditorScript(path))
                {
                    category = EditorCategory;
                    home = "Assets/Editor";
                }
                if (path.StartsWith(home + "/")) continue;
                problems.Add(new Problem { Category = category, Path = path, Target = home + "/" + parts[parts.Length - 1] });
            }
            return problems;
        }

        static bool IsAllowedTopFolder(string name) =>
            SpecialFolders.Contains(name) || HandsOff.Contains(name) || Rules.Any(r => r.home == "Assets/" + name) || "Assets/" + name == PalettesHome;

        // ponytail: matched by type name so this compiles in projects without the Tilemap module.
        static bool IsTile(string path)
        {
            if (!path.EndsWith(".asset")) return false;
            for (var t = AssetDatabase.GetMainAssetTypeAtPath(path); t != null; t = t.BaseType)
                if (t.Name == "TileBase") return true;
            return false;
        }

        // Same test the class portal uses for a pack: a License.txt or Preview.png directly inside.
        public static bool IsPack(string folder) =>
            Directory.GetFiles(folder).Select(f => System.IO.Path.GetFileName(f).ToLowerInvariant())
                .Any(n => n == "license.txt" || n == "preview.png");

        // ponytail: text match, not a parse. A script that wraps its UnityEditor
        // using in #if UNITY_EDITOR is a game script that also runs in the editor.
        static bool IsEditorScript(string path)
        {
            string text = File.ReadAllText(path);
            return text.Contains("using UnityEditor") && !text.Contains("#if UNITY_EDITOR");
        }

        // Fixes one category, or everything when category is null. Unused tiles are only listed.
        // Returns the moves that failed; empty means every fix worked.
        public static List<string> Fix(string category)
        {
            var failures = new List<string>();
            // Packs go last, so tiles inside a pack move out before the pack itself moves.
            var todo = Scan().Where(p => p.Target != null && (category == null || p.Category == category))
                .OrderBy(p => p.Category == PacksCategory).ToList();
            if (todo.Count == 0) return failures;

            bool git = Git.Ready;
            if (git && Git.HasChanges) Git.CommitAll("Save work before organizing");

            // Folders first: a folder made inside the batch below isn't there for the moves yet.
            foreach (var p in todo)
                EnsureFolder(p.Category == FoldersCategory ? p.Target : System.IO.Path.GetDirectoryName(p.Target).Replace('\\', '/'));

            // One batch for every move, so Unity refreshes once instead of once per file.
            var moves = todo.Where(p => p.Category != FoldersCategory).ToList();
            var emptied = new HashSet<string>();
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    var p = moves[i];
                    EditorUtility.DisplayProgressBar("Project Linter", "Moving " + p.Path, (float)i / moves.Count);
                    // ponytail: flattens subfolders (Assets/Player/Art/run.png lands in Sprites/run.png).
                    // Keep the relative path instead if students miss their own structure.
                    string dest = AssetDatabase.GenerateUniqueAssetPath(p.Target);
                    string error = AssetDatabase.MoveAsset(p.Path, dest);
                    if (!string.IsNullOrEmpty(error)) failures.Add(p.Path + ": " + error);
                    else emptied.Add(System.IO.Path.GetDirectoryName(p.Path).Replace('\\', '/'));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            foreach (string folder in emptied.OrderByDescending(f => f.Length)) DeleteIfEmpty(folder);
            AssetDatabase.Refresh();

            if (git && Git.HasChanges) Git.CommitAll("Organize project: " + (category ?? "all"));
            return failures;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        // Removes a folder a fix left empty, then its parent if that is empty too.
        // Stops at Assets/ and at any folder the rules expect to exist.
        static void DeleteIfEmpty(string folder)
        {
            while (folder != "Assets" && folder.StartsWith("Assets/") && AssetDatabase.IsValidFolder(folder))
            {
                string[] parts = folder.Split('/');
                if (parts.Length == 2 && IsAllowedTopFolder(parts[1])) return;
                if (Directory.GetFileSystemEntries(folder).Any(e => !e.EndsWith(".meta"))) return;
                AssetDatabase.DeleteAsset(folder);
                folder = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            }
        }
    }

    public class LinterWindow : EditorWindow
    {
        List<Problem> problems = new List<Problem>();
        List<string> failures = new List<string>();
        Vector2 scroll;

        [MenuItem("Tools/Classroom/Project Linter")]
        public static void Open() => GetWindow<LinterWindow>("Project Linter");

        void OnEnable()
        {
            EditorApplication.projectChanged += Rescan;
            Rescan();
        }

        void OnDisable() => EditorApplication.projectChanged -= Rescan;
        void OnFocus() => Rescan();

        void Rescan()
        {
            problems = ProjectLinter.Scan();
            Repaint();
        }

        void Run(string category)
        {
            failures = ProjectLinter.Fix(category);
            Rescan();
        }

        void OnGUI()
        {
            foreach (string f in failures) EditorGUILayout.HelpBox("Couldn't move " + f, MessageType.Warning);

            int fixable = problems.Count(p => p.Target != null);
            if (fixable == 0) EditorGUILayout.HelpBox("Project is organized.", MessageType.Info);
            else if (GUILayout.Button("Fix All (" + fixable + ")", GUILayout.Height(28))) { Run(null); return; }
            if (problems.Count == 0) return;

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (string category in ProjectLinter.CategoryOrder)
            {
                var list = problems.Where(p => p.Category == category).ToList();
                if (list.Count == 0) continue;

                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(category + " (" + list.Count + ")", EditorStyles.boldLabel);
                    if (category != ProjectLinter.UnusedTilesCategory && GUILayout.Button("Fix " + category, GUILayout.Width(160))) { Run(category); GUIUtility.ExitGUI(); }
                }
                foreach (var p in list)
                {
                    string label = p.Category == ProjectLinter.FoldersCategory ? "Create " + p.Target
                        : p.Target == null ? p.Path + "  (no palette uses it; delete it if you don't need it)"
                        : p.Path + "  ->  " + p.Target;
                    if (GUILayout.Button(label, EditorStyles.label))
                        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(p.Path));
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
