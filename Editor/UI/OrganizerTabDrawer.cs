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

            // --- Top Input Fields (Matches original DrawOrganizerTab) ---
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

            using (new GUILayout.HorizontalScope())
            {
                settings.copySuffix = EditorGUILayout.TextField("Copy Suffix", settings.copySuffix);
            }
            destinationPath = AssetOrganizerGUI.AssetFolderPath(destinationPath, "Destination Folder");
            using (new GUILayout.HorizontalScope())
            {
                settings.folderSuffix = EditorGUILayout.TextField("Folder Suffix", settings.folderSuffix);
            }

            // --- Asset List (Matches original DrawOrganizerTab) ---
            if (assets != null && assets.Count > 0)
            {
                AssetOrganizerGUI.DrawSeparator(4, 20);
                DrawAssetList(assets);
                DrawActionButtons(assets, mainAsset, actions);
            }

            return actions;
        }

        private void DrawAssetList(List<DependencyAsset> assets)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label($"Dependencies ({assets.Count}):", EditorStyles.boldLabel);
                
                float maxPathWidth = 0f;
                GUIStyle pathStyleForCalc = new GUIStyle(GUI.skin.label);
                if (assets.Count > 0)
                {
                    foreach (var a in assets)
                    {
                        float width = pathStyleForCalc.CalcSize(new GUIContent($"| {a.path}")).x;
                        if (width > maxPathWidth) maxPathWidth = width;
                    }
                }

                _assetListScroll = EditorGUILayout.BeginScrollView(_assetListScroll, GUILayout.ExpandHeight(true));

                GUIStyle pathStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Overflow
                };

                float visiblePathWidth = 0;
                float pathAreaX = 0;

                foreach (var a in assets)
                {
                    Rect fullRowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4);
                    GUI.Box(fullRowRect, GUIContent.none, GUI.skin.box);
                    Rect paddedRect = new Rect(fullRowRect.x + 2, fullRowRect.y + 2, fullRowRect.width - 4, fullRowRect.height - 4);

                    const float actionBoxWidth = 60f;
                    const float spacing = 4f;
                    float iconWidth = paddedRect.height;

                    Rect actionBoxRect = new Rect(paddedRect.x + paddedRect.width - actionBoxWidth, paddedRect.y, actionBoxWidth, paddedRect.height);
                    Rect iconRect = new Rect(paddedRect.x, paddedRect.y, iconWidth, paddedRect.height);
                    Rect pathRect = new Rect(iconRect.xMax + spacing, paddedRect.y, actionBoxRect.x - (iconRect.xMax + spacing), paddedRect.height);
                    
                    pathAreaX = pathRect.x;
                    visiblePathWidth = pathRect.width;

                    GUI.Label(iconRect, a.icon);
                    a.action = (ManageAction)EditorGUI.EnumPopup(actionBoxRect, a.action);

                    GUI.BeginClip(pathRect);
                    Rect scrolledPathContentRect = new Rect(-_pathHorizontalScrollPos.x, 0, maxPathWidth, pathRect.height);
                    if (GUI.Button(scrolledPathContentRect, $"| {a.path}", pathStyle))
                    {
                        EditorGUIUtility.PingObject(a.asset);
                    }
                    GUI.EndClip();
                }

                EditorGUILayout.EndScrollView();

                if (maxPathWidth > visiblePathWidth)
                {
                    Rect scrollbarRect = EditorGUILayout.GetControlRect(false, 18);
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
                AssetOrganizerGUI.DrawSeparator(0, 2);

                if (GUILayout.Button("Organize by Type"))
                {
                    actions.ProcessByType = true;
                }

                bool isFolder = mainAsset != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(mainAsset));
                if (isFolder)
                {
                    AssetOrganizerGUI.DrawSeparator(0, 2);
                    if (GUILayout.Button("Keep Structure"))
                    {
                        actions.ProcessByStructure = true;
                    }
                }
            }
        }
    }
} 