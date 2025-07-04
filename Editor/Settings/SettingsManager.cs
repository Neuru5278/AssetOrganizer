using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.neuru5278.assetorganizer.Settings
{
    public static class SettingsManager
    {
        private const string SettingsFileName = "AssetOrganizerSettings.asset";
        private const string SettingsFolderPath = "Assets/Editor Default Resources/AssetOrganizer";

        public static AssetOrganizerSettings GetSettings()
        {
            var settingsPath = Path.Combine(SettingsFolderPath, SettingsFileName);
            var settings = AssetDatabase.LoadAssetAtPath<AssetOrganizerSettings>(settingsPath);

            if (settings == null)
            {
                if (!Directory.Exists(SettingsFolderPath))
                {
                    Directory.CreateDirectory(SettingsFolderPath);
                }

                settings = ScriptableObject.CreateInstance<AssetOrganizerSettings>();
                settings.ResetToDefaults();
                
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
                
                Debug.Log("Asset Organizer: Created default settings asset.");
            }

            return settings;
        }
    }
} 