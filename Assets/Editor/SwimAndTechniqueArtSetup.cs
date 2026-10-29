#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class SwimAndTechniqueArtSetup
    {
        private const string SwimRoot = "Assets/Art/Production/SwimAnimeV2/";
        private const string TechniqueRoot = "Assets/Art/Production/BossTechniquesV1/";
        [Serializable] private sealed class Manifest { public string version; public Entry[] assets; }
        [Serializable] private sealed class Entry
        {
            public string path, kind, sha256, normal, normalSha256;
            public int stage, slot, frame;
            public float pixelsPerUnit;
        }
        [Serializable] private sealed class Catalog { public CatalogEntry[] entries; }
        [Serializable] private sealed class CatalogEntry { public int stage, slot; public string name, release, visual, dodge; }

        [MenuItem("Tools/Mori Echoes/Production Art/Apply Swim And Boss Techniques")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            Manifest swim = ReadManifest(SwimRoot, "swim-anime-v2", 86);
            Manifest techniques = ReadManifest(TechniqueRoot, "boss-techniques-v1", 60);
            var catalog = JsonUtility.FromJson<Catalog>("{\"entries\":" + File.ReadAllText("ArtProduction/boss-techniques-v1/catalog.json") + "}");
            if (catalog.entries.Length != 30) throw new InvalidOperationException("Expected thirty unique skills.");
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null || config.themes.Length != 10) throw new InvalidOperationException("Ten configured stages required.");
            string[] protectedStages = config.themes.Take(8).Select(JsonUtility.ToJson).ToArray();
            Directory.CreateDirectory("ArtProduction/swim-anime-v1/config-backups");
            File.Copy(MoriEchoesSetup.ConfigPath,"ArtProduction/swim-anime-v1/config-backups/BeforeApply_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".asset",false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Import(SwimRoot,swim); Import(TechniqueRoot,techniques);
            Sprite Read(string root, Manifest manifest, string kind, int stage, int slot, int frame)
            {
                Entry entry = manifest.assets.Single(item => item.kind == kind && item.stage == stage && item.slot == slot && item.frame == frame);
                return AssetDatabase.LoadAssetAtPath<Sprite>(root + entry.path) ?? throw new InvalidOperationException(entry.path);
            }
            for (int stage = 8; stage < 10; stage++)
            {
                ThemeProfile theme = config.themes[stage];
                theme.background = Read(SwimRoot,swim,"background",stage,0,0);
                theme.bossForms = Enumerable.Range(0,3).Select(slot => new SideSpriteAnimation { facesRight=false,framesPerSecond=8,
                    frames=Enumerable.Range(0,2).Select(frame=>Read(SwimRoot,swim,"boss",stage,slot,frame)).ToArray() }).ToArray();
                theme.bossAnimation = theme.bossForms[0];
                theme.enemyAnimations = Enumerable.Range(0,4).Select(slot=>new SideSpriteAnimation { facesRight=false,framesPerSecond=7,
                    frames=Enumerable.Range(0,2).Select(frame=>Read(SwimRoot,swim,"enemies",stage,slot,frame)).ToArray() }).ToArray();
                theme.architecture = Enumerable.Range(0,4).Select(slot=>Read(SwimRoot,swim,"architecture",stage,slot,0)).ToArray();
                theme.segmentProps = theme.architecture;
                theme.encounterEffects = Enumerable.Range(0,8).Select(slot=>Read(SwimRoot,swim,"effects",stage,slot,0)).ToArray();
                theme.bossOrnaments = Enumerable.Range(0,12).Select(slot=>Read(SwimRoot,swim,"ornaments",stage,slot,0)).ToArray();
                theme.bossUltimateFrames = Enumerable.Range(0,4).Select(slot=>Read(SwimRoot,swim,"ultimate",stage,slot,0)).ToArray();
            }
            for(int stage=0;stage<8;stage++)
                if(JsonUtility.ToJson(config.themes[stage])!=protectedStages[stage]) throw new InvalidOperationException("Protected original stage changed: "+stage);
            for(int stage=0;stage<10;stage++)
            {
                int current=stage;
                config.themes[stage].bossTechniques=Enumerable.Range(0,3).Select(slot=>
                {
                    CatalogEntry description=catalog.entries.Single(entry=>entry.stage==current && entry.slot==slot);
                    return new BossTechniqueProfile { name=description.name, release=description.release, visual=description.visual,dodge=description.dodge,
                        frames=Enumerable.Range(0,2).Select(frame=>Read(TechniqueRoot,techniques,"technique",current,slot,frame)).ToArray() };
                }).ToArray();
            }
            if(!config.Validate(out string reason)) throw new InvalidOperationException(reason);
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("SWIM_TECHNIQUE_ART_APPLIED | nativeAlphaSprites=144 | backgrounds=2 | skills=30 | stages=10 | original8ArtPreserved=true");
        }

        public static void ApplyAndBuild() { Apply(); MoriEchoesSetup.BuildWindows(); }

        private static Manifest ReadManifest(string root,string version,int count)
        {
            var result=JsonUtility.FromJson<Manifest>(File.ReadAllText(root+"art-manifest.json"));
            if(result==null || result.version!=version || result.assets.Length!=count) throw new InvalidOperationException("Art manifest invalid: "+root);
            foreach(Entry entry in result.assets)
            {
                Verify(root+entry.path,entry.sha256);
                if(!string.IsNullOrEmpty(entry.normal)) Verify(root+entry.normal,entry.normalSha256);
            }
            return result;
        }

        private static void Import(string root,Manifest manifest)
        {
            foreach(Entry entry in manifest.assets)
            {
                Texture2D normal=null;
                if(!string.IsNullOrEmpty(entry.normal))
                {
                    var normalImporter=(TextureImporter)AssetImporter.GetAtPath(root+entry.normal);
                    normalImporter.textureType=TextureImporterType.NormalMap;
                    Configure(normalImporter);normalImporter.SaveAndReimport();
                    normal=AssetDatabase.LoadAssetAtPath<Texture2D>(root+entry.normal);
                }
                var importer=(TextureImporter)AssetImporter.GetAtPath(root+entry.path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=entry.pixelsPerUnit; importer.alphaIsTransparency=true;
                Configure(importer);
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;
                settings.spritePivot=Vector2.one*.5f; settings.spriteGenerateFallbackPhysicsShape=false;
                importer.SetTextureSettings(settings);
                if(normal!=null) importer.secondarySpriteTextures=new[]{new SecondarySpriteTexture{name="_NormalMap",texture=normal}};
                importer.SaveAndReimport();
            }
        }

        private static void Configure(TextureImporter importer)
        {
            importer.mipmapEnabled=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
        }

        private static void Verify(string path,string expected)
        {
            using(var algorithm=SHA256.Create())
            using(var stream=File.OpenRead(path))
                if(BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=expected)
                    throw new InvalidOperationException("Art hash mismatch: "+path);
        }
    }
}
#endif
