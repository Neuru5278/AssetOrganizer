using System.Collections.Generic;
using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Settings;
using com.neuru5278.assetorganizer.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.neuru5278.assetorganizer.UI
{
    public class OrganizerTabDrawer
    {
        private Vector2 _assetListScroll;
        private Vector2 _pathHorizontalScrollPos;

        public class UserActions
        {
            public bool GetAssets { get; set; }
            public bool ProcessByType { get; set; }
            public bool ProcessByStructure { get; set; }
            public bool MainAssetChanged { get; set; }
        }

        public UserActions Draw(ref Object mainAsset, ref string destinationPath, AssetOrganizerSettings settings, List<DependencyAsset> assets)
        {
            var actions = new UserActions();

            // --- Top Input Fields ---
            using (new GUILayout.HorizontalScope())
            {
                var newMainAsset = EditorGUILayout.ObjectField("Main Asset", mainAsset, typeof(Object), false);
                if (newMainAsset != mainAsset)
                {
                    mainAsset = newMainAsset;
                    actions.MainAssetChanged = true;
                }

                using (new EditorGUI.DisabledScope(mainAsset == null))
                {
                    if (GUILayout.Button("Get Assets", GUILayout.Width(80)))
                    {
                        actions.GetAssets = true;
                    }
                }
            }
            
            settings.copySuffix = EditorGUILayout.TextField("Copy Suffix", settings.copySuffix);
            destinationPath = AssetOrganizerGUI.AssetFolderPath(destinationPath, "Destination Folder");
            settings.folderSuffix = EditorGUILayout.TextField("Folder Suffix", settings.folderSuffix);
            
            // --- Asset List ---
            if (assets != null && assets.Count > 0)
            {
                AssetOrganizerGUI.DrawSeparator(2, 10);
                DrawAssetList(assets, settings);
                AssetOrganizerGUI.DrawSeparator(2, 10);
                DrawActionButtons(assets, mainAsset, actions);
            }

            return actions;
        }
        
        private void DrawAssetList(List<DependencyAsset> assets, AssetOrganizerSettings settings)
        {
            GUILayout.Label($"Dependencies ({assets.Count}):", EditorStyles.boldLabel);

            float maxPathWidth = 0f;
            GUIStyle pathStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Overflow };
            foreach (var asset in assets)
            {
                float width = pathStyle.CalcSize(new GUIContent($"| {asset.path}")).x;
                if (width > maxPathWidth) maxPathWidth = width;
            }

            _assetListScroll = EditorGUILayout.BeginScrollView(_assetListScroll, GUILayout.ExpandHeight(true));

            float visiblePathWidth = 0;
            float pathAreaX = 0;

            foreach (var asset in assets)
            {
                Rect fullRowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4);
                GUI.Box(fullRowRect, GUIContent.none, GUI.skin.box);
                Rect paddedRect = new Rect(fullRowRect.x + 2, fullRowRect.y + 2, fullRowRect.width - 4, fullRowRect.height - 4);

                const float actionBoxWidth = 60f;
                const float spacing = 4f;
                float iconWidth = paddedRect.height;

                Rect iconRect = new Rect(paddedRect.x, paddedRect.y, iconWidth, paddedRect.height);
                Rect actionBoxRect = new Rect(paddedRect.x + paddedRect.width - actionBoxWidth, paddedRect.y, actionBoxWidth, paddedRect.height);
                Rect pathRect = new Rect(iconRect.xMax + spacing, paddedRect.y, actionBoxRect.x - (iconRect.xMax + spacing), paddedRect.height);
                
                pathAreaX = pathRect.x;
                visiblePathWidth = pathRect.width;

                GUI.Label(iconRect, asset.icon);
                asset.action = (ManageAction)EditorGUI.EnumPopup(actionBoxRect, asset.action);

                GUI.BeginClip(pathRect);
                Rect scrolledPathContentRect = new Rect(-_pathHorizontalScrollPos.x, 0, maxPathWidth, pathRect.height);
                if (GUI.Button(scrolledPathContentRect, $"| {asset.path}", pathStyle))
                {
                    EditorGUIUtility.PingObject(asset.asset);
                }
                GUI.EndClip();
            }

            EditorGUILayout.EndScrollView();
            
            if (maxPathWidth > visiblePathWidth)
            {
                Rect scrollbarRect = GUILayoutUtility.GetRect(visiblePathWidth, 18);
                scrollbarRect.x = pathAreaX;
                scrollbarRect.width = visiblePathWidth;

                _pathHorizontalScrollPos.x = GUI.HorizontalScrollbar(
                    scrollbarRect,
                    _pathHorizontalScrollPos.x,
                    visiblePathWidth,
                    0,
                    maxPathWidth
                );
            }
        }
        
        private void DrawActionButtons(List<DependencyAsset> assets, Object mainAsset, UserActions actions)
        {
            using (new GUILayout.VerticalScope("helpbox"))
            {
                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.HorizontalScope("helpbox"))
                    {
                        if (GUILayout.Button("Move => Copy"))
                        {
                            foreach (var a in assets)
                            {
                                if (a.action == ManageAction.Move)
                                    a.action = ManageAction.Copy;
                            }
                        }
                    }
                    using (new GUILayout.HorizontalScope("helpbox"))
                    {
                        if (GUILayout.Button("Copy => Move"))
                        {
                            foreach (var a in assets)
                            {
                                if (a.action == ManageAction.Copy)
                                    a.action = ManageAction.Move;
                            }
                        }
                    }
                }
                AssetOrganizerGUI.DrawSeparator(1, 2);

                if (GUILayout.Button("Organize by Type"))
                {
                    actions.ProcessByType = true;
                }

                bool isFolder = mainAsset != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(mainAsset));
                if (isFolder)
                {
                    AssetOrganizerGUI.DrawSeparator(1, 2);
                    if (GUILayout.Button("Keep Structure"))
                    {
                        actions.ProcessByStructure = true;
                    }
                }
            }
        }
    }
} 