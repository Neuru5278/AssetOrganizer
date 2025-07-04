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
            _settings = AssetOrganizerSettings.LoadSettings();
            _organizerTabDrawer = new OrganizerTabDrawer();
        }

        private void OnGUI()
        {
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
            }
            
            if (userActions.ProcessByStructure)
            {
                var processor = new AssetProcessor(_settings);
                processor.ProcessAssets(_assets, _destinationPath, true);
            }
        }

        private void GetDependencyAssets()
        {
            if (!_mainAsset) return;
            var analyzer = new DependencyAnalyzer(_settings);
            _assets = analyzer.GetDependencyAssets(_mainAsset);
        }
    }
} 