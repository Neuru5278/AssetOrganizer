using System.Collections.Generic;
using System.IO;
using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Services;
using com.neuru5278.assetorganizer.Settings;
using com.neuru5278.assetorganizer.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.neuru5278.assetorganizer
{
    public class AssetOrganizerWindow : EditorWindow
    {
        private AssetOrganizerSettings _settings;
        private AssetOrganizationService _service;
        private OrganizerTabDrawer _organizerTabDrawer;
        
        private Object _mainAsset;
        private string _mainAssetPath;
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
            _service = new AssetOrganizationService(_settings);
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
                if (_mainAsset != null)
                    _assets = _service.RunAnalysis(_mainAsset, out _mainAssetPath);
            }

            if (userActions.ProcessByType)
            {
                _service.RunProcessing(_assets, _mainAssetPath, _destinationPath, ProcessMode.ByType);
                _assets = null;
            }
            
            if (userActions.ProcessByStructure)
            {
                _service.RunProcessing(_assets, _mainAssetPath, _destinationPath, ProcessMode.ByStructure);
                _assets = null;
            }
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_settings);
            }
        }
    }
} 