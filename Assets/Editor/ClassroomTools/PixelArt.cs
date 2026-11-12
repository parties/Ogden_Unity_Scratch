// PixelArt.cs
//
// Part of Classroom Tools. Lives in  Assets/Editor/ClassroomTools/.
//
// Keeps pixel art crisp. The first time a PNG comes into the project it gets
// Point filter and no compression. A PNG inside a pack (Packs/<pack>/, or a
// folder with a License.txt) also gets Pixels Per Unit set to the pack's tile
// size, so one tile fills one tilemap cell.
//
// Tools > Classroom > Fix Pixel Art sets filter and compression on PNGs already
// in the project. It leaves Pixels Per Unit alone: changing it would resize
// sprites already placed in your scenes.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ClassroomTools
{
    public class PixelArt : AssetPostprocessor
    {
        const int MaxTileSize = 128;

        // ponytail: one size per pack for the whole editor session; a pack edited mid-session keeps its first size.
        static readonly Dictionary<string, int> tileSizes = new Dictionary<string, int>();

        void OnPreprocessTexture()
        {
            if (!IsPng(assetPath) || !IsFirstImport()) return;
            var importer = (TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            string pack = PackRoot(assetPath);
            int size = pack == null ? 0 : TileSize(pack);
            if (size > 0) importer.spritePixelsPerUnit = size;
        }

        // No .meta yet, or the short .meta a class portal pack ships with (it has no Pixels Per Unit).
        bool IsFirstImport()
        {
            if (assetImporter.importSettingsMissing) return true;
            string meta = assetPath + ".meta";
            return File.Exists(meta) && !File.ReadAllText(meta).Contains("spritePixelsToUnits");
        }

        static bool IsPng(string path) =>
            path.StartsWith("Assets/") && !path.StartsWith("Assets/TextMesh Pro/")
            && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

        // Assets/Packs/<pack>/..., or a top-level folder with a License.txt (not yet moved into Packs/).
        public static string PackRoot(string path)
        {
            string[] parts = path.Split('/');
            if (parts.Length > 3 && (parts[1] == "Packs" || parts[1] == "ThirdParty")) return string.Join("/", parts.Take(3));
            if (parts.Length > 2 && ProjectLinter.IsPack("Assets/" + parts[1])) return "Assets/" + parts[1];
            return null;
        }

        // The most common square size among the pack's small PNGs; 0 if it has none.
        public static int TileSize(string packRoot)
        {
            if (tileSizes.TryGetValue(packRoot, out int size)) return size;
            size = Directory.GetFiles(packRoot, "*.png", SearchOption.AllDirectories)
                .Select(PngSize)
                .Where(s => s.w == s.h && s.w > 0 && s.w <= MaxTileSize)
                .GroupBy(s => s.w)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                .Select(g => g.Key).FirstOrDefault();
            return tileSizes[packRoot] = size;
        }

        // Width and height from the PNG header, without loading the image.
        static (int w, int h) PngSize(string file)
        {
            var b = new byte[24];
            using (var f = File.OpenRead(file))
                if (f.Read(b, 0, 24) < 24 || b[1] != 'P' || b[2] != 'N' || b[3] != 'G') return (0, 0);
            return (b[16] << 24 | b[17] << 16 | b[18] << 8 | b[19], b[20] << 24 | b[21] << 16 | b[22] << 8 | b[23]);
        }

        [MenuItem("Tools/Classroom/Fix Pixel Art")]
        public static void FixMenu()
        {
            int n = FixAll();
            EditorUtility.DisplayDialog("Fix Pixel Art",
                n == 0 ? "Every PNG is already crisp." : n + (n == 1 ? " PNG now uses" : " PNGs now use") + " Point filter and no compression.", "OK");
        }

        // Returns how many PNGs changed.
        public static int FixAll()
        {
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in AssetDatabase.GetAllAssetPaths().Where(IsPng))
                {
                    if (!(AssetImporter.GetAtPath(path) is TextureImporter t)) continue;
                    if (t.filterMode == FilterMode.Point && t.textureCompression == TextureImporterCompression.Uncompressed) continue;
                    t.filterMode = FilterMode.Point;
                    t.textureCompression = TextureImporterCompression.Uncompressed;
                    t.SaveAndReimport();
                    changed++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            return changed;
        }
    }
}
