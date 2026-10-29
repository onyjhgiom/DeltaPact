#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class SkillArtSetup
    {
        private const string Root = "Assets/Art/Production/SkillsV1/";
        [Serializable] private sealed class Manifest { public string version; public string status; public Entry[] assets; }
        [Serializable] private sealed class Entry { public string path; public string sha256; }

        [MenuItem("Tools/Mori Echoes/Production Art/Import Reviewed Character Skills")]
        public static void ImportReviewed()
        {
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "art-manifest.json"));
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null || !config.Validate(out _) || manifest == null || manifest.version != "skills-v1" || manifest.status != "visually-reviewed" || manifest.assets == null || manifest.assets.Length != 4)
                throw new InvalidOperationException("Four reviewed skill sprites and valid config required.");
            for (int index = 0; index < 4; index++)
            {
                Entry entry = manifest.assets[index];
                if (entry.path != "skill-" + index + ".png") throw new InvalidOperationException("Unexpected skill image path.");
                using (var hash = SHA256.Create())
                using (var stream = File.OpenRead(Root + entry.path))
                    if (BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != entry.sha256)
                        throw new InvalidOperationException("Skill image hash differs.");
                Sprite previous = config.characters[index].skillSprite;
                if (previous != null && AssetDatabase.GetAssetPath(previous) != Root + entry.path) throw new InvalidOperationException("Custom skill art preserved.");
            }
            Directory.CreateDirectory("Validation");
            File.Copy(MoriEchoesSetup.ConfigPath, "Validation/SkillConfig_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".asset", false);
            AssetDatabase.Refresh();
            var sprites = new Sprite[4];
            for (int index = 0; index < 4; index++)
            {
                string path = Root + manifest.assets[index].path;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing skill texture importer.");
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 512; importer.spritePivot = Vector2.one * 0.5f;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
                sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[index] == null) throw new InvalidOperationException("Sprite import failed.");
            }
            Undo.RecordObject(config, "Assign character skill art");
            for (int index = 0; index < 4; index++) config.characters[index].skillSprite = sprites[index];
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            Debug.Log("Four dedicated character skill sprites imported; config backed up.");
        }
    }
}
#endif
