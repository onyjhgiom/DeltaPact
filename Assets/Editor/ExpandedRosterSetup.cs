#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class ExpandedRosterSetup
    {
        [MenuItem("Tools/Mori Echoes/Expand Character Roster to Eight")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Expand the roster outside Play Mode.");
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null) throw new InvalidOperationException("An existing MoriEchoesConfig asset is required.");
            if (config.characters != null && config.characters.Length >= 8)
            {
                Debug.Log("Roster already contains eight or more entries; existing character data and art preserved.");
                return;
            }
            if (config.characters == null || (config.characters.Length != 4 && config.characters.Length != 6))
                throw new InvalidOperationException("Only an existing four- or six-character roster can be expanded to eight.");
            foreach (CharacterDefinition character in config.characters)
                if (character == null) throw new InvalidOperationException("An original character is missing; the config was preserved.");
            CharacterDefinition[] defaults = CharacterDefinition.Defaults();
            int existingCount = config.characters.Length;
            var expanded = new CharacterDefinition[8];
            Array.Copy(config.characters, expanded, existingCount);
            for (int index = existingCount; index < expanded.Length; index++) expanded[index] = defaults[index];
            Undo.RecordObject(config, "Expand character roster to eight");
            config.characters = expanded;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("Expanded the roster to eight; all existing character definitions and art references are preserved.");
        }
    }
}
#endif
