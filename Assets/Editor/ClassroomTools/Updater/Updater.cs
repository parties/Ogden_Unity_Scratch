// Updater.cs
//
// Part of Classroom Tools. Lives in  Assets/Editor/ClassroomTools/Updater/.
//
// When you open Unity it asks the class portal for the newest Classroom Tools, installs it,
// and tells you what changed. If the new version doesn't compile on your machine, this puts the
// old files back. Tools > Classroom > Check for Updates runs the check by hand.
// This file is in its own assembly and never changes with an update, so it still runs when
// the Classroom Tools assembly is broken. Do not add a reference to ClassroomTools here.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Networking;

[InitializeOnLoad]
public static class ClassroomToolsUpdater
{
    const string DefaultBaseUrl = "https://portal.ogdenlab.local/unity/";
    const string UrlFile = "UserSettings/ClassroomTools-update-url.txt";
    const string ToolDir = "Assets/Editor/ClassroomTools/";
    const string LibDir = "Library/ClassroomTools/";
    const string CheckedKey = "ClassroomTools.UpdateChecked";

    static bool checking;

    // JsonUtility only fills public fields of [Serializable] classes. "scripts" is not declared, so it is ignored.
    [Serializable] public class Manifest { public Tool tool; }
    [Serializable] public class Tool { public string version; public FileEntry[] files; public Change[] changes; }
    [Serializable] public class FileEntry { public string path; public string sha256; }
    [Serializable] public class Change { public string version; public string note; public string breaking; }

    static string Marker(string name) => LibDir + name;
    static string FirstLine(string path) => File.ReadLines(path).FirstOrDefault() ?? "";

    static string BaseUrl
    {
        get
        {
            string url = DefaultBaseUrl;
            try
            {
                string line = File.Exists(UrlFile) ? File.ReadLines(UrlFile).FirstOrDefault() : null;
                if (!string.IsNullOrWhiteSpace(line)) url = line.Trim();
            }
            catch (IOException) { }
            return url.EndsWith("/") ? url : url + "/";
        }
    }

    // The ?user= value is the Windows name DayLog sends as windowsUser. A file:// URL gets no query.
    static string Url(string rel)
    {
        string url = BaseUrl + rel;
        bool web = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        return web ? url + "?user=" + UnityWebRequest.EscapeURL(Environment.UserName) : url;
    }

    // Only files directly in classroom/ map to a path. That is what keeps classroom/Updater/... and
    // anything outside the folder from ever being written.
    public static string TargetPath(string path)
    {
        const string prefix = "classroom/";
        if (path == null || !path.StartsWith(prefix, StringComparison.Ordinal)) return null;
        string name = path.Substring(prefix.Length);
        if (name.Length == 0 || name.Contains("/") || name.Contains("\\") || name.Contains("..")) return null;
        return ToolDir + name;
    }

    public static string InstalledVersion()
    {
        try
        {
            string file = ToolDir + "DayLog.cs";
            if (!File.Exists(file)) return "0.0.0";
            Match m = Regex.Match(File.ReadAllText(file), "Version = \"(\\d+\\.\\d+\\.\\d+)\"");
            return m.Success ? m.Groups[1].Value : "0.0.0";
        }
        catch (IOException) { return "0.0.0"; }
    }

    public static bool IsNewer(string a, string b) => new System.Version(a).CompareTo(new System.Version(b)) > 0;

    public static Manifest Parse(string json) => JsonUtility.FromJson<Manifest>(json);

