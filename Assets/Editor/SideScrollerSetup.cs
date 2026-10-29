#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MoriEchoes.EditorTools
{
    public static class SideScrollerSetup
    {
        [MenuItem("Tools/Mori Echoes/Side Scroller/Prepare Config")]
        public static void Prepare()
        {
            MoriEchoesSetup.CheckEnvironment();
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null) throw new InvalidOperationException("Existing GameConfig missing. No asset was overwritten.");
            config.sideScroller = true;
            if (config.characters == null || config.characters.Length == 0) config.characters = CharacterDefinition.Defaults();
            if (config.themes == null || config.themes.Length == 0) config.themes = ThemeProfile.Defaults();
            // Append only the two authorized aquatic stages; retain all eight existing profiles and art bindings.
            if (config.themes.Length == 8)
            {
                ThemeProfile[] defaults = ThemeProfile.Defaults();
                Array.Resize(ref config.themes, 10);
                config.themes[8] = defaults[8]; config.themes[9] = defaults[9];
                Debug.Log("Appended storm sea and abyss stages; original eight profiles preserved.");
            }
            const string path = "Assets/Settings/MoriEchoesSideSpriteLit.mat";
            if (config.sideSpriteMaterial == null)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Lit material path occupied; preserved.");
                    Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                    if (shader == null) throw new InvalidOperationException("URP 2D Lit shader unavailable.");
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                config.sideSpriteMaterial = material;
            }
            if (config.sideLightPrefab == null)
            {
                const string prefabPath = "Assets/Prefabs/Effects/SideNormalLight.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(prefabPath) != null) throw new InvalidOperationException("Light prefab path occupied.");
                    if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Effects")) AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
                    var temporary = new GameObject("SideNormalLight");
                    try
                    {
                        Light2D light = temporary.AddComponent<Light2D>(); light.lightType = Light2D.LightType.Point;
                        var settings = new SerializedObject(light);
                        settings.FindProperty("m_NormalMapQuality").intValue = (int)Light2D.NormalMapQuality.Accurate;
                        settings.FindProperty("m_NormalMapDistance").floatValue = 1.5f;
                        settings.ApplyModifiedPropertiesWithoutUndo();
                        prefab = PrefabUtility.SaveAsPrefabAsset(temporary, prefabPath);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(temporary); }
                }
                config.sideLightPrefab = prefab;
            }
            if (!config.Validate(out string reason)) throw new InvalidOperationException(reason);
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            Debug.Log("Side scroller config prepared. Existing scenes and legacy sprite material preserved.");
        }

        [MenuItem("Tools/Mori Echoes/Side Scroller/Build Windows")]
        public static void BuildWindows()
        {
            Prepare(); MoriEchoesSetup.BuildWindows();
        }
    }
}
#endif
