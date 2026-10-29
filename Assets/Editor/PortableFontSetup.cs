#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class PortableFontSetup
    {
        public const string FontPath = "Assets/Fonts/NotoSansCJKsc-Regular.otf";

        [MenuItem("Tools/Mori Echoes/Build Portable Windows With Embedded Font")]
        public static void BuildWindows()
        {
            MoriEchoesSetup.CheckEnvironment();
            if (!File.Exists(FontPath) || !File.Exists("Assets/Fonts/OFL.txt"))
                throw new FileNotFoundException("The bundled font and its OFL license are required.");
            AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
            if (importer == null) throw new InvalidOperationException("Bundled font did not import as a TrueType/OpenType font.");
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.includeFontData = true;
            importer.fontNames = new[] { "Noto Sans CJK SC" };
            importer.SaveAndReimport();
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) throw new InvalidOperationException("Embedded font asset could not be loaded.");
            foreach (char character in "末日回响三角洲契约恢复传送关卡开始武器技能胜利失败0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ")
                if (!font.HasCharacter(character)) throw new InvalidOperationException("Bundled font is missing glyph: " + character);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null) throw new InvalidOperationException("Game configuration is missing.");
            if (config.uiFont != null && config.uiFont != font)
                throw new InvalidOperationException("A custom UI font is already assigned; it was preserved.");
            config.uiFont = font;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("Portable font ready: includeFontData=True; glyph probes passed; OS-font fallback is unnecessary for the configured UI.");
            MoriEchoesSetup.BuildWindows();
        }
    }
}
#endif
