using System.Collections.Generic;
using System.IO;
using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Services;
using com.neuru5278.assetorganizer.Settings;
using com.neuru5278.assetorganizer.UI;
using UnityEditor;
using UnityEngine;
using FileUtil = UnityEditor.FileUtil;
using Object = UnityEngine.Object;

namespace com.neuru5278.assetorganizer
{
    public class AssetOrganizerWindow : EditorWindow
    {
        private AssetOrganizerSettings _settings;
        private OrganizerTabDrawer _organizerTabDrawer;
        
        private Object _mainAsset;
        private string _destinationPath;
        private List<DependencyAsset> _assets;

        [MenuItem("Tools/Asset Organizer")]
        public static void ShowWindow()
        {
            GetWindow<AssetOrganizerWindow>("Asset Organizer");
        }

        private void OnEnable()
        {
            _settings = SettingsManager.GetSettings();
            _organizerTabDrawer = new OrganizerTabDrawer();
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("Settings asset could not be loaded. Please check the console for errors.", MessageType.Error);
                if(GUILayout.Button("Retry Loading Settings")) OnEnable();
                return;
            }
            
            var userActions = _organizerTabDrawer.Draw(ref _mainAsset, ref _destinationPath, _settings, _assets);

            if (userActions.MainAssetChanged)
            {
                if (_mainAsset)
                {
                    _destinationPath = AssetDatabase.GetAssetPath(_mainAsset);
                    if (!AssetDatabase.IsValidFolder(_destinationPath))
                    {
                        _destinationPath = Path.GetDirectoryName(_destinationPath)?.Replace('\\', '/');
                    }
                }
                else
                {
                    _destinationPath = "Assets";
                }
                _assets = null;
            }

            if (userActions.GetAssets)
            {
                GetDependencyAssets();
            }

            if (userActions.ProcessByType)
            {
                var processor = new AssetProcessor(_settings);
                processor.ProcessAssets(_assets, _destinationPath, false);
                _assets = null;
            }
            
            if (userActions.ProcessByStructure)
            {
                var processor = new AssetProcessor(_settings);
                processor.ProcessAssets(_assets, _destinationPath, true);
                _assets = null;
            }
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_settings);
            }
        }

        private void GetDependencyAssets()
        {
            if (!_mainAsset) return;
            var analyzer = new DependencyAnalyzer(_settings);
            _assets = analyzer.RunAnalysis(_mainAsset);
        }
    }
} 