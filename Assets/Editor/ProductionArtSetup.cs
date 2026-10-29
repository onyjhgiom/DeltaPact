#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class ProductionArtSetup
    {
        private const string Root = "Assets/Art/Production/AnimeV1/";
        [Serializable] private sealed class Manifest { public string version; public string status; public Entry[] assets; }
        [Serializable] private sealed class Entry
        {
            public string path;
            public string category;
            public string normal;
            public string sha256;
            public string normalSha256;
            public float pixelsPerUnit;
            public int group;
            public int frame;
        }

        [MenuItem("Tools/Mori Echoes/Production Art/Import Reviewed Anime V1")]
        public static void ImportReviewed()
        {
            const string manifestPath = Root + "art-manifest.json";
            if (!File.Exists(manifestPath)) throw new InvalidOperationException("No generated art manifest. Config and scenes preserved.");
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.version != "anime-v1" || manifest.status != "visually-reviewed" || manifest.assets == null || manifest.assets.Length != 80)
                throw new InvalidOperationException("Expected 80 visually reviewed sprites. Inspect contact sheet and originals before setting manifest status to visually-reviewed.");
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null || !config.Validate(out string reason)) throw new InvalidOperationException("Valid existing configuration required.");
            var destinations = new HashSet<string>();
            var assignments = new HashSet<string>();
            foreach (Entry entry in manifest.assets)
            {
                string path = SafePath(entry.path);
                if (!destinations.Add(path) || !File.Exists(path) || Hash(path) != entry.sha256 || entry.pixelsPerUnit <= 0 || float.IsNaN(entry.pixelsPerUnit) || float.IsInfinity(entry.pixelsPerUnit))
                    throw new InvalidOperationException("Invalid or modified sprite: " + path);
                ValidateSlot(entry);
                if (!assignments.Add(entry.category + ":" + entry.group + ":" + entry.frame)) throw new InvalidOperationException("Duplicate art slot.");
                Sprite current = Existing(config, entry);
                if (current != null && AssetDatabase.GetAssetPath(current) != path) throw new InvalidOperationException("Existing art preserved: " + path);
                if (!string.IsNullOrEmpty(entry.normal))
                {
                    string normalPath = SafePath(entry.normal);
                    if (!destinations.Add(normalPath) || !File.Exists(normalPath) || Hash(normalPath) != entry.normalSha256)
                        throw new InvalidOperationException("Missing or modified normal map: " + path);
                }
            }
            if (assignments.Count != 80) throw new InvalidOperationException("Incomplete art assignment set.");
            AssetDatabase.Refresh();
            var imported = new Dictionary<string, Sprite>();
            foreach (Entry entry in manifest.assets)
            {
                string path = SafePath(entry.path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("PNG importer missing: " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.pixelsPerUnit; importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.isReadable = false;
                importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = entry.category == "background" ? 2048 : 1024;
                if (!string.IsNullOrEmpty(entry.normal))
                {
                    string normalPath = SafePath(entry.normal);
                    var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                    if (normalImporter == null) throw new InvalidOperationException("Normal importer missing.");
                    normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.mipmapEnabled = false; normalImporter.isReadable = false;
                    normalImporter.textureCompression = TextureImporterCompression.Uncompressed; normalImporter.maxTextureSize = 1024; normalImporter.SaveAndReimport();
                    importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) } };
                }
                importer.SaveAndReimport();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Sprite load failed: " + path);
                imported.Add(path, sprite);
            }
            string backup = Path.Combine("Validation", "ArtConfig_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".asset");
            Directory.CreateDirectory("Validation"); File.Copy(MoriEchoesSetup.ConfigPath, backup, false);
            Undo.RecordObject(config, "Import reviewed production anime art");
            foreach (Entry entry in manifest.assets) Assign(config, entry, imported[SafePath(entry.path)]);
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            Debug.Log("Imported 80 reviewed anime sprites; existing scenes preserved. Config backup: " + backup);
        }

        private static string SafePath(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidOperationException("Relative art path required.");
            string path = Path.GetFullPath(Path.Combine(Root, relative));
            string root = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Art path outside versioned directory.");
            return Root + path.Substring(root.Length).Replace('\\', '/');
        }

        private static string Hash(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void ValidateSlot(Entry entry)
        {
            int groups; int frames;
            switch (entry.category)
            {
                case "character": groups = 4; frames = 8; break;
                case "portrait": groups = 4; frames = 1; break;
                case "enemy": groups = 4; frames = 2; break;
                case "boss": groups = 5; frames = 4; break;
                case "background": groups = 5; frames = 1; break;
                case "weapon": groups = 3; frames = 1; break;
                case "effect": groups = 8; frames = 1; break;
                default: throw new InvalidOperationException("Unknown art category.");
            }
            if (entry.group < 0 || entry.group >= groups || entry.frame < 0 || entry.frame >= frames) throw new InvalidOperationException("Art slot out of range.");
        }

        private static Sprite Existing(GameConfig config, Entry entry)
        {
            switch (entry.category)
            {
                case "portrait": return config.characters[entry.group].portrait;
                case "background": return config.themes[entry.group].background;
                case "weapon": return config.weaponSprites != null && entry.group < config.weaponSprites.Length ? config.weaponSprites[entry.group] : null;
                case "effect": return config.combatEffectSprites != null && entry.group < config.combatEffectSprites.Length ? config.combatEffectSprites[entry.group] : null;
            }
            SideSpriteAnimation animation = entry.category == "character" ? config.characters[entry.group].animation
                : entry.category == "boss" ? config.themes[entry.group].bossAnimation
                : config.enemyAnimations != null && entry.group < config.enemyAnimations.Length ? config.enemyAnimations[entry.group] : null;
            return animation != null && animation.frames != null && entry.frame < animation.frames.Length ? animation.frames[entry.frame] : null;
        }

        private static void Assign(GameConfig config, Entry entry, Sprite sprite)
        {
            switch (entry.category)
            {
                case "portrait": config.characters[entry.group].portrait = sprite; return;
                case "background": config.themes[entry.group].background = sprite; return;
                case "weapon": if (config.weaponSprites == null || config.weaponSprites.Length < 3) Array.Resize(ref config.weaponSprites, 3); config.weaponSprites[entry.group] = sprite; return;
                case "effect": if (config.combatEffectSprites == null || config.combatEffectSprites.Length < 8) Array.Resize(ref config.combatEffectSprites, 8); config.combatEffectSprites[entry.group] = sprite; return;
            }
            SideSpriteAnimation animation;
            int count;
            if (entry.category == "character") { animation = config.characters[entry.group].animation ?? (config.characters[entry.group].animation = new SideSpriteAnimation()); count = 8; }
            else if (entry.category == "boss") { animation = config.themes[entry.group].bossAnimation ?? (config.themes[entry.group].bossAnimation = new SideSpriteAnimation()); count = 4; }
            else
            {
                if (config.enemyAnimations == null || config.enemyAnimations.Length < 4) Array.Resize(ref config.enemyAnimations, 4);
                animation = config.enemyAnimations[entry.group] ?? (config.enemyAnimations[entry.group] = new SideSpriteAnimation()); count = 2;
            }
            if (animation.frames == null || animation.frames.Length < count) Array.Resize(ref animation.frames, count);
            animation.frames[entry.frame] = sprite; animation.facesRight = entry.category == "character";
        }
    }
}
#endif
