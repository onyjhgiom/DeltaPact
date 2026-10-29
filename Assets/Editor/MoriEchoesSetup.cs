#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoriEchoes.EditorTools
{
    public static class MoriEchoesSetup
    {
        public const string ConfigPath = "Assets/Settings/MoriEchoesConfig.asset";
        public const string MaterialPath = "Assets/Settings/MoriEchoesSpriteUnlit.mat";
        public static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Boot/Boot.unity",
            "Assets/Scenes/MainMenu/MainMenu.unity",
            "Assets/Scenes/Battle_01/Battle_01.unity"
        };

        [MenuItem("Tools/Mori Echoes/1. Check Environment")]
        public static void CheckEnvironment()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before running setup/build tools.");
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!Directory.Exists(Path.Combine(project, "ProjectSettings")) || !File.Exists(Path.Combine(project, "Packages", "manifest.json")))
                throw new InvalidOperationException("Not a complete Unity project. Create a Universal 2D project in Unity Hub first.");
            if (!Application.unityVersion.StartsWith("6000.", StringComparison.Ordinal))
                throw new InvalidOperationException("This source targets Unity 6 (6000.x). Do not upgrade another project automatically.");
#if ENABLE_INPUT_SYSTEM
            var inputPackage = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(InputSystem).Assembly);
            Debug.Log("Mori Echoes | Unity " + Application.unityVersion + " | Input System " + (inputPackage != null ? inputPackage.version : "version unavailable")
                + " | Project: " + project);
#else
            throw new InvalidOperationException("Install Input System through Package Manager; set Active Input Handling to Input System Package (New), then restart Unity.");