    static ClassroomToolsUpdater()
    {
        CompilationPipeline.assemblyCompilationFinished += OnAssembly;

        // The editor was closed or restarted between the file copy and the compile.
        if (File.Exists(Marker("pending")))
        {
            if (EditorUtility.scriptCompilationFailed) Restore();
            else Finish();
        }

        if (Application.isBatchMode)
        {
            foreach (string name in new[] { "updated", "rolledback" })
            {
                if (!File.Exists(Marker(name))) continue;
                Debug.Log("Classroom Tools: marker " + name + " " + FirstLine(Marker(name)));
                File.Delete(Marker(name));
            }
            return;
        }

        if (File.Exists(Marker("updated")))
        {
            string[] lines = File.ReadAllLines(Marker("updated"));
            File.Delete(Marker("updated"));
            string v = lines.Length > 0 ? lines[0] : "";
            string notes = string.Join("\n\n", lines.Skip(1).Where(l => l.Length > 0));
            string body = "Classroom Tools is now " + v + "." + (notes.Length > 0 ? "\n\n" + notes : "");
            EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Classroom Tools updated", body, "OK");
        }
        if (File.Exists(Marker("rolledback")))
        {
            string v = FirstLine(Marker("rolledback"));
            File.Delete(Marker("rolledback"));
            string body = "Classroom Tools " + v + " did not compile on this machine, so " + InstalledVersion() + " was put back. Tell your teacher.";
            EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Classroom Tools update failed", body, "OK");
        }

        // Once per editor session, not on every script reload.
        if (!SessionState.GetBool(CheckedKey, false))
        {
            SessionState.SetBool(CheckedKey, true);
            EditorApplication.delayCall += () => CheckNow(quiet: true);
        }
    }

    // 1990 puts it just above the grayed Version item at 2000, under one separator.
    [MenuItem("Tools/Classroom/Check for Updates", false, 1990)]
    static void MenuCheck() => CheckNow(quiet: false);

    public static void CheckNow(bool quiet)
    {
        if (checking || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        checking = true;

        UnityWebRequest req;
        try
        {
            req = UnityWebRequest.Get(Url("manifest.json"));
            req.timeout = 5;
            req.SendWebRequest();
        }
        catch (Exception e)
        {
            checking = false;
            Skipped(e.Message, quiet);
            return;
        }

        // A -quit batchmode run exits before EditorApplication.update ticks, so it waits here instead.
        if (Application.isBatchMode)
        {
            Wait(req);
            Finished(req, quiet);
            return;
        }

        // The editor never waits on the portal: off campus the name does not resolve and a blocking
        // wait would freeze every editor open. Poll from update; give up after 10 s.
        double start = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction poll = null;
        poll = () =>
        {
            if (!req.isDone && EditorApplication.timeSinceStartup - start < 10) return;
            EditorApplication.update -= poll;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                checking = false;
                req.Dispose();
                return;
            }
            Finished(req, quiet);
        };
        EditorApplication.update += poll;
    }

    static void Finished(UnityWebRequest req, bool quiet)
    {
        try
        {
            if (!req.isDone)
            {
                req.Abort();
                Skipped("no answer in 10 seconds", quiet);
                return;
            }
            if (req.result != UnityWebRequest.Result.Success)
            {
                Skipped(req.error, quiet);
                return;
            }
            Apply(req.downloadHandler.text, quiet);
        }
        catch (Exception e) { Skipped(e.Message, quiet); }
        finally
        {
            checking = false;
            req.Dispose();
        }
    }

    static void Skipped(string error, bool quiet)
    {
        Debug.Log("Classroom Tools: update check skipped (" + error + ")");
        if (!quiet && !Application.isBatchMode) EditorUtility.DisplayDialog("Check for Updates", "Could not reach the portal.\n" + error, "OK");
    }

