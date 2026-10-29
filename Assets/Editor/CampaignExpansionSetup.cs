#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoriEchoes.EditorTools
{
    public static class CampaignExpansionSetup
    {
        [MenuItem("Tools/Mori Echoes/Expand Campaign to Eight Stages")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Expand campaign data outside Play Mode.");

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(MoriEchoesSetup.ConfigPath);
            if (config == null) throw new InvalidOperationException("Existing MoriEchoesConfig asset is required.");
            if (config.characters == null || config.characters.Length < 4 || config.characters.Length > 12
                || config.themes == null || config.themes.Length < 5 || config.themes.Length > 8
                || config.weapons == null || config.weapons.Length < 3 || config.weapons.Length > 9)
                throw new InvalidOperationException("Existing config arrays are outside supported limits; no asset was changed.");
            foreach (CharacterDefinition character in config.characters)
                if (character == null) throw new InvalidOperationException("An existing character entry is missing; config preserved.");
            foreach (ThemeProfile theme in config.themes)
                if (theme == null) throw new InvalidOperationException("An existing stage theme is missing; config preserved.");
            foreach (WeaponDefinition weapon in config.weapons)
                if (weapon == null) throw new InvalidOperationException("An existing weapon entry is missing; config preserved.");

            GameConfig defaults = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                bool changed = config.characters.Length < 12 || config.themes.Length < 8 || config.weapons.Length < 9
                    || config.weaponSprites == null || config.weaponSprites.Length < 9
                    || config.weaponViewSprites == null || config.weaponViewSprites.Length < 9;
                if (!changed)
                {
                    if (!config.Validate(out string validReason)) throw new InvalidOperationException(validReason);
                    Debug.Log("Campaign already contains eight stages, twelve characters and nine weapons; no changes made.");
                    return;
                }

                string backupFolder = "Validation/CampaignBackups";
                Directory.CreateDirectory(backupFolder);
                string backup = Path.Combine(backupFolder, "MoriEchoesConfig_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".asset");
                File.Copy(MoriEchoesSetup.ConfigPath, backup, false);
                Undo.RecordObject(config, "Expand campaign content");

                int characterCount = config.characters.Length;
                Array.Resize(ref config.characters, 12);
                for (int index = characterCount; index < config.characters.Length; index++)
                    config.characters[index] = defaults.characters[index];

                int themeCount = config.themes.Length;
                Array.Resize(ref config.themes, 8);
                for (int index = themeCount; index < config.themes.Length; index++)
                    config.themes[index] = defaults.themes[index];

                int weaponCount = config.weapons.Length;
                Array.Resize(ref config.weapons, 9);
                for (int index = weaponCount; index < config.weapons.Length; index++)
                    config.weapons[index] = defaults.weapons[index];

                Array.Resize(ref config.weaponSprites, 9);
                Array.Resize(ref config.weaponViewSprites, 9);
                config.allCharactersAvailable = true;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
                if (!config.Validate(out string reason)) throw new InvalidOperationException(reason + " Backup: " + backup);
                Debug.Log("Expanded campaign to eight stages, twelve characters and nine weapons. Existing entries and sprite references were preserved. Backup: " + backup);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(defaults);
            }
        }
    }
}
#endif