#endif
        }

        [MenuItem("Tools/Mori Echoes/2. Prepare Prototype Scenes")]
        public static void PreparePrototypeScenes()
        {
            CheckEnvironment();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets/Settings");
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(ConfigPath);
            GameConfig config = existing as GameConfig;
            if (existing != null && config == null)
                throw new InvalidOperationException("Config path is occupied by another asset; it was not overwritten: " + ConfigPath);
            if (config == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) throw new InvalidOperationException("Universal 2D sprite shader missing. Use the Universal 2D template and let packages finish importing.");
                UnityEngine.Object materialAsset = AssetDatabase.LoadMainAssetAtPath(MaterialPath);
                Material material = materialAsset as Material;
                if (materialAsset != null && material == null) throw new InvalidOperationException("Material path is occupied; nothing was overwritten: " + MaterialPath);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, MaterialPath);
                }
                config = ScriptableObject.CreateInstance<GameConfig>();
                config.spriteMaterial = material;
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            string reason;
            if (!config.Validate(out reason)) throw new InvalidOperationException(reason + " Existing config was preserved.");
            if (config.spriteMaterial == null) throw new InvalidOperationException("Existing GameConfig has no Sprite Material. Assign a URP 2D Unlit material manually; existing config was not changed.");
            Scene originalActive = SceneManager.GetActiveScene();
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene loaded = SceneManager.GetSceneAt(index);
                if (!loaded.isLoaded || !string.IsNullOrEmpty(loaded.path)) continue;
                EnsureFolder("Assets/Scenes/SetupRecovery");
                string recovery = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/SetupRecovery/RecoveredUntitled.unity");
                if (!EditorSceneManager.SaveScene(loaded, recovery)) throw new IOException("Cannot preserve untitled scene before additive setup.");
                Debug.Log("Preserved untitled scene before setup: " + recovery);
            }
            foreach (string path in ScenePaths)
            {
                if (File.Exists(path))
                {
                    Debug.Log("Preserved existing scene without changes: " + path);
                    continue;
                }
                EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    var root = new GameObject("MoriEchoesBootstrap");
                    var bootstrap = root.AddComponent<GameBootstrap>();
                    bootstrap.config = config;
                    bootstrap.startInMenu = path != ScenePaths[2];
                    EditorUtility.SetDirty(bootstrap);
                    if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Scene could not be saved: " + path);
                    Debug.Log("Created prototype scene: " + path);
                }
                finally
                {
                    if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            VerifySceneBindings();
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in ScenePaths)
            {
                if (!buildScenes.Exists(item => item.path == path)) buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Prototype source setup completed. No pre-existing scene/config was overwritten. Open Main Menu next. Runtime play tests are still required.");
        }

        [MenuItem("Tools/Mori Echoes/3. Open Main Menu")]
        public static void OpenMainMenu()
        {
            if (!File.Exists(ScenePaths[1])) throw new FileNotFoundException("Run Prepare Prototype Scenes first.", ScenePaths[1]);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePaths[1], OpenSceneMode.Single);
        }

        [MenuItem("Tools/Mori Echoes/4. Build Windows x64 Prototype")]
        public static void BuildWindows()
        {
            CheckEnvironment();
            if (EditorApplication.isCompiling) throw new BuildFailedException("Wait for compilation to finish before building.");
            foreach (string path in ScenePaths)
                if (!File.Exists(path)) throw new BuildFailedException("Required scene missing: " + path + ". Run Prepare Prototype Scenes first.");
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            string reason;
            if (config == null) throw new BuildFailedException("GameConfig asset missing.");
            if (!config.Validate(out reason)) throw new BuildFailedException("Invalid GameConfig: " + reason);
            if (config.spriteMaterial == null) throw new BuildFailedException("Assign an Unlit Sprite Material to GameConfig so its shader is included in the build.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new BuildFailedException("Install Windows Build Support for this exact Unity Editor version through Unity Hub.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            VerifySceneBindings();
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string folder = Path.GetFullPath(Path.Combine("Builds", "Windows", "MoriEchoes_" + stamp));
            if (Directory.Exists(folder)) throw new IOException("Refusing to reuse an existing build directory: " + folder);
            Directory.CreateDirectory(folder);
            string executable = Path.Combine(folder, "MoriEchoes.exe");
            var options = new BuildPlayerOptions
            {
                scenes = ScenePaths,
                target = BuildTarget.StandaloneWindows64,
                locationPathName = executable,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool exists = File.Exists(executable);
            string reportFolder = Path.GetFullPath(Path.Combine("docs", "BuildReports"));
            Directory.CreateDirectory(reportFolder);
            string reportPath = Path.Combine(reportFolder, "Windows_" + stamp + ".md");
            string status = report.summary.result.ToString();
            var content = new StringBuilder();
            content.AppendLine("# Windows 构建记录");
            content.AppendLine("- 时间：" + DateTime.Now.ToString("O"));
            content.AppendLine("- Unity：" + Application.unityVersion);
            content.AppendLine("- 目标：StandaloneWindows64 / Development");
            content.AppendLine("- BuildPipeline 结果：" + status);
            content.AppendLine("- 错误 / 警告：" + report.summary.totalErrors + " / " + report.summary.totalWarnings);
            content.AppendLine("- 输出：`" + executable + "`");
            content.AppendLine("- EXE 实际存在：" + exists);
            content.AppendLine("- 耗时：" + report.summary.totalTime);
            content.AppendLine("- 包体字节：" + report.summary.totalSize);
            content.AppendLine("- 游戏运行与完整流程：未由本工具自动测试，必须人工验收。");
            bool animeArt = config.sideScroller && config.characters != null && config.characters.Length > 0 && config.characters[0].animation != null && config.characters[0].animation.IsReady;
            content.AppendLine(animeArt ? "- 已绑定生成动漫素材；平台/机关仍有程序化表现。画面质量需运行检查。" : "- 程序化占位素材；画面质量需运行检查。");
            content.AppendLine(config.uiFont != null ? "- UI 字体已绑定：" + AssetDatabase.GetAssetPath(config.uiFont) + "；字体嵌入设置由打包步骤检查。" : "- 中文 OS 字体回退需要在目标机检查，推荐构建前指定合法嵌入字体。");
            File.WriteAllText(reportPath, content.ToString(), new UTF8Encoding(false));
            string progress = Path.Combine("docs", "PROGRESS.md");
            if (File.Exists(progress)) File.AppendAllText(progress, "\n\n## " + DateTime.Now.ToString("O") + " / L：实际构建\n\n- BuildPipeline: "
                + status + "; EXE exists: " + exists + "\n- Report: `" + reportPath + "`\n- 运行验收：未执行。\n", new UTF8Encoding(false));
            if (report.summary.result != BuildResult.Succeeded || !exists)
                throw new BuildFailedException("Windows build failed. Read Console and " + reportPath);
            Debug.Log("Windows build created (not runtime-tested): " + executable + "\nReport: " + reportPath);
        }

        [MenuItem("Tools/Mori Echoes/Verify Scene Bindings")]
        public static void VerifySceneBindings()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before verifying scene assets.");
            Scene originalActive = SceneManager.GetActiveScene();
            foreach (string path in ScenePaths)
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Required scene missing.", path);
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var bootstraps = new List<GameBootstrap>();
                    foreach (GameObject root in scene.GetRootGameObjects()) bootstraps.AddRange(root.GetComponentsInChildren<GameBootstrap>(true));
                    if (bootstraps.Count != 1 || bootstraps[0].config == null)
                        throw new InvalidOperationException("Scene must contain exactly one configured GameBootstrap: " + path + ". Existing scene was not overwritten.");
                    string reason;
                    if (!bootstraps[0].config.Validate(out reason)) throw new InvalidOperationException(path + ": " + reason);
                    if (bootstraps[0].config.spriteMaterial == null)
                        throw new InvalidOperationException("Assign Sprite Material in the config used by: " + path);
                    if (bootstraps[0].startInMenu != (path != ScenePaths[2]))
                        throw new InvalidOperationException("Start In Menu must be enabled for Boot/MainMenu and disabled for Battle_01: " + path);
                    if (!bootstraps[0].gameObject.activeInHierarchy || !bootstraps[0].enabled)
                        throw new InvalidOperationException("GameBootstrap must be enabled on an active root: " + path);
                }
                finally
                {
                    if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
                    if (opened) EditorSceneManager.CloseScene(scene, true);
                }
            }
            Debug.Log("Scene asset bindings verified. This does not constitute Play Mode testing.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) throw new InvalidOperationException("Invalid Assets folder: " + path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
