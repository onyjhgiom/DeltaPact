#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class ArsenalSetup
    {
        private const string Root = "Assets/Art/Production/ArsenalV2/";
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

        [MenuItem("Tools/Mori Echoes/Production Art/Import Reviewed Arsenal V2")]
        public static void ImportReviewed()
        {
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "art-manifest.json"));
            if (manifest == null || manifest.version != "arsenal-v2" || manifest.status != "visually-reviewed" || manifest.assets == null || manifest.assets.Length != 50)
                throw new InvalidOperationException("Expected 50 visually reviewed arsenal sprites.");
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null || !config.Validate(out _)) throw new InvalidOperationException("Existing valid config required.");
            var slots = new HashSet<string>();
            var paths = new HashSet<string>();
            foreach (Entry entry in manifest.assets)
            {
                int groups = entry.category == "portrait" ? 4 : entry.category == "boss" ? 5 : entry.category == "scenery" ? 8
                    : entry.category == "weapon" || entry.category == "view" || entry.category == "platform" ? 6 : 0;
                int frames = entry.category == "boss" ? 4 : 1;
                if (entry.group < 0 || entry.group >= groups || entry.frame < 0 || entry.frame >= frames ||
                    !slots.Add(entry.category + ":" + entry.group + ":" + entry.frame)) throw new InvalidOperationException("Invalid or duplicate art slot.");
                string path = SafePath(entry.path);
                if (!paths.Add(path) || Hash(path) != entry.sha256 || entry.pixelsPerUnit <= 0 || float.IsNaN(entry.pixelsPerUnit) || float.IsInfinity(entry.pixelsPerUnit))
                    throw new InvalidOperationException("Invalid sprite hash or PPU: " + path);
                if (!string.IsNullOrEmpty(entry.normal))
                {
                    string normal = SafePath(entry.normal);
                    if (!paths.Add(normal) || Hash(normal) != entry.normalSha256) throw new InvalidOperationException("Invalid normal map: " + normal);
                }
                Sprite previous = Current(config, entry);
                string previousPath = previous != null ? AssetDatabase.GetAssetPath(previous) : "";
                if (previous != null && !previousPath.StartsWith("Assets/Art/Production/AnimeV1/", StringComparison.Ordinal) && previousPath != path)
                    throw new InvalidOperationException("Custom asset preserved; review existing assignment: " + previousPath);
            }
            string backup = Path.Combine("Validation", "ArsenalConfig_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".asset");
            Directory.CreateDirectory("Validation"); File.Copy(MoriEchoesSetup.ConfigPath, backup, false);
            AssetDatabase.Refresh();
            var imported = new Dictionary<string, Sprite>();
            foreach (Entry entry in manifest.assets)
            {
                string path = SafePath(entry.path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing texture importer: " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.pixelsPerUnit; importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.isReadable = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 1024;
                importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                if (!string.IsNullOrEmpty(entry.normal))
                {
                    string normal = SafePath(entry.normal);
                    var normalImporter = AssetImporter.GetAtPath(normal) as TextureImporter;
                    if (normalImporter == null) throw new InvalidOperationException("Missing normal importer.");
                    normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.mipmapEnabled = false; normalImporter.isReadable = false;
                    normalImporter.textureCompression = TextureImporterCompression.Uncompressed; normalImporter.SaveAndReimport();
                    importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = AssetDatabase.LoadAssetAtPath<Texture2D>(normal) } };
                }
                importer.SaveAndReimport();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Sprite import failed: " + path);
                imported.Add(path, sprite);
            }
            Undo.RecordObject(config, "Upgrade reviewed arsenal presentation");
            ExtendWeapons(config);
            Array.Resize(ref config.weaponSprites, 6); Array.Resize(ref config.weaponViewSprites, 6);
            Array.Resize(ref config.scenerySprites, 8); Array.Resize(ref config.platformSprites, 6);
            foreach (Entry entry in manifest.assets)
            {
                Sprite sprite = imported[SafePath(entry.path)];
                switch (entry.category)
                {
                    case "portrait": config.characters[entry.group].portrait = sprite; break;
                    case "weapon": config.weaponSprites[entry.group] = sprite; break;
                    case "view": config.weaponViewSprites[entry.group] = sprite; break;
                    case "scenery": config.scenerySprites[entry.group] = sprite; break;
                    case "platform": config.platformSprites[entry.group] = sprite; break;
                    case "boss":
                        SideSpriteAnimation animation = config.themes[entry.group].bossAnimation ?? (config.themes[entry.group].bossAnimation = new SideSpriteAnimation());
                        Array.Resize(ref animation.frames, 4); animation.frames[entry.frame] = sprite; animation.facesRight = false; break;
                }
            }
            config.allCharactersAvailable = true;
            config.characters[3].role = "蜂群爆破 · 弹匣 +30%";
            PlayerSettings.resizableWindow = true; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            if (!config.Validate(out string reason)) throw new InvalidOperationException(reason);
            Debug.Log("Arsenal V2 imported: 50 sprites, six weapons, four selectable characters; scenes preserved. Backup: " + backup);
        }

        private static void ExtendWeapons(GameConfig config)
        {
            int previous = config.weapons.Length;
            Array.Resize(ref config.weapons, 6);
            if (previous < 4) config.weapons[3] = new WeaponDefinition("SMG-21 / 疾风", 7, 13, 42, 1.05f, 1, 5, 1, 27, new Color(0.2f, 0.8f, 1)) { recoil = 0.035f };
            if (previous < 5) config.weapons[4] = new WeaponDefinition("RG-08 / 电磁轨道", 82, 0.7f, 4, 1.9f, 1, 0, 4, 52, new Color(0.8f, 0.35f, 1)) { recoil = 0.2f };
            if (previous < 6) config.weapons[5] = new WeaponDefinition("GL-40 / 烈焰榴弹", 42, 1.2f, 5, 1.8f, 1, 3, 1, 15, new Color(1, 0.64f, 0.1f)) { explosionRadius = 2.5f, recoil = 0.16f };
        }

        private static Sprite Current(GameConfig config, Entry entry)
        {
            if (entry.category == "portrait") return config.characters[entry.group].portrait;
            if (entry.category == "boss")
            {
                SideSpriteAnimation animation = config.themes[entry.group].bossAnimation;
                return animation != null && animation.frames != null && entry.frame < animation.frames.Length ? animation.frames[entry.frame] : null;
            }
            Sprite[] sprites = entry.category == "weapon" ? config.weaponSprites : entry.category == "view" ? config.weaponViewSprites
                : entry.category == "scenery" ? config.scenerySprites : config.platformSprites;
            return sprites != null && entry.group < sprites.Length ? sprites[entry.group] : null;
        }

        private static string SafePath(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidOperationException("Relative image path required.");
            string root = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(Root, relative));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !full.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Image path outside arsenal directory.");
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
