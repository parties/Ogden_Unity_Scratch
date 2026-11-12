// DayLog.cs
//
// Part of Classroom Tools. Lives in  Assets/Editor/ClassroomTools/.
//
// Start Day asks for your goal and End Day asks what you got done. Both write
// to DEVLOG.md (next to the Assets folder) and save a git commit. Unity won't
// close until you run End Day, and your work is committed every 10 minutes.
// Each day's Goal and Done lines and a summary of that day's commits go to EduDash.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassroomTools
{
    [InitializeOnLoad]
    public static class DayLog
    {
        // Bump with every package rebuild, and the portal.json title with it. Major: a student
        // has to do something new. Minor: a new feature they don't have to act on. Patch: a fix.
        public const string Version = "4.0.0";

        public const int MinLength = 10;
        public const int QuitWindowMinutes = 10;
        public const int SnapshotMinutes = 10;
        const string GoalTag = "**Goal:** ";
        const string DoneTag = "**Done:** ";
        const string LastSnapshotKey = "ClassroomTools.LastSnapshot";

        // The teacher's logins skip setup, Start Day, the quit check and snapshots.
        public static readonly string[] TeacherUsers = { "Eric.Ogden", "eric" };
        public static bool IsTeacher(string user) => Array.Exists(TeacherUsers, t => string.Equals(t, user, StringComparison.OrdinalIgnoreCase));
        public static bool TeacherLogin => IsTeacher(Environment.UserName);

        // Set just before a restart Classroom Tools asks for itself.
        public static bool AllowNextQuit;

        static DayLog()
        {
            EditorApplication.wantsToQuit += OnWantsToQuit;
            if (!Application.isBatchMode && !TeacherLogin) EditorApplication.update += SnapshotTick;
        }

        public static string LogPath => Path.Combine(Git.Root, "DEVLOG.md");
        static string ReadLog() => File.Exists(LogPath) ? File.ReadAllText(LogPath) : "";

        [MenuItem("Tools/Classroom/Start Day")]
        public static void StartDay() => EntryWindow.Open(false, false);

        [MenuItem("Tools/Classroom/End Day")]
        public static void EndDay() => EntryWindow.Open(true, false);

        [MenuItem("Tools/Classroom/Connect to EduDash")]
        static void ConnectToEduDash() => ConnectWindow.Open();

        [MenuItem("Tools/Classroom/Connect to EduDash", true)]
        static bool CanConnectToEduDash() => !TeacherLogin;

        // Grayed out: it's there to read, at the bottom of the menu.
        [MenuItem("Tools/Classroom/Version " + Version, false, 2000)]
        static void ShowVersion() { }

        [MenuItem("Tools/Classroom/Version " + Version, true)]
        static bool CanShowVersion() => false;

        public static void StartDayIfNeeded()
        {
            if (!TeacherLogin && !HasToday(ReadLog(), DateTime.Now)) StartDay();
        }

        internal static bool GoalSavedToday() => TodaySection(ReadLog(), DateTime.Now)?.Contains(GoalTag) == true;

        static bool OnWantsToQuit()
        {
            if (Application.isBatchMode) return true;
            try
            {
                if (TeacherLogin) return true;
                if (AllowNextQuit)
                {
                    AllowNextQuit = false;
                    return true;
                }
                if (CanQuit(ReadLog(), DateTime.Now)) return true;
                if (EditorUtility.DisplayDialog("End your day?",
                        "Run End Day before you close Unity. It saves your work and what you got done.",
                        "End Day", "Keep working"))
                    EntryWindow.Open(true, true);
                return false;
            }
            catch (Exception e)
            {
                // Lab machines block Task Manager, so a broken check must never trap a student.
                NoteEndDayFailure(e);
                return true;
            }
        }

        // ---- Snapshots ---------------------------------------------------

        static void SnapshotTick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            double now = EditorApplication.timeSinceStartup;
            if (now - SessionState.GetFloat(LastSnapshotKey, 0f) < SnapshotMinutes * 60) return;
            SessionState.SetFloat(LastSnapshotKey, (float)now);
            if (!Snapshot(DateTime.Now)) return;
            MarkPending(DateTime.Now);
            SendPending();
        }

        // Saves scenes that already have a file (never a Save As prompt) and commits any changes.
        public static bool Snapshot(DateTime now)
        {
            // The compile check lives in SnapshotTick: a script the student just saved reads as compiling here.
            if (!Git.Ready || EditorApplication.isPlayingOrWillChangePlaymode) return false;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.isDirty && !string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            if (!Git.HasChanges) return false;
            return Git.CommitAll("Snapshot " + now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        }

        // ---- Saving (no UI, so the selftest can call these) ---------------

        public static void SaveGoal(string goal)
        {
            DateTime now = DateTime.Now;
            File.WriteAllText(LogPath, AppendGoal(ReadLog(), now, goal));
            if (!CanCommit()) return;
            Git.Ok("add DEVLOG.md");
            Git.Commit("Start day " + Date(now) + ": " + OneLine(goal), "DEVLOG.md");
        }

        public static void SaveDone(string done)
        {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            DateTime now = DateTime.Now;
            File.WriteAllText(LogPath, AppendDone(ReadLog(), now, done));
            if (CanCommit()) Git.CommitAll("End day " + Date(now) + ": " + OneLine(done));
        }

        public static bool TrySaveDone(string done)
        {
            try
            {
                SaveDone(done);
                return true;
            }
            catch (Exception e)
            {
                NoteEndDayFailure(e);
                return false;
            }
        }

        static void NoteEndDayFailure(Exception e)
        {
            Debug.LogError("[Classroom Tools] End Day failed: " + e);
            try
            {
                MarkPending(DateTime.Now);
                DateTime now = DateTime.Now;
                File.WriteAllText(LogPath, Append(ReadLog(), now, "**End Day failed:** " + e.GetType().Name + " (" + Clock(now) + ")"));
            }
            catch (Exception)
            {
                // DEVLOG.md itself may be what's broken; the Console still has the error.
            }
        }

        static bool CanCommit()
        {
            if (Git.Ready) return true;
            Debug.LogWarning("[Classroom Tools] Saved to DEVLOG.md, but git isn't set up, so nothing was committed. Run Tools > Classroom > Run Setup Again.");
            return false;
        }

        // ---- DEVLOG.md text ----------------------------------------------

        public static string Heading(DateTime day) => "## " + day.ToString("yyyy-MM-dd (ddd)", CultureInfo.InvariantCulture);
        static string Date(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        static string Clock(DateTime t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);
        static string OneLine(string text) => Regex.Replace(text.Trim(), @"\s*[\r\n]+\s*", " ");

        // Today's section runs from today's heading to the next heading.
        static string TodaySection(string log, DateTime now)
        {
            log = log.Replace("\r\n", "\n");
            string heading = Heading(now) + "\n";
            int start = log.IndexOf(heading, StringComparison.Ordinal);
            if (start < 0) return null;
            int next = log.IndexOf("\n## ", start + heading.Length - 1, StringComparison.Ordinal);
            return next < 0 ? log.Substring(start) : log.Substring(start, next - start);
        }

        public static bool HasToday(string log, DateTime now) => TodaySection(log ?? "", now) != null;

        public static bool HasGoalWithoutDone(string log, DateTime now)
        {
            string today = TodaySection(log ?? "", now);
            return today != null && today.Contains(GoalTag) && !today.Contains(DoneTag);
        }

        public static string AppendGoal(string log, DateTime now, string goal) => Append(log, now, GoalTag + OneLine(goal) + " (" + Clock(now) + ")");

        public static string AppendDone(string log, DateTime now, string done)
        {
            DateTime? goal = LastTime(log, now, GoalTag);
            int minutes = goal.HasValue ? (int)(now - goal.Value).TotalMinutes : -1;
            string stamp = Clock(now) + (minutes >= 0 ? ", " + minutes + " min after Start Day" : "");
            return Append(log, now, DoneTag + OneLine(done) + " (" + stamp + ")");
        }

        // True only within QuitWindowMinutes of today's last End Day, so an End Day
        // at the start of class doesn't unlock the rest of the period.
        public static bool CanQuit(string log, DateTime now)
        {
            DateTime? done = LastTime(log, now, DoneTag);
            if (!done.HasValue) return false;
            double minutes = (now - done.Value).TotalMinutes;
            return minutes >= 0 && minutes <= QuitWindowMinutes;
        }

        static readonly Regex TimeTag = new Regex(@"\((\d{2}):(\d{2})(, (\d+) min after Start Day)?\)$");

        // Time on today's last line that starts with tag; lines from the old package have none.
        static DateTime? LastTime(string log, DateTime now, string tag)
        {
            string today = TodaySection(log ?? "", now);
            if (today == null) return null;
            DateTime? last = null;
            foreach (string line in today.Split('\n'))
            {
                if (!line.StartsWith(tag, StringComparison.Ordinal)) continue;
                Match m = TimeTag.Match(line);
                if (m.Success) last = now.Date.AddHours(int.Parse(m.Groups[1].Value)).AddMinutes(int.Parse(m.Groups[2].Value));
            }
            return last;
        }

        // Today is always the last section, so new lines go at the end.
        static string Append(string log, DateTime now, string line)
        {
            log = (log ?? "").Replace("\r\n", "\n");
            if (log.Length == 0) log = "# Dev log\n";
            if (!log.EndsWith("\n")) log += "\n";
            if (!HasToday(log, now)) log += "\n" + Heading(now) + "\n";
            return log + line + "\n";
        }

        // ---- Day summary from git ----------------------------------------

        public class Summary
        {
            public int csLines, assetsAdded, sceneObjectsAdded, sceneObjectsRemoved;
            public List<string> files = new List<string>(), assets = new List<string>(),
                sceneObjects = new List<string>(), csAdded = new List<string>();
        }

        // The day's commits, from the oldest one's parent to the newest. A root
        // commit stands as its own start, so the setup commit's template never counts.
        public static bool DayRange(DateTime day, out string from, out string to)
        {
            from = to = null;
            string d = Date(day);
            string[] commits = Git.Out("log --format=%H --since=\"" + d + " 00:00:00\" --until=\"" + d + " 23:59:59\"")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (commits.Length == 0) return false;
            to = commits[0].Trim();
            string oldest = commits[commits.Length - 1].Trim();
            string parent = Git.Out("rev-parse --verify -q " + oldest + "^");
            from = parent.Length > 0 ? parent : oldest;
            return true;
        }

        // Classroom Tools installs into Assets/Editor/ClassroomTools/, so its own C# never counts as the student's.
        static bool Counted(string path) =>
            !path.EndsWith(".meta", StringComparison.Ordinal) && path != "DEVLOG.md" &&
            !path.StartsWith("ProjectSettings/", StringComparison.Ordinal) && !path.StartsWith("Library/", StringComparison.Ordinal) &&
            !path.StartsWith("Assets/Editor/ClassroomTools/", StringComparison.Ordinal);

        static string GitDiff(string args) => Git.Out("-c core.quotepath=off diff --no-renames " + args).Replace("\r", "");

        // Added lines of the counted files in a -U0 diff, each as its sign and text.
        static IEnumerable<string> ChangedLines(string diff)
        {
            bool counted = false;
            foreach (string line in diff.Split('\n'))
            {
                if (line.StartsWith("diff --git ", StringComparison.Ordinal)) counted = false;
                else if (line.StartsWith("+++ b/", StringComparison.Ordinal)) counted = Counted(line.Substring(6));
                else if (line.StartsWith("--- a/", StringComparison.Ordinal)) counted = Counted(line.Substring(6));
                else if (counted && !line.StartsWith("+++", StringComparison.Ordinal) && !line.StartsWith("---", StringComparison.Ordinal) &&
                         (line.StartsWith("+", StringComparison.Ordinal) || line.StartsWith("-", StringComparison.Ordinal)))
                    yield return line;
            }
        }

        static readonly Regex SceneName = new Regex(@"^([+-])\s*m_Name: (.+)$");

        public static Summary Summarize(string from, string to)
        {
            var s = new Summary();
            string range = from + " " + to;
            foreach (string line in GitDiff("--name-status " + range).Split('\n'))
            {
                string[] parts = line.Split('\t');
                if (parts.Length < 2 || !Counted(parts[1])) continue;
                s.files.Add(parts[1]);
                if (parts[0] == "A" && parts[1].StartsWith("Assets/", StringComparison.Ordinal) && !parts[1].EndsWith(".cs", StringComparison.Ordinal))
                    s.assets.Add(parts[1]);
            }
            foreach (string line in ChangedLines(GitDiff("-U0 " + range + " -- *.cs")))
            {
                string code = line.Substring(1).Trim();
                if (line[0] != '+' || code.Length == 0 || code.StartsWith("//") || code.StartsWith("/*") || code.StartsWith("*")) continue;
                s.csAdded.Add(code);
            }
            foreach (string line in ChangedLines(GitDiff("-U0 " + range + " -- *.unity")))
            {
                Match m = SceneName.Match(line);
                if (!m.Success || m.Groups[2].Value.Trim().Length == 0) continue;
                s.sceneObjects.Add(m.Groups[1].Value + m.Groups[2].Value.Trim());
                if (m.Groups[1].Value == "+") s.sceneObjectsAdded++;
                else s.sceneObjectsRemoved++;
            }
            s.assetsAdded = s.assets.Count;
            s.csLines = s.csAdded.Count;
            return s;
        }

        // ---- JSON body (by hand: JsonUtility can't write null) ------------

        // Caps from EduDash's contract: it rejects an over-long body, it never truncates.
        const int TextCap = 500, NameCap = 100, ListCap = 200, EntryCap = 200, CountCap = 10000, DoneCap = 10;

        public static string Body(DateTime day, string log, Summary s, string project, string windowsUser)
        {
            string goal = "null";
            var done = new List<string>();
            bool failed = false;
            string today = TodaySection(log ?? "", day);
            foreach (string line in (today ?? "").Split('\n'))
            {
                if (line.StartsWith("**End Day failed:**", StringComparison.Ordinal)) failed = true;
                bool isGoal = line.StartsWith(GoalTag, StringComparison.Ordinal);
                if (!isGoal && !line.StartsWith(DoneTag, StringComparison.Ordinal)) continue;
                // Lines from the old package have no time tag, and EduDash needs one.
                Match m = TimeTag.Match(line);
                if (!m.Success) continue;
                string at = Json(m.Groups[1].Value + ":" + m.Groups[2].Value);
                int start = (isGoal ? GoalTag : DoneTag).Length;
                string text = Json(Cap(line.Substring(start, m.Index - start).TrimEnd(), TextCap));
                if (isGoal) goal = "{\"at\":" + at + ",\"text\":" + text + "}";
                else done.Add("{\"at\":" + at + ",\"minutesAfterStart\":" + (m.Groups[4].Success ? m.Groups[4].Value : "null") + ",\"text\":" + text + "}");
            }
            if (done.Count > DoneCap) done.RemoveRange(0, done.Count - DoneCap);

            return "{\"date\":" + Json(Date(day)) +
                   ",\"project\":" + Json(Cap(project, NameCap)) +
                   ",\"windowsUser\":" + Json(Cap(windowsUser, NameCap)) +
                   ",\"toolVersion\":" + Json(Version) +
                   ",\"goal\":" + goal +
                   ",\"done\":[" + string.Join(",", done) + "]" +
                   ",\"endDayFailed\":" + (failed ? "true" : "false") +
                   ",\"counts\":{\"csLines\":" + Count(s.csLines) + ",\"assetsAdded\":" + Count(s.assetsAdded) +
                   ",\"sceneObjectsAdded\":" + Count(s.sceneObjectsAdded) + ",\"sceneObjectsRemoved\":" + Count(s.sceneObjectsRemoved) + "}" +
                   ",\"files\":" + JsonList(s.files) +
                   ",\"assets\":" + JsonList(s.assets) +
                   ",\"sceneObjects\":" + JsonList(s.sceneObjects) +
                   ",\"csAdded\":" + JsonList(s.csAdded) + "}";
        }

        static int Count(int n) => Math.Max(0, Math.Min(n, CountCap));

        // A cut never leaves half of a surrogate pair.
        static string Cap(string text, int max)
        {
            if (text.Length <= max) return text;
            int n = char.IsHighSurrogate(text[max - 1]) ? max - 1 : max;
            return text.Substring(0, n);
        }

        static string JsonList(List<string> items)
        {
            var parts = new List<string>();
            for (int i = 0; i < items.Count && i < ListCap; i++) parts.Add(Json(Cap(items[i], EntryCap)));
            return "[" + string.Join(",", parts) + "]";
        }

        static string Json(string text)
        {
            var b = new StringBuilder("\"");
            foreach (char c in text)
            {
                if (c == '"' || c == '\\') b.Append('\\').Append(c);
                else if (c == '\n') b.Append("\\n");
                else if (c == '\r') b.Append("\\r");
                else if (c == '\t') b.Append("\\t");
                else if (c < ' ') b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else b.Append(c);
            }
            return b.Append('"').ToString();
        }

        // ---- Sending to EduDash --------------------------------------------
        // None of these throw: they run before saved = true in Start Day and before Unity quits.

        public static string IngestUrl = "https://dash.ogdenlab.local/api/devlog/ingest";

        // In UserSettings/, which the standard .gitignore keeps out of git.
        static string SettingsPath(string name) => Path.Combine(Git.Root, "UserSettings", name);
        static string CodePath => SettingsPath("ClassroomTools-code.txt");
        static string PendingPath => SettingsPath("ClassroomTools-pending.txt");

        static readonly Regex CodeShape = new Regex(@"^[A-Za-z0-9_-]{32}\z");

        public static bool IsCodeShaped(string code)
        {
            try { return code != null && CodeShape.IsMatch(code); }
            catch (Exception) { return false; }
        }

        public static string Code()
        {
            try
            {
                if (!File.Exists(CodePath)) return null;
                string code = File.ReadAllText(CodePath).Trim();
                return code.Length > 0 ? code : null;
            }
            catch (Exception) { return null; }
        }

        public static void SaveCode(string code)
        {
            try
            {
                if (code != null) WriteSetting(CodePath, code);
                else if (File.Exists(CodePath)) File.Delete(CodePath);
            }
            catch (Exception e) { CantSave(CodePath, e); }
        }

        public static List<string> Pending()
        {
            try
            {
                var days = new List<string>();
                if (!File.Exists(PendingPath)) return days;
                foreach (string line in File.ReadAllLines(PendingPath))
                    if (line.Trim().Length > 0) days.Add(line.Trim());
                return days;
            }
            catch (Exception) { return new List<string>(); }
        }

        public static void MarkPending(DateTime day)
        {
            try
            {
                var days = Pending();
                if (days.Contains(Date(day))) return;
                days.Add(Date(day));
                days.Sort(StringComparer.Ordinal);
                WriteSetting(PendingPath, string.Join("\n", days) + "\n");
            }
            catch (Exception e) { CantSave(PendingPath, e); }
        }

        static void Acknowledge(string date)
        {
            var days = Pending();
            if (!days.Remove(date)) return;
            if (days.Count == 0) File.Delete(PendingPath);
            else WriteSetting(PendingPath, string.Join("\n", days) + "\n");
        }

        static void WriteSetting(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        static void CantSave(string path, Exception e) =>
            Debug.LogWarning("[Classroom Tools] Couldn't save " + Path.GetFileName(path) + " (" + e.GetType().Name + ": " + e.Message + ").");

        public static long CheckCode(string code)
        {
            try { return Request(UnityWebRequest.Get(IngestUrl), code, out _, out _); }
            catch (Exception) { return 0; }
        }

        // A pasted code is checked with EduDash first; only a code EduDash says it doesn't know blocks the save.
        public static bool TryConnect(string code, out string error)
        {
            error = null;
            string c = (code ?? "").Trim();
            if (!IsCodeShaped(c))
            {
                error = "That isn't a whole Unity code. Copy it again from My Progress and paste it here.";
                return false;
            }
            long status = CheckCode(c);
            if (status == 401)
            {
                error = "EduDash doesn't know that code. Make a new one in My Progress and paste it here.";
                return false;
            }
            SaveCode(c);
            if (status != 204) Debug.LogWarning("[Classroom Tools] EduDash can't be reached, so your Unity code will be checked when your day log is sent.");
            return true;
        }

        public static long SendDay(DateTime day)
        {
            string date = Date(day);
            try
            {
                string code = Code();
                if (code == null) return 0;
                Summary summary = DayRange(day, out string from, out string to) ? Summarize(from, to) : new Summary();
                string body = Body(day, ReadLog(), summary, Path.GetFileName(Git.Root), Environment.UserName);
                var req = new UnityWebRequest(IngestUrl, "POST", new DownloadHandlerBuffer(), new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)));
                req.SetRequestHeader("Content-Type", "application/json");
                long status = Request(req, code, out string reply, out string error);
                if (status == 204) Acknowledge(date);
                else if (status == 400 || status == 413)
                {
                    // EduDash says not to send that same body again; a later change to the day marks it again.
                    Acknowledge(date);
                    Debug.LogWarning("[Classroom Tools] EduDash didn't accept the day log for " + date + " (" + status + "): " + reply);
                }
                else if (status == 401)
                {
                    SaveCode(null);
                    Debug.LogWarning("[Classroom Tools] EduDash doesn't know this project's Unity code any more. The next Start Day asks for a new one, or use Tools > Classroom > Connect to EduDash.");
                }
                else Unreachable(date, status, error);
                return status;
            }
            catch (Exception e)
            {
                Unreachable(date, 0, e.GetType().Name + ": " + e.Message);
                return 0;
            }
        }

        static void Unreachable(string date, long status, string error) =>
            Debug.LogWarning("[Classroom Tools] Couldn't send the day log for " + date + " (" + status + " " + error + "). It will be sent again later.");

        // Oldest first; stops at the first day EduDash didn't answer, so the rest wait for the next send point.
        public static void SendPending()
        {
            try
            {
                foreach (string d in Pending())
                {
                    long status = SendDay(DateTime.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture));
                    if (status != 204 && status != 400 && status != 413) return;
                }
            }
            catch (Exception)
            {
                // A bad line in the pending file stops the flush; the day log itself is safe in DEVLOG.md.
            }
        }

        // ponytail: blocks the editor, up to 10 s per request when the network drops packets.
        // Move to completed callbacks (the lab spike's pattern) if labs see freezes.
        static long Request(UnityWebRequest req, string code, out string reply, out string error)
        {
            using (req)
            {
                req.SetRequestHeader("Authorization", "Bearer " + code);
                req.timeout = 5;
                var op = req.SendWebRequest();
                var clock = System.Diagnostics.Stopwatch.StartNew();
                while (!op.isDone && clock.Elapsed.TotalSeconds < 10) Thread.Sleep(10);
                if (!op.isDone)
                {
                    req.Abort();
                    reply = "";
                    error = "no answer in 10 seconds";
                    return 0;
                }
                reply = req.downloadHandler?.text ?? "";
                error = req.error;
                return req.responseCode;
            }
        }
    }

    // One window for both prompts. Start Day blocks the editor and comes back if
    // closed before a goal is saved; End Day can be closed, the quit check still waits.
    public class EntryWindow : EditorWindow
    {
        // Serialized so a script reload keeps the window in its mode.
        [SerializeField] bool isEnd;
        [SerializeField] bool quitAfter;
        [SerializeField] string text = "";
        [SerializeField] bool saved;
        [SerializeField] string code = "";
        string codeError;
        int outOfPlace;
        static GUIStyle wrap;

        public static void Open(bool isEnd, bool quitAfter)
        {
            if (Application.isBatchMode) return;
            // A new window every time: reusing an open one would turn a pending Start Day into End Day.
            var w = CreateInstance<EntryWindow>();
            w.titleContent = new GUIContent(isEnd ? "End Day" : "Start Day");
            w.isEnd = isEnd;
            w.quitAfter = quitAfter;
            w.outOfPlace = isEnd ? OutOfPlace() : 0;
            w.minSize = new Vector2(420, isEnd ? 210 : 300);
            if (isEnd) w.ShowUtility();
            else w.ShowModalUtility();
        }

        static int OutOfPlace()
        {
            try { return ProjectLinter.Scan().FindAll(p => p.Category != ProjectLinter.FoldersCategory && p.Target != null).Count; }
            catch (Exception e)
            {
                Debug.LogError("[Classroom Tools] Project Linter failed: " + e);
                return 0;
            }
        }

        void OnDestroy()
        {
            if (!isEnd && !saved && !DayLog.GoalSavedToday()) EditorApplication.delayCall += () => Open(false, false);
        }

        void OnGUI()
        {
            bool askCode = !isEnd && !DayLog.TeacherLogin && DayLog.Code() == null;
            if (askCode)
            {
                EditorGUILayout.LabelField("Connect Unity to EduDash first. In EduDash, open My Progress, press Connect Unity, copy the code, and paste it here.", EditorStyles.wordWrappedLabel);
                code = EditorGUILayout.TextField("Unity code", code);
                if (codeError != null) EditorGUILayout.HelpBox(codeError, MessageType.Error);
                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField(isEnd ? "What did you get done today?" : "What's your goal today?", EditorStyles.boldLabel);
            wrap ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            text = EditorGUILayout.TextArea(text, wrap, GUILayout.ExpandHeight(true));

            int remaining = DayLog.MinLength - text.Trim().Length;
            EditorGUILayout.LabelField(remaining > 0 ? "Write at least " + remaining + " more characters." : " ", EditorStyles.miniLabel);

            if (outOfPlace > 0)
            {
                EditorGUILayout.HelpBox(outOfPlace + (outOfPlace == 1 ? " file is" : " files are") + " out of place.", MessageType.Info);
                if (GUILayout.Button("Open Project Linter")) LinterWindow.Open();
            }

            using (new EditorGUI.DisabledScope(remaining > 0))
            {
                if (GUILayout.Button(isEnd ? "Save and end my day" : "Save my goal", GUILayout.Height(28)) && (!askCode || DayLog.TryConnect(code, out codeError)))
                {
                    // Set first, so a failed save never traps a student behind a window that reopens.
                    saved = true;
                    if (isEnd)
                    {
                        if (DayLog.TrySaveDone(text) && !DayLog.TeacherLogin)
                        {
                            DayLog.MarkPending(DateTime.Now);
                            DayLog.SendPending();
                        }
                    }
                    else
                    {
                        DayLog.SaveGoal(text);
                        if (!DayLog.TeacherLogin)
                        {
                            DayLog.MarkPending(DateTime.Now);
                            DayLog.SendPending();
                        }
                    }
                    Close();
                    if (quitAfter)
                    {
                        DayLog.AllowNextQuit = true;
                        EditorApplication.Exit(0);
                    }
                }
            }
        }
    }

    // Start Day only asks for a code when none is stored, so a student who made a new one comes here.
    public class ConnectWindow : EditorWindow
    {
        [SerializeField] string code = "";
        string codeError;

        public static void Open()
        {
            var w = GetWindow<ConnectWindow>(true, "Connect to EduDash");
            w.minSize = new Vector2(420, 130);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("In EduDash, open My Progress, press Connect Unity or Make a new code, copy the code, and paste it here.", EditorStyles.wordWrappedLabel);
            code = EditorGUILayout.TextField("Unity code", code);
            if (codeError != null) EditorGUILayout.HelpBox(codeError, MessageType.Error);
            EditorGUILayout.Space();

            if (GUILayout.Button("Connect", GUILayout.Height(28)) && DayLog.TryConnect(code, out codeError))
            {
                Close();
                EditorApplication.delayCall += () =>
                {
                    DayLog.SendPending();
                    EditorUtility.DisplayDialog("Connect to EduDash", "Your Unity code is saved in this project.", "OK");
                };
            }
        }
    }
}
