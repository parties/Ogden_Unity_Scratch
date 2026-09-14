// CheckMyTilemap.cs
//
// Drop this file into  Assets/Editor/  in your Unity project.
// Then use the menu:   Tools > Check My Tilemap
//
// It checks your scene and gives you two things:
//   1. a list of what is working and what is not, with the fix for each
//   2. a submission code to paste into the Canvas assignment
//
// It never changes your project. It only looks.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class CheckMyTilemap : EditorWindow
{
    const string SchemaVersion = "tm1";
    const string Prefix = "VGD";

    struct Check
    {
        public bool Pass;
        public string Label;
        public string Fix;
        public Check(bool pass, string label, string fix) { Pass = pass; Label = label; Fix = fix; }
    }

    List<Check> _checks = new List<Check>();
    string _code = "";
    Vector2 _scroll;

    [MenuItem("Tools/Check My Tilemap")]
    static void Open()
    {
        var w = GetWindow<CheckMyTilemap>(true, "Check My Tilemap", true);
        w.minSize = new Vector2(560, 520);
        w.Run();
        w.Show();
    }

    void Run()
    {
        _checks.Clear();

        var scene = SceneManager.GetActiveScene();

        // ---- find the tilemap with the most tiles painted on it ----
        Tilemap tilemap = null;
        int tileCount = 0;
        var allTilemaps = AllTilemaps();
        foreach (var tm in allTilemaps)
        {
            int n = CountTiles(tm);
            if (n > tileCount) { tileCount = n; tilemap = tm; }
        }
        if (tilemap == null && allTilemaps.Length > 0) tilemap = allTilemaps[0];

        // ---- find the player ----
        GameObject player = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            player = FindByNameContains(go.transform, "player");
            if (player != null) break;
        }

        // ---- read everything we are going to report on ----
        int distinctTiles = tilemap != null ? tilemap.GetUsedTilesCount() : 0;
        float ppu = FirstTilePixelsPerUnit(tilemap);
        int maxGap = MaxGapColumns(tilemap);

        TilemapCollider2D tmCollider = tilemap != null ? tilemap.GetComponent<TilemapCollider2D>() : null;
        CompositeCollider2D composite = tilemap != null ? tilemap.GetComponent<CompositeCollider2D>() : null;
        Rigidbody2D groundBody = tilemap != null ? tilemap.GetComponent<Rigidbody2D>() : null;

        int pathCount = composite != null ? composite.pathCount : 0;
        bool merged = IsMerged(tmCollider);
        string bodyType = groundBody != null ? groundBody.bodyType.ToString() : "none";

        Rigidbody2D playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
        Collider2D playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        float jumpForce = FindFloatField(player, "jumpForce");
        float gravityScale = playerBody != null ? playerBody.gravityScale : 0f;

        // ---- the checks, in the order the lab does them ----
        _checks.Add(new Check(tilemap != null,
            "You have a Tilemap in your scene",
            "GameObject > 2D Object > Tilemap > Rectangular"));

        _checks.Add(new Check(tileCount > 0,
            "You painted tiles on it  (" + tileCount + " tiles)",
            "Open Window > 2D > Tile Palette, pick a tile, and paint with the brush"));

        _checks.Add(new Check(ppu == 64f,
            "Your tiles are set to Pixels Per Unit 64  (yours: " + (ppu > 0f ? ppu.ToString() : "unknown") + ")",
            "Select all your tile sprites, set Pixels Per Unit to 64 in the Inspector, press Apply"));

        _checks.Add(new Check(player != null,
            "You have a Player object in the scene" + (player != null ? "  (\"" + player.name + "\")" : ""),
            "Your character object needs the word Player in its name so this check can find it"));

        _checks.Add(new Check(playerBody != null,
            "Your Player has a Rigidbody 2D",
            "Select the Player, Add Component, Rigidbody 2D"));

        _checks.Add(new Check(playerCollider != null,
            "Your Player has a Collider 2D",
            "Select the Player, Add Component, Box Collider 2D"));

        _checks.Add(new Check(tmCollider != null,
            "Your Tilemap has a Tilemap Collider 2D",
            "Select the Tilemap (the child, not the Grid), Add Component, Tilemap Collider 2D"));

        _checks.Add(new Check(composite != null,
            "Your Tilemap has a Composite Collider 2D",
            "Add Component, Composite Collider 2D. It adds a Rigidbody 2D for you."));

        _checks.Add(new Check(groundBody != null && groundBody.bodyType == RigidbodyType2D.Static,
            "That Rigidbody 2D is set to Static  (yours: " + bodyType + ")",
            "Set Body Type to Static, or your whole level falls out of the world when you press Play"));

        _checks.Add(new Check(merged,
            "Your Tilemap Collider is merged into the Composite",
            MergeFixText()));

        _checks.Add(new Check(pathCount >= 1,
            "Your level has a solid outline to stand on  (" + pathCount + " found)",
            "This appears once the three collider parts above are all set correctly"));

        int passed = 0;
        foreach (var c in _checks) if (c.Pass) passed++;

        // ---- the submission code ----
        var json = new StringBuilder();
        json.Append("{");
        Field(json, "v", SchemaVersion); json.Append(",");
        Field(json, "ts", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")); json.Append(",");
        Field(json, "unity", Application.unityVersion); json.Append(",");
        Field(json, "pid", PlayerSettings.productGUID.ToString()); json.Append(",");
        Field(json, "proj", Application.productName); json.Append(",");
        Field(json, "mach", SystemInfo.deviceName); json.Append(",");
        Field(json, "scene", scene.name); json.Append(",");
        Number(json, "tiles", tileCount); json.Append(",");
        Number(json, "types", distinctTiles); json.Append(",");
        Number(json, "ppu", ppu); json.Append(",");
        Number(json, "gap", maxGap); json.Append(",");
        Number(json, "paths", pathCount); json.Append(",");
        Bool(json, "tmcol", tmCollider != null); json.Append(",");
        Bool(json, "comp", composite != null); json.Append(",");
        Bool(json, "merged", merged); json.Append(",");
        Field(json, "body", bodyType); json.Append(",");
        Bool(json, "player", player != null); json.Append(",");
        Field(json, "pname", player != null ? player.name : ""); json.Append(",");
        Bool(json, "prb", playerBody != null); json.Append(",");
        Bool(json, "pcol", playerCollider != null); json.Append(",");
        Number(json, "jump", jumpForce); json.Append(",");
        Number(json, "grav", gravityScale); json.Append(",");
        Number(json, "pass", passed); json.Append(",");
        Number(json, "total", _checks.Count); json.Append(",");
        json.Append("\"objs\":[");
        var roots = scene.GetRootGameObjects();
        int limit = Mathf.Min(roots.Length, 40);
        for (int i = 0; i < limit; i++)
        {
            if (i > 0) json.Append(",");
            json.Append("\"").Append(Escape(roots[i].name)).Append("\"");
        }
        json.Append("]}");

        string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json.ToString()));
        _code = Prefix + "." + SchemaVersion + "." + payload + "." + Checksum(payload);
    }

    // =====================================================================
    // window
    // =====================================================================

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        int passed = 0;
        foreach (var c in _checks) if (c.Pass) passed++;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(passed + " of " + _checks.Count + " checks passed", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        foreach (var c in _checks)
        {
            EditorGUILayout.LabelField((c.Pass ? "PASS   " : "TO FIX   ") + c.Label);
            if (!c.Pass)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("-> " + c.Fix, EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Submission code", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Copy all of this into the Canvas assignment. Submit it even if checks are still failing.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.SelectableLabel(_code, EditorStyles.textArea, GUILayout.Height(90));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy code", GUILayout.Height(26)))
        {
            EditorGUIUtility.systemCopyBuffer = _code;
            ShowNotification(new GUIContent("Copied"));
        }
        if (GUILayout.Button("Check again", GUILayout.Height(26))) Run();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    // =====================================================================
    // helpers
    // =====================================================================

    static Tilemap[] AllTilemaps()
    {
#if UNITY_2022_2_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<Tilemap>();
#endif
    }

    static bool IsMerged(TilemapCollider2D c)
    {
        if (c == null) return false;
#if UNITY_2023_1_OR_NEWER
        return c.compositeOperation == Collider2D.CompositeOperation.Merge;
#else
        return c.usedByComposite;
#endif
    }

    static string MergeFixText()
    {
#if UNITY_2023_1_OR_NEWER
        return "On the Tilemap Collider 2D, set Composite Operation to Merge";
#else
        return "On the Tilemap Collider 2D, tick the Used By Composite checkbox";
#endif
    }

    static int CountTiles(Tilemap tm)
    {
        if (tm == null) return 0;
        int n = 0;
        var bounds = tm.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
            if (tm.HasTile(pos)) n++;
        return n;
    }

    static float FirstTilePixelsPerUnit(Tilemap tm)
    {
        if (tm == null) return 0f;
        foreach (var pos in tm.cellBounds.allPositionsWithin)
        {
            var sprite = tm.GetSprite(pos);
            if (sprite != null) return sprite.pixelsPerUnit;
        }
        return 0f;
    }

    // Widest run of completely empty columns inside the painted area.
    // This is the gap the player has to clear.
    static int MaxGapColumns(Tilemap tm)
    {
        if (tm == null) return 0;
        var b = tm.cellBounds;
        int first = int.MaxValue, last = int.MinValue;
        var occupied = new HashSet<int>();
        foreach (var pos in b.allPositionsWithin)
        {
            if (!tm.HasTile(pos)) continue;
            occupied.Add(pos.x);
            if (pos.x < first) first = pos.x;
            if (pos.x > last) last = pos.x;
        }
        if (occupied.Count == 0) return 0;

        int max = 0, run = 0;
        for (int x = first; x <= last; x++)
        {
            if (occupied.Contains(x)) { if (run > max) max = run; run = 0; }
            else run++;
        }
        if (run > max) max = run;
        return max;
    }

    static GameObject FindByNameContains(Transform t, string needle)
    {
        if (t.name.ToLowerInvariant().Contains(needle)) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindByNameContains(t.GetChild(i), needle);
            if (hit != null) return hit;
        }
        return null;
    }

    static float FindFloatField(GameObject go, string fieldName)
    {
        if (go == null) return 0f;
        foreach (var c in go.GetComponents<MonoBehaviour>())
        {
            if (c == null) continue;
            var f = c.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (f != null && f.FieldType == typeof(float)) return (float)f.GetValue(c);
        }
        return 0f;
    }

    static string Checksum(string s)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (char ch in s) { h ^= ch; h *= 16777619; }
            return (h % 0xFFFF).ToString("x4");
        }
    }

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }

    static void Field(StringBuilder sb, string k, string v)
    {
        sb.Append("\"").Append(k).Append("\":\"").Append(Escape(v)).Append("\"");
    }

    static void Number(StringBuilder sb, string k, float v)
    {
        sb.Append("\"").Append(k).Append("\":").Append(v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
    }

    static void Bool(StringBuilder sb, string k, bool v)
    {
        sb.Append("\"").Append(k).Append("\":").Append(v ? "true" : "false");
    }
}
