#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class BossAscensionArtSetup
    {
        private const string Root = "Assets/Art/Production/BossAscensionV2/";
        [Serializable] private sealed class Manifest { public string version; public Entry[] assets; }
        [Serializable] private sealed class Entry
        {
            public string path, kind, sha256, normal, normalSha256;
            public int group, slot, frame;
            public float pixelsPerUnit;
        }

        [MenuItem("Tools/Mori Echoes/Production Art/Apply Boss Ascension Art")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "art-manifest.json"));
            if (manifest == null || manifest.version != "boss-ascension-v2" || manifest.assets == null || manifest.assets.Length != 128)
                throw new InvalidOperationException("Expected 96 physical ornaments and 32 ultimate frames.");
            foreach (Entry entry in manifest.assets)
            {
                Verify(Root + entry.path, entry.sha256);
                if (!string.IsNullOrEmpty(entry.normal)) Verify(Root + entry.normal, entry.normalSha256);
            }
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null || config.themes.Length < 8) throw new InvalidOperationException("At least eight configured stages required.");
            Directory.CreateDirectory("ArtProduction/boss-ascension-v2/config-backups");
            string backup = "ArtProduction/boss-ascension-v2/config-backups/BeforeApply_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".asset";
            File.Copy(MoriEchoesSetup.ConfigPath, backup, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (Entry entry in manifest.assets)
            {
                Texture2D normal = null;
                if (!string.IsNullOrEmpty(entry.normal))
                {
                    var normalImporter = (TextureImporter)AssetImporter.GetAtPath(Root + entry.normal);
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    Configure(normalImporter); normalImporter.SaveAndReimport();
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + entry.normal);
                }
                var importer = (TextureImporter)AssetImporter.GetAtPath(Root + entry.path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.pixelsPerUnit;
                importer.alphaIsTransparency = true;
                Configure(importer);
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = Vector2.one * 0.5f;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                if (normal != null) importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } };
                importer.SaveAndReimport();
            }
            Sprite Read(string kind, int group, int frame, int slot)
            {
                Entry entry = manifest.assets.Single(item => item.kind == kind && item.group == group && item.frame == frame && item.slot == slot);
                return AssetDatabase.LoadAssetAtPath<Sprite>(Root + entry.path) ?? throw new InvalidOperationException(entry.path);
            }
            Undo.RecordObject(config, "Apply generated physical boss equipment and ultimate art");
            for (int stage = 0; stage < 8; stage++)
            {
                int group = stage;
                config.themes[stage].bossOrnaments = Enumerable.Range(0, 12).Select(index => Read("ornament", group, index / 4, index % 4)).ToArray();
                config.themes[stage].bossUltimateFrames = Enumerable.Range(0, 4).Select(frame => Read("ultimate", group, frame, 0)).ToArray();
            }
            config.bossSkillDamageMultiplier = 2.25f; config.bossUltimateDamageMultiplier = 3.25f;
            config.bossSkillAreaScale = 1.65f; config.bossUltimateAreaScale = 1.9f;
            config.bossDamageReduction = 0.62f; config.bossMaximumHitFraction = 0.008f; config.bossHealthMultiplier = 1.5f;
            if (!config.Validate(out string reason)) throw new InvalidOperationException(reason);
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("BOSS_ASCENSION_ART_APPLIED | sprites=128 | physicalParts=96 | ultimateFrames=32 | stages=8 | backup=" + backup);
        }

        public static void ApplyAndBuild() { Apply(); MoriEchoesSetup.BuildWindows(); }

        private static void Configure(TextureImporter importer)
        {
            importer.mipmapEnabled = false; importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        private static void Verify(string path, string expected)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
                if (BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != expected)
                    throw new InvalidOperationException("Art hash mismatch: " + path);
        }
    }
}
#endif
