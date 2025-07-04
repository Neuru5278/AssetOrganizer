using System.Collections.Generic;
using System.IO;
using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Services;
using com.neuru5278.assetorganizer.Settings;
using com.neuru5278.assetorganizer.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.neuru5278.assetorganizer.UI
{
    public class AssetOrganizerWindow : EditorWindow
    {
        // State
        private AssetOrganizerSettings _settings;
        private AssetOrganizationService _service;
        private OrganizerTabDrawer _organizerTabDrawer;
        private OptionsTabDrawer _optionsTabDrawer;

        private int _mainToolbarIndex;
        private readonly string[] _mainTabs = { "Organizer", "Options" };
        
        private Object _mainAsset;
        private string _mainAssetPath;
        private string _destinationPath;
        private List<DependencyAsset> _dependencyAssets = new List<DependencyAsset>();
        private Vector2 _scrollPosition;

        [MenuItem("Tools/NeuruTools/Asset Organizer", false, 36)]
        private static void ShowWindow()
        {
            var window = GetWindow<AssetOrganizerWindow>("Asset Organizer");
            window.minSize = new Vector2(420, 320);
            window.Show();
        }

        private void OnEnable()
        {
            _settings = SettingsManager.GetSettings();
            _service = new AssetOrganizationService(_settings);
            _organizerTabDrawer = new OrganizerTabDrawer();
            _optionsTabDrawer = new OptionsTabDrawer(_settings);
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("Settings asset could not be loaded. Please check the console for errors.", MessageType.Error);
                if(GUILayout.Button("Retry Loading Settings")) OnEnable();
                return;
            }
            
            _mainToolbarIndex = GUILayout.Toolbar(_mainToolbarIndex, _mainTabs);
            AssetOrganizerGUI.DrawSeparator();

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollView.scrollPosition;
                
                switch (_mainToolbarIndex)
                {
                    case 0:
                        DrawOrganizerTab();
                        break;
                    case 1:
                        _optionsTabDrawer.Draw();
                        break;
                }
            }

            // Save settings if they have been changed in the UI
            if (GUI.changed)
            {
                EditorUtility.SetDirty(_settings);
            }
        }

        private void DrawOrganizerTab()
        {
            // The OrganizerTabDrawer is now responsible for drawing all fields.
            // We pass the state to it and handle the returned actions.
            var actions = _organizerTabDrawer.Draw(ref _mainAsset, ref _destinationPath, _settings, _dependencyAssets);
            
            if(actions.MainAssetChanged) HandleMainAssetChange();
            
            HandleUserActions(actions);
        }
        
        private void HandleMainAssetChange()
        {
            if (_mainAsset == null)
            {
                _destinationPath = "Assets";
                _dependencyAssets.Clear();
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(_mainAsset);
                _destinationPath = AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path);
            }
        }

        private void HandleUserActions(OrganizerTabDrawer.UserActions actions)
        {
            if (actions.GetAssets)
            {
                _dependencyAssets = _service.RunAnalysis(_mainAsset, out _mainAssetPath);
            }

            if (actions.ProcessByType)
            {
                _service.RunProcessing(_dependencyAssets, _mainAssetPath, _destinationPath, ProcessMode.ByType);
                _dependencyAssets.Clear(); // Clear list after processing
            }

            if (actions.ProcessByStructure)
            {
                _service.RunProcessing(_dependencyAssets, _mainAssetPath, _destinationPath, ProcessMode.ByStructure);
                _dependencyAssets.Clear(); // Clear list after processing
            }
        }
    }
} 