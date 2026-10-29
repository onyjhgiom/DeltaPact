#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoriEchoes.EditorTools
{
    public static class MoriEchoesIntegration
    {
        public const string RendererPath = "Assets/Settings/MoriEchoesRenderer2D.asset";
        public const string PipelinePath = "Assets/Settings/MoriEchoesUniversal2D.asset";

        [MenuItem("Tools/Mori Echoes/0. Configure Empty Project and Prepare")]
        public static void ConfigureAndPrepare()
        {
            MoriEchoesSetup.CheckEnvironment();
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                var renderer = LoadOrReject<Renderer2DData>(RendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                    AssetDatabase.CreateAsset(renderer, RendererPath);
                    ResourceReloader.ReloadAllNullIn(renderer, "Packages/com.unity.render-pipelines.universal");
                    EditorUtility.SetDirty(renderer);
                }
                var pipeline = LoadOrReject<UniversalRenderPipelineAsset>(PipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    pipeline.supportsHDR = true;
                    pipeline.msaaSampleCount = 1;
                    AssetDatabase.CreateAsset(pipeline, PipelinePath);
                }
                GraphicsSettings.defaultRenderPipeline = pipeline;
                Debug.Log("Configured missing render pipeline with native URP 2D assets; existing pipelines were not overwritten.");
            }
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            MoriEchoesSetup.PreparePrototypeScenes();
            Debug.Log("MORI_INTEGRATION_READY | Unity " + Application.unityVersion + " | " + Path.GetFullPath("."));
        }

        private static T LoadOrReject<T>(string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && !(existing is T)) throw new InvalidOperationException("Asset path occupied; no overwrite: " + path);
            return existing as T;
        }
    }
}
#endif