    static void Apply(string json, bool quiet)
    {
        Tool tool = Parse(json)?.tool;
        if (tool == null || string.IsNullOrEmpty(tool.version)) throw new Exception("no tool in manifest");

        string installed = InstalledVersion();
        if (!IsNewer(tool.version, installed))
        {
            string msg = "Classroom Tools " + installed + " is up to date.";
            Debug.Log(msg);
            if (!quiet && !Application.isBatchMode) EditorUtility.DisplayDialog("Check for Updates", msg, "OK");
            return;
        }

        // Download and verify everything first. Nothing under Assets/ changes until all of it is good.
        string newDir = LibDir + "new/", prevDir = LibDir + "prev/";
        if (Directory.Exists(newDir)) Directory.Delete(newDir, true);
        Directory.CreateDirectory(newDir);
        string failed = null;
        if (tool.files == null || tool.files.Length == 0) failed = "no files in manifest";
        else
        {
            // ponytail: these downloads block the editor. They run only when a newer version exists.
            // Chain them through the same update handler if labs see pauses.
            foreach (FileEntry f in tool.files)
            {
                string target = TargetPath(f.path);
                if (target == null) { failed = "bad path " + f.path; break; }
                using (UnityWebRequest r = UnityWebRequest.Get(Url("files/" + f.path)))
                {
                    r.timeout = 5;
                    r.SendWebRequest();
                    if (!Wait(r)) { r.Abort(); failed = "no answer in 10 seconds for " + f.path; break; }
                    if (r.result != UnityWebRequest.Result.Success) { failed = r.error; break; }
                    byte[] data = r.downloadHandler.data;
                    if (Sha256(data) != f.sha256) { failed = "checksum mismatch for " + f.path; break; }
                    File.WriteAllBytes(newDir + Path.GetFileName(target), data);
                }
            }
        }
        if (failed != null)
        {
            Debug.Log("Classroom Tools: update to " + tool.version + " skipped (" + failed + ")");
            Directory.Delete(newDir, true);
            return;
        }

        // Keep the files being replaced, record what is happening, then copy the new ones in.
        if (Directory.Exists(prevDir)) Directory.Delete(prevDir, true);
        Directory.CreateDirectory(prevDir);
        foreach (string f in Directory.GetFiles(ToolDir))
        {
            string ext = Path.GetExtension(f);
            if (ext == ".cs" || ext == ".asmdef") File.Copy(f, prevDir + Path.GetFileName(f), true);
        }
        var pending = new StringBuilder(tool.version);
        IEnumerable<Change> newer = (tool.changes ?? new Change[0]).Where(c => IsNewer(c.version, installed)).OrderByDescending(c => new System.Version(c.version));
        foreach (Change c in newer)
        {
            pending.Append("\nNew in ").Append(c.version).Append(": ").Append(c.note);
            if (!string.IsNullOrEmpty(c.breaking)) pending.Append("\nYou need to: ").Append(c.breaking);
        }
        File.WriteAllText(Marker("pending"), pending.ToString());
        foreach (string f in Directory.GetFiles(newDir)) File.Copy(f, ToolDir + Path.GetFileName(f), true);
        AssetDatabase.Refresh();
    }

    // A compile failure does not reload the domain, so this handler is still alive to restore the files.
    // A success reloads the domain and the constructor above finishes the job.
    static void OnAssembly(string path, CompilerMessage[] messages)
    {
        if (!File.Exists(Marker("pending")) || Path.GetFileName(path) != "ClassroomTools.dll") return;
        if (messages.Any(m => m.type == CompilerMessageType.Error)) Restore();
        else Finish();
    }

    static void Restore()
    {
        string failedVersion = FirstLine(Marker("pending"));
        string newDir = LibDir + "new/", prevDir = LibDir + "prev/";
        if (Directory.Exists(newDir))
        {
            // A file the update added has no old copy: delete it and its .meta.
            foreach (string f in Directory.GetFiles(newDir))
            {
                string name = Path.GetFileName(f);
                if (File.Exists(prevDir + name)) continue;
                File.Delete(ToolDir + name);
                File.Delete(ToolDir + name + ".meta");
            }
        }
        if (Directory.Exists(prevDir))
        {
            foreach (string f in Directory.GetFiles(prevDir)) File.Copy(f, ToolDir + Path.GetFileName(f), true);
        }
        File.Delete(Marker("rolledback"));
        File.Move(Marker("pending"), Marker("rolledback"));
        if (Directory.Exists(newDir)) Directory.Delete(newDir, true);
        if (Directory.Exists(prevDir)) Directory.Delete(prevDir, true);
        Debug.Log("Classroom Tools: " + failedVersion + " failed to compile, restored " + InstalledVersion());
        EditorApplication.delayCall += () => AssetDatabase.Refresh();
    }

    static void Finish()
    {
        string v = FirstLine(Marker("pending"));
        File.Delete(Marker("updated"));
        File.Move(Marker("pending"), Marker("updated"));
        if (Directory.Exists(LibDir + "new/")) Directory.Delete(LibDir + "new/", true);
        if (Directory.Exists(LibDir + "prev/")) Directory.Delete(LibDir + "prev/", true);
        Debug.Log("Classroom Tools: updated to " + v);
    }

    // Same 10 s ceiling as DayLog.Request (5 s timeout, then give up). Copied, not shared:
    // the two assemblies cannot see each other.
    static bool Wait(UnityWebRequest req)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!req.isDone && clock.Elapsed.TotalSeconds < 10) Thread.Sleep(10);
        return req.isDone;
    }

    static string Sha256(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
    }
}
