#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class EncounterArtSetup
    {
        private const string Root = "Assets/Art/Production/EncounterV3/";
        [Serializable] private sealed class Manifest { public string version; public string status; public Entry[] assets; }
        [Serializable] private sealed class Entry
        {
            public string path;
            public string category;
            public string normal;
            public string sha256;
            public string normalSha256;
            public float pixelsPerUnit;
            public int stage;
            public int group;
            public int frame;
        }

        [MenuItem("Tools/Mori Echoes/Production Art/Import Reviewed Encounters V3")]
        public static void ImportReviewed()
        {
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "art-manifest.json"));
            if (manifest == null || manifest.version != "encounter-v3" || manifest.status != "visually-reviewed" || manifest.assets == null || manifest.assets.Length != 60)
                throw new InvalidOperationException("Expected 60 visually reviewed encounter sprites.");
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null || !config.Validate(out _) || config.themes.Length != 5) throw new InvalidOperationException("Five valid themes required.");
            var slots = new HashSet<string>();
            var paths = new HashSet<string>();
            foreach (Entry entry in manifest.assets)
            {
                if (entry.stage < 0 || entry.stage >= 5 || entry.group < 0 || entry.group >= 4 ||
                    entry.category != "enemy" && entry.category != "effect" || entry.frame < 0 || entry.frame >= (entry.category == "enemy" ? 2 : 1) ||
                    !slots.Add(entry.category + ":" + entry.stage + ":" + entry.group + ":" + entry.frame))
                    throw new InvalidOperationException("Invalid or duplicate encounter slot.");
                string path = SafePath(entry.path);
                if (!paths.Add(path) || Hash(path) != entry.sha256 || entry.pixelsPerUnit <= 0 || float.IsNaN(entry.pixelsPerUnit) || float.IsInfinity(entry.pixelsPerUnit))
                    throw new InvalidOperationException("Invalid hash or PPU: " + path);
                if (!string.IsNullOrEmpty(entry.normal))
                {
                    string normal = SafePath(entry.normal);
                    if (!paths.Add(normal) || Hash(normal) != entry.normalSha256) throw new InvalidOperationException("Invalid normal: " + normal);
                }
                ThemeProfile theme = config.themes[entry.stage];
                Sprite previous = null;
                if (entry.category == "effect" && theme.encounterEffects != null && theme.encounterEffects.Length > entry.group) previous = theme.encounterEffects[entry.group];
                if (entry.category == "enemy" && theme.enemyAnimations != null && theme.enemyAnimations.Length > entry.group)
                {
                    SideSpriteAnimation animation = theme.enemyAnimations[entry.group];
                    if (animation != null && animation.frames != null && animation.frames.Length > entry.frame) previous = animation.frames[entry.frame];
                }
                if (previous != null && AssetDatabase.GetAssetPath(previous) != path) throw new InvalidOperationException("Preserving existing custom theme assignment.");
            }
            string backup = "Validation/EncounterConfig_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".asset";
            Directory.CreateDirectory("Validation"); File.Copy(MoriEchoesSetup.ConfigPath, backup, false);
            AssetDatabase.Refresh();
            var imported = new Dictionary<string, Sprite>();
            foreach (Entry entry in manifest.assets)
            {
                string path = SafePath(entry.path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Texture importer missing: " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.pixelsPerUnit; importer.spritePivot = Vector2.one * 0.5f;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.isReadable = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 1024;
                importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                if (!string.IsNullOrEmpty(entry.normal))
                {
                    string normalPath = SafePath(entry.normal);
                    var normal = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                    if (normal == null) throw new InvalidOperationException("Normal importer missing.");
                    normal.textureType = TextureImporterType.NormalMap; normal.mipmapEnabled = false;
                    normal.textureCompression = TextureImporterCompression.Uncompressed; normal.SaveAndReimport();
                    importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) } };
                }
                importer.SaveAndReimport();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Sprite missing: " + path);
                imported.Add(path, sprite);
            }
            Undo.RecordObject(config, "Import reviewed stage encounters");
            foreach (ThemeProfile theme in config.themes)
            {
                Array.Resize(ref theme.enemyAnimations, 4); Array.Resize(ref theme.encounterEffects, 4);
            }
            foreach (Entry entry in manifest.assets)
            {
                ThemeProfile theme = config.themes[entry.stage];
                Sprite sprite = imported[SafePath(entry.path)];
                if (entry.category == "effect") theme.encounterEffects[entry.group] = sprite;
                else
                {
                    SideSpriteAnimation animation = theme.enemyAnimations[entry.group] ?? (theme.enemyAnimations[entry.group] = new SideSpriteAnimation());
                    Array.Resize(ref animation.frames, 2); animation.frames[entry.frame] = sprite; animation.facesRight = false; animation.framesPerSecond = 4;
                }
            }
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            Debug.Log("Encounter V3 imported: 20 distinct minions, 40 animation frames, 20 effects. Existing scenes preserved. Backup: " + backup);
        }

        private static string SafePath(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidOperationException("Relative PNG path required.");
            string root = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(Root, relative));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !full.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Image path outside encounter directory.");
            return Root + full.Substring(root.Length).Replace('\\', '/');
        }

        private static string Hash(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
