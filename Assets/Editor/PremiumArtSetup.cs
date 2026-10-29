#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class PremiumArtSetup
    {
        private const string Root = "Assets/Art/Production/PremiumV1/";
        [Serializable] private sealed class Manifest { public string version; public Entry[] assets; }
        [Serializable] private sealed class Entry
        {
            public string path, kind, sha256, normal, normalSha256;
            public int group, slot, frame;
            public float pixelsPerUnit;
        }

        [MenuItem("Tools/Mori Echoes/Production Art/Apply Premium Campaign Art")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "art-manifest.json"));
            if (manifest == null || manifest.version != "descent-premium-v1" || manifest.assets == null)
                throw new InvalidOperationException("Premium art manifest missing.");
            foreach (Entry entry in manifest.assets)
            {
                Verify(Root + entry.path, entry.sha256);
                if (!string.IsNullOrEmpty(entry.normal)) Verify(Root + entry.normal, entry.normalSha256);
            }
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null || config.themes.Length != 8) throw new InvalidOperationException("Eight-stage existing campaign required.");
            string[] callsigns = { "DELTA", "VANGUARD", "WRAITH", "HAVOC", "KALT", "VEIL", "SERA", "ROOK" };
            CharacterDefinition[] roster = callsigns.Select(key => config.characters.Single(character => character.name.StartsWith(key + " /", StringComparison.Ordinal))).ToArray();
            Directory.CreateDirectory("ArtProduction/descent-premium-v1/config-backups");
            string backup = "ArtProduction/descent-premium-v1/config-backups/BeforeApply_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".asset";
            File.Copy(MoriEchoesSetup.ConfigPath, backup, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (Entry entry in manifest.assets)
            {
                Texture2D normal = null;
                if (!string.IsNullOrEmpty(entry.normal))
                {
                    var normalImporter = (TextureImporter)AssetImporter.GetAtPath(Root + entry.normal);
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    Configure(normalImporter);
                    normalImporter.SaveAndReimport();
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + entry.normal);
                }
                var importer = (TextureImporter)AssetImporter.GetAtPath(Root + entry.path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.pixelsPerUnit;
                importer.alphaIsTransparency = entry.kind != "background";
                Configure(importer);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = Vector2.one * 0.5f;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                if (normal != null) importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } };
                importer.SaveAndReimport();
            }
            Sprite Read(string kind, int group, int slot = 0, int frame = 0)
            {
                Entry entry = manifest.assets.Single(item => item.kind == kind && item.group == group && item.slot == slot && item.frame == frame);
                return AssetDatabase.LoadAssetAtPath<Sprite>(Root + entry.path) ?? throw new InvalidOperationException(entry.path);
            }
            SideSpriteAnimation Animation(string kind, int group, int slot, bool facesRight)
            {
                int count = manifest.assets.Count(item => item.kind == kind && item.group == group && item.slot == slot);
                return new SideSpriteAnimation { facesRight = facesRight, framesPerSecond = 10,
                    frames = Enumerable.Range(0, count).Select(frame => Read(kind, group, slot, frame)).ToArray() };
            }
            Undo.RecordObject(config, "Apply generated premium campaign artwork");
            config.characters = roster;
            for (int index = 4; index < roster.Length; index++)
            {
                roster[index].portrait = Read("portrait", index);
                roster[index].animation = Animation("character", index, 0, true);
                roster[index].poweredAnimation = Animation("powered", index, 0, true);
                roster[index].skillSprite = Read("skill", index);
                roster[index].ultimateSprite = Read("skill", index, 1);
            }
            Array.Resize(ref config.solidWallSprites, 8);
            for (int stage = 0; stage < 8; stage++)
            {
                ThemeProfile theme = config.themes[stage];
                theme.enemyAnimations = Enumerable.Range(0, 4).Select(kind => Animation("enemy", stage, kind, false)).ToArray();
                theme.encounterEffects = Enumerable.Range(0, 8).Select(slot => Read("effect", stage, slot)).ToArray();
                theme.architecture = Enumerable.Range(0, stage < 5 ? 6 : 4).Select(slot => Read("architecture", stage, slot)).ToArray();
                if (stage < 5) theme.segmentProps = new[] { Read("architecture", stage, 3), Read("architecture", stage, 4), Read("architecture", stage, 1) };
                if (stage < 5)
                {
                    theme.hazardAnimations = Enumerable.Range(0, 2).Select(slot => Animation("hazard", stage, slot, false)).ToArray();
                    theme.stationaryAnimation = Animation("hazard", stage, 2, false);
                }
                config.solidWallSprites[stage] = theme.architecture[1];
                if (stage >= 5)
                {
                    theme.background = Read("background", stage);
                    theme.bossAnimation = Animation("boss", stage, 0, false);
                }
                theme.bossForms = new[] { theme.bossAnimation, Animation("boss", stage, 1, false), Animation("boss", stage, 2, false) };
                if (stage < 5) theme.guardianSprites = Enumerable.Range(0, 4).Select(slot => Read("guardian", stage, slot % 2, slot / 2)).ToArray();
            }
            for (int index = 6; index < 9; index++)
            {
                config.weaponSprites[index] = Read("weapon", index);
                config.weaponViewSprites[index] = Read("view", index);
            }
            config.pickupSprites = Enumerable.Range(0, 8).Select(index => Read("pickup", index)).ToArray();
            for (int stage = 5; stage < 8; stage++) config.solidWallSprites[stage] = config.themes[stage].Architecture(1);
            config.incomingDamageMultiplier = 1.65f;
            config.stageDamageStep = 0.08f;
            config.allCharactersAvailable = true;
            if (!config.Validate(out string reason)) throw new InvalidOperationException(reason);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("PREMIUM_ART_APPLIED | sprites=" + manifest.assets.Length + " | roster=8 | stages=8 | backup=" + backup);
        }

        private static void Configure(TextureImporter importer)
        {
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        private static void Verify(string path, string expected)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
                if (BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != expected)
                    throw new InvalidOperationException("Art file hash mismatch: " + path);
        }
    }
}
#endif
