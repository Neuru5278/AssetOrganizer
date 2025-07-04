using System.Collections.Generic;
using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Services;
using com.neuru5278.assetorganizer.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.neuru5278.assetorganizer.UI
{
    public class OrganizerTabDrawer
    {
        private Vector2 _assetListScroll;

        public class UserActions
        {
            public bool GetAssets { get; set; }
            public bool ProcessByType { get; set; }
            public bool ProcessByStructure { get; set; }
        }

        public UserActions Draw(Object mainAsset, List<DependencyAsset> assets, string destinationPath)
        {
            var actions = new UserActions();

            // Top input area
            using (new GUILayout.HorizontalScope())
            {
                var newMainAsset = EditorGUILayout.ObjectField("Main Asset", mainAsset, typeof(Object), false);
                if (newMainAsset != mainAsset)
                {
                    // This change needs to be handled by the main window
                }

                using (new EditorGUI.DisabledScope(mainAsset == null))
                {
                    if (GUILayout.Button("Get Assets", GUILayout.Width(80)))
                    {
                        actions.GetAssets = true;
                    }
                }
            }

            // Destination Path
            // This state should be managed by the main window.
            // destinationPath = AssetOrganizerGUI.AssetFolderPath(destinationPath, "Destination Folder");
            EditorGUILayout.LabelField("Destination Folder", destinationPath);


            // Asset list display area
            if (assets != null && assets.Count > 0)
            {
                AssetOrganizerGUI.DrawSeparator(1, 15);
                DrawAssetList(assets);
                AssetOrganizerGUI.DrawSeparator(1, 15);
                
                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("Actions", EditorStyles.boldLabel);
                    if (GUILayout.Button("Organize by Type"))
                    {
                        actions.ProcessByType = true;
                    }

                    bool isFolder = mainAsset != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(mainAsset));
                    if (isFolder)
                    {
                        if (GUILayout.Button("Keep Structure"))
                        {
                            actions.ProcessByStructure = true;
                        }
                    }
                }
            }

            return actions;
        }

        private void DrawAssetList(List<DependencyAsset> assets)
        {
            using (var scrollView = new EditorGUILayout.ScrollViewScope(_assetListScroll, GUILayout.ExpandHeight(true)))
            {
                _assetListScroll = scrollView.scrollPosition;
                
                GUILayout.Label($"Dependencies ({assets.Count}):", EditorStyles.boldLabel);

                foreach (var asset in assets)
                {
                    using (new GUILayout.HorizontalScope("box"))
                    {
                        GUILayout.Label(asset.icon, GUILayout.Width(20), GUILayout.Height(20));
                        
                        if (GUILayout.Button(new GUIContent(asset.path, asset.path), EditorStyles.label, GUILayout.ExpandWidth(true)))
                        {
                            EditorGUIUtility.PingObject(asset.asset);
                        }
                        
                        asset.action = (ManageAction)EditorGUILayout.EnumPopup(asset.action, GUILayout.Width(70));
                    }
                }
            }
        }
    }
} 