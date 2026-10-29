#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class CombatPolishArtSetup
    {
        private const string Root = "Assets/Art/Production/CombatPolish/";
        private const string Version = "combat-polish-v1";
        private static readonly string[] ThemeKeys = { "ruins", "factory", "ice", "neon", "fortress" };
        private static readonly string[] BossNames = { "破碎者", "熔核", "霜牙", "零点", "终末回响" };
        private static readonly string[] PoweredPrefixes = { "DELTA", "VANGUARD", "WRAITH", "HAVOC" };

        [Serializable]
        private sealed class Manifest
        {
            public string version;
            public string status;
            public int baselineTop;
            public Entry[] assets;
        }

        [Serializable]
        private sealed class Entry
        {
            public string path;
            public string kind;
            public string key;
            public int phase;
            public int frame;
            public string sha256;
            public string guid;
            public string normalPath;
            public string normalSha256;
            public float pixelsPerUnit;
            public int width;
            public int height;
            public int footBaselineTop;
            public float boundsHeightUnits;
            public string mesh;
        }

        [MenuItem("Tools/Mori Echoes/Production Art/Apply Reviewed Combat Polish")]
        public static void Apply()
        {
            string manifestPath = Root + "art-manifest.json";
            if (!File.Exists(manifestPath)) throw new InvalidOperationException("Prepare the reviewed combat-polish-v1 images first.");
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.version != Version || manifest.status != "visually-reviewed" || manifest.baselineTop != 469 || manifest.assets == null)
                throw new InvalidOperationException("A visually reviewed combat-polish-v1 manifest is required.");

            ValidateManifest(manifest);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null || config.characters == null || config.themes == null || config.themes.Length < ThemeKeys.Length)
                throw new InvalidOperationException("Existing character roster and five ordered themes are required. Run ExpandedRosterSetup.Apply first when installing an expanded roster.");

            var poweredCharacters = new CharacterDefinition[PoweredPrefixes.Length];
            for (int index = 0; index < PoweredPrefixes.Length; index++)
            {
                CharacterDefinition character = FindCharacter(config.characters, PoweredPrefixes[index]);
                string path = Root + "Characters/" + PoweredPrefixes[index].ToLowerInvariant() + "-powered.png";
                RequireUnassignedOrOwned(character.poweredAnimation, path);
                poweredCharacters[index] = character;
            }
            for (int stage = 0; stage < ThemeKeys.Length; stage++)
            {
                ThemeProfile theme = config.themes[stage];
                if (theme == null || theme.boss != BossNames[stage] || theme.bossAnimation == null || !theme.bossAnimation.IsReady)
                    throw new InvalidOperationException("Ordered theme identity and original stage-one boss animation must be present: " + ThemeKeys[stage]);
                if (theme.bossForms != null)
                {
                    if (theme.bossForms.Length > 3) throw new InvalidOperationException("Custom additional boss forms preserved: " + ThemeKeys[stage]);
                    if (theme.bossForms.Length > 0 && theme.bossForms[0] != null && theme.bossForms[0].IsReady && !SameAnimation(theme.bossForms[0], theme.bossAnimation))
                        throw new InvalidOperationException("Custom stage-one boss form preserved: " + ThemeKeys[stage]);
                    for (int phase = 2; phase <= 3; phase++)
                        if (theme.bossForms.Length >= phase) RequireUnassignedOrOwned(theme.bossForms[phase - 1], Root + "Bosses/" + ThemeKeys[stage] + "-form-" + phase + ".png");
                }
                if (config.solidWallSprites != null && config.solidWallSprites.Length > stage)
                    RequireUnassignedOrOwned(config.solidWallSprites[stage], Root + "Walls/" + ThemeKeys[stage] + ".png");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (Entry entry in manifest.assets)
            {
                string path = Root + entry.path;
                if (AssetDatabase.AssetPathToGUID(path) != entry.guid) throw new InvalidOperationException("Sprite GUID mismatch: " + path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing new texture importer: " + path);
                if (!string.IsNullOrEmpty(entry.normalPath))
                {
                    string normalPath = Root + entry.normalPath;
                    var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                    if (normalImporter == null) throw new InvalidOperationException("Missing new normal importer: " + normalPath);
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    ConfigureTexture(normalImporter);
                    normalImporter.SaveAndReimport();
                    Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                    if (normal == null) throw new InvalidOperationException("Normal import failed: " + normalPath);
                    importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } };
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.pixelsPerUnit;
                importer.spritePivot = Vector2.one * 0.5f;
                importer.alphaIsTransparency = true;
                ConfigureTexture(importer);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = Vector2.one * 0.5f;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null || Mathf.Abs(sprite.bounds.size.y - entry.boundsHeightUnits) > 0.001f)
                    throw new InvalidOperationException("Full-rect sprite bounds mismatch: " + path);
                sprites.Add(entry.path, sprite);
            }

            var forms = new SideSpriteAnimation[ThemeKeys.Length][];
            var walls = new Sprite[Math.Max(config.themes.Length, config.solidWallSprites != null ? config.solidWallSprites.Length : 0)];
            if (config.solidWallSprites != null) Array.Copy(config.solidWallSprites, walls, config.solidWallSprites.Length);
            for (int stage = 0; stage < ThemeKeys.Length; stage++)
            {
                forms[stage] = new[]
                {
                    config.themes[stage].bossAnimation,
                    Animation(sprites["Bosses/" + ThemeKeys[stage] + "-form-2.png"], false),
                    Animation(sprites["Bosses/" + ThemeKeys[stage] + "-form-3.png"], false)
                };
                walls[stage] = sprites["Walls/" + ThemeKeys[stage] + ".png"];
            }
            string backupFolder = "ArtProduction/combat-polish-v1/config-backups";
            Directory.CreateDirectory(backupFolder);
            string backup = backupFolder + "/BeforeApply_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N") + ".asset";
            File.Copy(MoriEchoesSetup.ConfigPath, backup, false);
            Undo.RecordObject(config, "Apply reviewed combat polish art");
            for (int index = 0; index < poweredCharacters.Length; index++)
                poweredCharacters[index].poweredAnimation = Animation(sprites["Characters/" + PoweredPrefixes[index].ToLowerInvariant() + "-powered.png"], true);
            for (int stage = 0; stage < ThemeKeys.Length; stage++) config.themes[stage].bossForms = forms[stage];
            config.solidWallSprites = walls;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("Combat polish applied: ten new boss forms, original bossAnimation retained, four callsign-bound powered forms and five solid walls. Additional characters and original animations preserved. Backup: " + backup);
        }

        private static void ValidateManifest(Manifest manifest)
        {
            var expected = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string key in ThemeKeys)
            {
                expected.Add("Bosses/" + key + "-form-2.png", "boss-form:" + key + ":2");
                expected.Add("Bosses/" + key + "-form-3.png", "boss-form:" + key + ":3");
                expected.Add("Walls/" + key + ".png", "wall:" + key + ":0");
            }
            foreach (string prefix in PoweredPrefixes)
            {
                string key = prefix.ToLowerInvariant();
                expected.Add("Characters/" + key + "-powered.png", "powered:" + key + ":0");
            }
            var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
            if (manifest.assets.Length != expected.Count) throw new InvalidOperationException("Exactly the reviewed core art entries are required; roster size is not restricted.");
            foreach (Entry entry in manifest.assets)
            {
                if (entry == null || string.IsNullOrEmpty(entry.path) || !expected.TryGetValue(entry.path, out string identity)
                    || identity != entry.kind + ":" + entry.key + ":" + entry.phase || entry.frame != 0 || result.ContainsKey(entry.path)
                    || entry.mesh != "FullRect" || entry.pixelsPerUnit != 256 || entry.width < 1 || entry.height < 1
                    || float.IsNaN(entry.boundsHeightUnits) || float.IsInfinity(entry.boundsHeightUnits)
                    || Mathf.Abs(entry.boundsHeightUnits - entry.height / entry.pixelsPerUnit) > 0.0001f)
                    throw new InvalidOperationException("Invalid or duplicate combat polish manifest entry.");
                if (entry.kind != "wall" && (entry.height != 512 || entry.footBaselineTop != 469))
                    throw new InvalidOperationException("Character baseline or canvas differs: " + entry.path);
                if (entry.normalPath != entry.path.Replace(".png", "-normal.png")) throw new InvalidOperationException("Unexpected normal path.");
                VerifyHash(Root + entry.path, entry.sha256);
                VerifyHash(Root + entry.normalPath, entry.normalSha256);
                string meta = File.ReadAllText(Root + entry.path + ".meta").Replace("\r\n", "\n");
                if (string.IsNullOrEmpty(entry.guid) || !Guid.TryParseExact(entry.guid, "N", out _) || !meta.Contains("guid: " + entry.guid + "\n"))
                    throw new InvalidOperationException("New sprite metadata differs: " + entry.path);
                result.Add(entry.path, entry);
            }
        }

        private static CharacterDefinition FindCharacter(CharacterDefinition[] roster, string prefix)
        {
            CharacterDefinition result = null;
            foreach (CharacterDefinition character in roster)
            {
                if (character == null || character.name == null || !(character.name == prefix || character.name.StartsWith(prefix + " /", StringComparison.Ordinal))) continue;
                if (result != null) throw new InvalidOperationException("Ambiguous callsign: " + prefix);
                result = character;
            }
            if (result == null) throw new InvalidOperationException("Required existing callsign missing: " + prefix);
            return result;
        }

        private static void RequireUnassignedOrOwned(SideSpriteAnimation previous, string path)
        {
            if (previous == null || previous.frames == null) return;
            foreach (Sprite frame in previous.frames) RequireUnassignedOrOwned(frame, path);
        }

        private static void RequireUnassignedOrOwned(Sprite previous, string path)
        {
            if (previous != null && AssetDatabase.GetAssetPath(previous) != path)
                throw new InvalidOperationException("Custom art preserved; nothing assigned: " + AssetDatabase.GetAssetPath(previous));
        }

        private static bool SameAnimation(SideSpriteAnimation first, SideSpriteAnimation second)
        {
            if (first == second) return true;
            if (first == null || second == null || first.facesRight != second.facesRight || first.framesPerSecond != second.framesPerSecond
                || first.frames == null || second.frames == null || first.frames.Length != second.frames.Length) return false;
            for (int index = 0; index < first.frames.Length; index++) if (first.frames[index] != second.frames[index]) return false;
            return true;
        }

        private static SideSpriteAnimation Animation(Sprite sprite, bool facesRight)
        {
            return new SideSpriteAnimation { frames = new[] { sprite }, facesRight = facesRight, framesPerSecond = 10 };
        }

        private static void ConfigureTexture(TextureImporter importer)
        {
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
        }

        private static void VerifyHash(string path, string expected)
        {
            if (string.IsNullOrEmpty(expected) || expected.Length != 64 || !File.Exists(path)) throw new InvalidOperationException("Missing reviewed file/hash: " + path);
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
                if (BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != expected)
                    throw new InvalidOperationException("Reviewed file hash differs: " + path);
        }
    }
}
#endif
