using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

// AssetOrganizer
// Unity Editor extension for organizing assets by type or folder structure.
// Supports moving or copying assets based on type or original folder.
//
// Forked from Dreadrith's AssetOrganizer (MIT License).
// Modified and maintained by Neuru5278.
// GitHub: https://github.com/Neuru5278/AssetOrganizer

namespace com.neuru5278.assetorganizer
{
    /// <summary>
    /// Main EditorWindow for asset organization.
    /// </summary>
    public class AssetOrganizer : EditorWindow
    {
        #region Declarations
        #region Constants
        /// <summary>
        /// Encoding for YAML formatted files.
        /// </summary>
        private static Encoding Encoding { get { return Encoding.GetEncoding("UTF-8"); } }
        private const string PrefsKey = "AvatarAssetOrganizerSettings";
        // Asset type definitions for organization
        private static readonly ManageType[] ManageTypes =
        {
            new ManageType(0, "Animations", typeof(AnimationClip), typeof(BlendTree)),
            new ManageType(1, "Controllers", typeof(AnimatorController), typeof(AnimatorOverrideController)),
            new ManageType(2, "Textures", typeof(Texture), typeof(Texture2D), typeof(RenderTexture), typeof(Cubemap)),
            new ManageType(3, "Materials", typeof(Material)),
            new ManageType(4, "Models", new string[] {".fbx",".obj", ".dae", ".3ds", ".dxf", ".blend"}, typeof(Mesh)),
            new ManageType(5, "Prefabs", new string[] {".prefab"}, typeof(GameObject)),
            new ManageType(6, "Audio", typeof(AudioClip)),
            new ManageType(7, "Masks", typeof(AvatarMask)),
            new ManageType(8, "Scenes", new string[] {".unity"}, typeof(SceneAsset)),
            new ManageType(9, "Presets", typeof(Preset)),
            new ManageType(10, "VRC", "VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters, VRCSDK3A","VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu, VRCSDK3A"),
            new ManageType(11, "Shaders", typeof(Shader), typeof(ComputeShader)),
            new ManageType(12, "Scripts", new string[] {".dll", ".cs"}, typeof(MonoScript)),
            new ManageType(13, "Fonts", typeof(Font)),
            new ManageType(14, "Physics", typeof(PhysicMaterial), typeof(PhysicsMaterial2D)),
            new ManageType(15, "Lighting", typeof(LightingDataAsset)),
            new ManageType(16, "Other", typeof(ScriptableObject))
        };

        private static readonly string[] mainTabs = { "Organizer", "Options" };
        private static readonly string[] optionTabs = { "Folders", "Types" };

        private enum ManageAction
        {
            Skip,
            Move,
            Copy
        }

        private enum SortOptions
        {
            AlphabeticalPath,
            AlphabeticalAsset,
            AssetType
        }
        #endregion
        #region Automated Variables
        private static int mainToolbarIndex;
        private static int optionsToolbarIndex;
        private static DependencyAsset[] assets;
        private static List<string> createdFolders = new List<string>();
        private Vector2 scrollview;
        private Vector2 assetListScroll;
        private Vector2 pathHorizontalScrollPos;
        private float visiblePathWidth; // Stores the actual width of the path area.
        private float pathAreaX;        // Stores the starting X-coordinate of the path area.
        #endregion
        #region Input
        private static Object mainAsset;
        private static string destinationPath;
        private static string copySuffix;
        private static string folderSuffix = "_copy";

        [SerializeField] private List<CustomFolder> specialFolders;
        [SerializeField] private ManageAction[] typeActions;
        [SerializeField] private SortOptions sortByOption;
        [SerializeField] private bool deleteEmptyFolders = true;
        #endregion
        #endregion

        #region Methods
        #region Main Methods
        private void OnGUI()
        {
            mainToolbarIndex = GUILayout.Toolbar(mainToolbarIndex, mainTabs);

            switch (mainToolbarIndex)
            {
                case 0:
                    DrawOrganizerTab();
                    break;
                case 1:
                    scrollview = EditorGUILayout.BeginScrollView(scrollview);
                    DrawOptionsTab();
                    EditorGUILayout.EndScrollView();
                    break;
            }
            DrawSeparator();
        }
        private void GetDependencyAssets()
        {
            destinationPath = AssetDatabase.GetAssetPath(mainAsset);
            bool isFolder = AssetDatabase.IsValidFolder(destinationPath);
            string[] assetsPath = isFolder ? GetAssetPathsInFolder(destinationPath).ToArray() : AssetDatabase.GetDependencies(destinationPath);
            assets = assetsPath.Select(p => new DependencyAsset(p)).ToArray();

            if (!isFolder) destinationPath = destinationPath.Replace('\\', '/').Substring(0, destinationPath.LastIndexOf('/'));

            foreach (var a in assets)
            {
                string[] subFolders = a.path.Split('/');
                
                if (!TrySetAction(a))
                    a.associatedType = ManageTypes.Last();
                
                foreach (var f in specialFolders)
                {
                    if (!f.active) continue;
                    if (subFolders.All(s => s != f.name)) continue;

                    a.action = f.action;
                    break;

                }
            }

            switch (sortByOption)
            {
                case SortOptions.AlphabeticalPath:
                    assets = assets.OrderBy(a => a.path).ToArray();
                    break;
                case SortOptions.AlphabeticalAsset:
                    assets = assets.OrderBy(a => a.asset.name).ToArray();
                    break;
                case SortOptions.AssetType:
                    assets = assets.OrderBy(a => a.type.Name).ToArray();
                    break;
            }

        }
        /// <summary>
        /// Organizes assets while keeping the original folder structure.
        /// </summary>
        private void Organize()
        {
            CheckFolders();
            List<string> affectedFolders = new List<string>();
            var assetPathMap = new Dictionary<string, string>();
            var assetPathGUIDMap = new Dictionary<string, string>();
            var GUIDMap = new Dictionary<string, string>();
            try
            {
                AssetDatabase.StartAssetEditing();
                int count = assets.Length;
                float progressPerAsset = 1f / count;
                for (var i = 0; i < count; i++)
                {
                    EditorUtility.DisplayProgressBar("Organizing", $"Organizing Assets ({i + 1}/{count})", (i + 1) * progressPerAsset);
                    var a = assets[i];
                    string newPath = AssetDatabase.GenerateUniqueAssetPath($"{destinationPath}/{a.associatedType.name}/{Path.GetFileName(a.path)}");
                    string copyPath = AssetDatabase.GenerateUniqueAssetPath($"{destinationPath}/{a.associatedType.name}/{Path.GetFileNameWithoutExtension(a.path)}{copySuffix}{Path.GetExtension(a.path)}");
                    switch (a.action)
                    {
                        default: case ManageAction.Skip: continue;
                        case ManageAction.Move:
                            AssetDatabase.MoveAsset(a.path, newPath);
                            affectedFolders.Add(Path.GetDirectoryName(a.path));
                            break;
                        case ManageAction.Copy:
                            AssetDatabase.CopyAsset(a.path, copyPath);
                            assetPathMap.Add(a.path, copyPath);
                            assetPathGUIDMap.Add(a.path, a.guid);
                            break;
                    }
                }

            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var kvp in assetPathMap)
                {
                    var assetPath = kvp.Key;
                    var copyPath = kvp.Value;
                    GUIDMap.Add(AssetDatabase.AssetPathToGUID(assetPath), AssetDatabase.AssetPathToGUID(copyPath));
                }
                foreach (var kvp in assetPathMap)
                {
                    var assetPath = kvp.Key;
                    var copyPath = kvp.Value;

                    // Update GUID references in YAML files.
                    using (StreamReader sr = new StreamReader(assetPath, Encoding))
                    {
                        string s = sr.ReadToEnd();
                        if (s.StartsWith("%YAML"))
                        {
                            foreach (var originalAssetPath in assetPathMap.Keys)
                            {
                                var originalAssetGUID = assetPathGUIDMap[originalAssetPath];
                                var copyAssetGUID = GUIDMap[originalAssetGUID];
                                s = s.Replace(originalAssetGUID, copyAssetGUID);
                            }

                            using (StreamWriter sw = new StreamWriter(copyPath, false, Encoding))
                            {
                                sw.Write(s);
                            }
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var folderPath in createdFolders.Concat(affectedFolders).Distinct().Where(DirectoryIsEmpty))
                    AssetDatabase.DeleteAsset(folderPath);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(destinationPath));

            assets = null;
            destinationPath = null;
        }
        /// <summary>
        /// Organizes assets while keeping the original folder structure.
        /// </summary>
        private void Work()
        {
            string dirMainAsset = AssetDatabase.GetAssetPath(mainAsset);
            CheckFolders2(dirMainAsset);
            List<string> affectedFolders = new List<string>();
            var assetPathMap = new Dictionary<string, string>();
            var assetPathGUIDMap = new Dictionary<string, string>();
            var GUIDMap = new Dictionary<string, string>();
            try
            {
                AssetDatabase.StartAssetEditing();
                int count = assets.Length;
                float progressPerAsset = 1f / count;
                for (var i = 0; i < count; i++)
                {
                    EditorUtility.DisplayProgressBar("working", $"working Assets ({i + 1}/{count})", (i + 1) * progressPerAsset);
                    var a = assets[i];
                    string dirA = Path.GetDirectoryName(a.path).Replace("\\", "/");
                    string dirMiddle = dirA.Replace(dirMainAsset, "");
                    string newPath = AssetDatabase.GenerateUniqueAssetPath($"{destinationPath}/{dirMiddle}/{Path.GetFileName(a.path)}");
                    string copyPath = AssetDatabase.GenerateUniqueAssetPath($"{destinationPath}/{dirMiddle}/{Path.GetFileNameWithoutExtension(a.path)}{copySuffix}{Path.GetExtension(a.path)}");
                    switch (a.action)
                    {
                        default: case ManageAction.Skip: continue;
                        case ManageAction.Move:
                            AssetDatabase.MoveAsset(a.path, newPath);
                            affectedFolders.Add(Path.GetDirectoryName(a.path));
                            break;
                        case ManageAction.Copy:
                            AssetDatabase.CopyAsset(a.path, copyPath);
                            assetPathMap.Add(a.path, copyPath);
                            assetPathGUIDMap.Add(a.path, a.guid);
                            break;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var kvp in assetPathMap)
                {
                    var assetPath = kvp.Key;
                    var copyPath = kvp.Value;
                    GUIDMap.Add(AssetDatabase.AssetPathToGUID(assetPath), AssetDatabase.AssetPathToGUID(copyPath));
                }
                foreach (var kvp in assetPathMap)
                {
                    var assetPath = kvp.Key;
                    var copyPath = kvp.Value;

                    // Update GUID references in YAML files.
                    using (StreamReader sr = new StreamReader(assetPath, Encoding))
                    {
                        string s = sr.ReadToEnd();
                        if (s.StartsWith("%YAML"))
                        {
                            foreach (var originalAssetPath in assetPathMap.Keys)
                            {
                                var originalAssetGUID = assetPathGUIDMap[originalAssetPath];
                                var copyAssetGUID = GUIDMap[originalAssetGUID];
                                s = s.Replace(originalAssetGUID, copyAssetGUID);
                            }

                            using (StreamWriter sw = new StreamWriter(copyPath, false, Encoding))
                            {
                                sw.Write(s);
                            }
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var folderPath in createdFolders.Concat(affectedFolders).Distinct().Where(DirectoryIsEmpty))
                    AssetDatabase.DeleteAsset(folderPath);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(destinationPath));

            assets = null;
            destinationPath = null;
        }
        #endregion
        #region GUI Methods

        private void DrawOrganizerTab()
        {
            // Top input area
            using (new GUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                mainAsset = EditorGUILayout.ObjectField("Main Asset", mainAsset, typeof(Object), false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (mainAsset)
                    {
                        destinationPath = AssetDatabase.GetAssetPath(mainAsset);
                        if (!AssetDatabase.IsValidFolder(destinationPath)) destinationPath = Path.GetDirectoryName(destinationPath).Replace('\\', '/');
                    }
                    assets = null;
                }
                using (new EditorGUI.DisabledScope(!mainAsset))
                    if (GUILayout.Button("Get Assets", GUILayout.Width(80)))
                        GetDependencyAssets();
            }
            using (new GUILayout.HorizontalScope())
            {
                copySuffix = EditorGUILayout.TextField("Copy Suffix", copySuffix);
            }
            destinationPath = AssetFolderPath(destinationPath, "Destination Folder");
            using (new GUILayout.HorizontalScope())
            {
                folderSuffix = EditorGUILayout.TextField("Folder Suffix", folderSuffix);
            }

            // Asset list display area
            if (assets != null)
            {
                DrawSeparator(4, 20);

                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label($"Dependencies ({assets.Length}):", EditorStyles.boldLabel);

                    // Calculate the width of the longest path among all paths.
                    float maxPathWidth = 0f;
                    GUIStyle pathStyleForCalc = new GUIStyle(GUI.skin.label);
                    if (assets.Length > 0)
                    {
                        foreach (var a in assets)
                        {
                            float width = pathStyleForCalc.CalcSize(new GUIContent($"| {a.path}")).x;
                            if (width > maxPathWidth) maxPathWidth = width;
                        }
                    }

                    assetListScroll = EditorGUILayout.BeginScrollView(assetListScroll, GUILayout.ExpandHeight(true));

                    GUIStyle pathStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Overflow
                    };

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

                        // Store the X-coordinate and width of the path area in variables.
                        // These values are used when drawing the scrollbar after the loop.
                        pathAreaX = pathRect.x;
                        visiblePathWidth = pathRect.width;

                        GUI.Label(iconRect, a.icon);
                        a.action = (ManageAction)EditorGUI.EnumPopup(actionBoxRect, a.action);

                        GUI.BeginClip(pathRect);
                        Rect scrolledPathContentRect = new Rect(-pathHorizontalScrollPos.x, 0, maxPathWidth, pathRect.height);
                        if (GUI.Button(scrolledPathContentRect, $"| {a.path}", pathStyle))
                        {
                            EditorGUIUtility.PingObject(a.asset);
                        }
                        GUI.EndClip();
                    }

                    EditorGUILayout.EndScrollView();

                    // Render scrollbar only if content width exceeds visible width.
                    if (maxPathWidth > visiblePathWidth)
                    {
                        // Allocate vertical space for the scrollbar using GUILayout.
                        Rect scrollbarRect = EditorGUILayout.GetControlRect(false, 18); // 18 is the default height of a scrollbar

                        // Adjust the allocated space to precisely match the path area (pathRect).
                        scrollbarRect.x = pathAreaX;
                        scrollbarRect.width = visiblePathWidth;

                        // Draw a common horizontal scrollbar in the adjusted Rect.
                        pathHorizontalScrollPos.x = GUI.HorizontalScrollbar(
                            scrollbarRect,
                            pathHorizontalScrollPos.x,
                            visiblePathWidth,
                            0,
                            maxPathWidth
                        );
                    }
                }

                // Bottom action buttons area
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
                    DrawSeparator(0, 2);

                    if (GUILayout.Button("Organize by Type"))
                        Organize();

                    bool isFolder = mainAsset != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(mainAsset));
                    if (isFolder)
                    {
                        DrawSeparator(0, 2);
                        if (GUILayout.Button("Keep Structure"))
                            Work();
                    }
                }
            }
        }

        private void DrawOptionsTab()
        {
            optionsToolbarIndex = GUILayout.Toolbar(optionsToolbarIndex, optionTabs);
            switch (optionsToolbarIndex)
            {
                case 0:
                    DrawFolderOptions();
                    DrawSeparator();
                    using (new GUILayout.VerticalScope("helpbox"))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            using (new GUILayout.HorizontalScope("helpbox"))
                            {
                                if (GUILayout.Button("Move => Copy"))
                                {
                                    foreach (var f in specialFolders)
                                    {
                                        if (f.action == ManageAction.Move)
                                            f.action = ManageAction.Copy;
                                    }
                                }
                            }
                            using (new GUILayout.HorizontalScope("helpbox"))
                            {
                                if (GUILayout.Button("Copy => Move"))
                                {
                                    foreach (var f in specialFolders)
                                    {
                                        if (f.action == ManageAction.Copy)
                                            f.action = ManageAction.Move;
                                    }
                                }
                            }
                        }
                        DrawSeparator(0, 2);
                        using (new GUILayout.HorizontalScope())
                        {
                            deleteEmptyFolders = EditorGUILayout.Toggle(new GUIContent("Delete Empty Folders", "After moving assets, delete source folders if they're empty"), deleteEmptyFolders);
                            sortByOption = (SortOptions)EditorGUILayout.EnumPopup("Sort Search By", sortByOption);
                        }
                    }
                    break;
                case 1:
                    DrawTypeOptions();
                    DrawSeparator();
                    using (new GUILayout.VerticalScope("helpbox"))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            using (new GUILayout.HorizontalScope("helpbox"))
                            {
                                if (GUILayout.Button("Move => Copy"))
                                {
                                    for (int i = 0; i < ManageTypes.Length; i++)
                                    {
                                        if (typeActions[ManageTypes[i].actionIndex] == ManageAction.Move)
                                        {
                                            typeActions[ManageTypes[i].actionIndex] = ManageAction.Copy;
                                        }
                                    }
                                }
                            }
                            using (new GUILayout.HorizontalScope("helpbox"))
                            {
                                if (GUILayout.Button("Copy => Move"))
                                {
                                    for (int i = 0; i < ManageTypes.Length; i++)
                                    {
                                        if (typeActions[ManageTypes[i].actionIndex] == ManageAction.Copy)
                                        {
                                            typeActions[ManageTypes[i].actionIndex] = ManageAction.Move;
                                        }
                                    }
                                }
                            }
                        }
                        DrawSeparator(0, 2);
                        using (new GUILayout.HorizontalScope())
                        {
                            deleteEmptyFolders = EditorGUILayout.Toggle(new GUIContent("Delete Empty Folders", "After moving assets, delete source folders if they're empty"), deleteEmptyFolders);
                            sortByOption = (SortOptions)EditorGUILayout.EnumPopup("Sort Search By", sortByOption);
                        }
                    }
                    break;
            }
        }
        private void DrawFolderOptions()
        {
            for (var i = 0; i < specialFolders.Count; i++)
            {
                var f = specialFolders[i];
                using (new GUILayout.HorizontalScope("helpbox"))
                {
                    using (new BGColoredScope(Color.green, Color.grey, f.active))
                        f.active = GUILayout.Toggle(f.active, f.active ? "Enabled" : "Disabled", GUI.skin.button, GUILayout.Width(100), GUILayout.Height(18));
                    using (new EditorGUI.DisabledScope(!f.active))
                    {
                        f.name = GUILayout.TextField(f.name);
                        f.action = (ManageAction)EditorGUILayout.EnumPopup(f.action, GUILayout.Width(60));
                        if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(18)))
                            specialFolders.RemoveAt(i);
                    }
                }
            }

            if (GUILayout.Button("Add"))
                specialFolders.Add(new CustomFolder());
        }

        private void DrawTypeOptions()
        {
            using (new GUILayout.HorizontalScope())
            {
                void DrawTypeGUI(ManageType t)
                {
                    var icon = GUIContent.none;
                    if (t.associatedTypes.Length > 0)
                        icon = new GUIContent(AssetPreview.GetMiniTypeThumbnail(t.associatedTypes[0]));

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(icon, GUILayout.Height(18), GUILayout.Width(18));
                        GUILayout.Label($"| {t.name}");
                        if (TryGetTypeAction(t, out _))
                            typeActions[t.actionIndex] = (ManageAction)EditorGUILayout.EnumPopup(typeActions[t.actionIndex], GUILayout.Width(60));
                    }
                }

                using (new GUILayout.VerticalScope("helpbox"))
                {
                    for (int i = 0; i < ManageTypes.Length; i += 2)
                        DrawTypeGUI(ManageTypes[i]);
                }
                using (new GUILayout.VerticalScope("helpbox"))
                {
                    for (int i = 1; i < ManageTypes.Length; i += 2)
                        DrawTypeGUI(ManageTypes[i]);
                }
            }
        }

        private static string AssetFolderPath(string variable, string title)
        {
            using (new GUILayout.HorizontalScope())
            {
                // Create drag-and-drop area
                Rect dropArea = EditorGUILayout.GetControlRect();
                variable = EditorGUI.TextField(dropArea, title, variable);

                // Handle drag-and-drop
                Event currentEvent = Event.current;
                
                if (dropArea.Contains(currentEvent.mousePosition))
                {
                    switch (currentEvent.type)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                            if (DragAndDrop.paths.Length > 0)
                            {
                                string draggedPath = DragAndDrop.paths[0];
                                
                                // Check if it's a folder
                                if (AssetDatabase.IsValidFolder(draggedPath))
                                {
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                    
                                    if (currentEvent.type == EventType.DragPerform)
                                    {
                                        DragAndDrop.AcceptDrag();
                                        variable = draggedPath;
                                        GUI.changed = true;
                                    }
                                }
                                else
                                {
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                                }
                            }
                            currentEvent.Use();
                            break;
                    }
                }

                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    var dummyPath = EditorUtility.OpenFolderPanel(title, AssetDatabase.IsValidFolder(variable) ? variable : "Assets", string.Empty);
                    if (string.IsNullOrEmpty(dummyPath))
                        return variable;
                    string newPath = FileUtil.GetProjectRelativePath(dummyPath);

                    if (!newPath.StartsWith("Assets"))
                    {
                        // Warn if the selected path is not inside the Assets folder.
                        Debug.LogWarning("Selected path must be inside the Assets folder.");
                        return variable;
                    }

                    variable = newPath;
                }
            }

            return variable;
        }
        private static void DrawSeparator(int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(thickness + padding));
            r.height = thickness;
            r.y += padding / 2f;
            r.x -= 2;
            r.width += 6;
            ColorUtility.TryParseHtmlString(EditorGUIUtility.isProSkin ? "#595959" : "#858585", out Color lineColor);
            EditorGUI.DrawRect(r, lineColor);
        }
        #endregion

        #region Sub-Main Methods
        
        [MenuItem("Tools/NeuruTools/Asset Organizer", false, 36)]
        private static void ShowWindow()
        {
            var wnd = GetWindow<AssetOrganizer>(false, "Asset Organizer", true);
            wnd.minSize = new Vector2(420, 320);
        }

        private bool TrySetAction(DependencyAsset a)
        {
            for (int i = 0; i < ManageTypes.Length; i++)
            {
                if (!ManageTypes[i].IsAppliedTo(a)) continue;

                if (TryGetTypeAction(ManageTypes[i], out var action))
                {
                    a.action = action;
                    a.associatedType = ManageTypes[i];
                    return true;
                }
            }
            return false;
        }

        private bool TryGetTypeAction(ManageType type, out ManageAction action)
        {
            bool hasDoubleTried = false;
            TryAgain:
            try
            {
                action = typeActions[type.actionIndex];
                return true;
            }
            catch (Exception)
            {
                if (hasDoubleTried) throw;

                ManageAction[] newArray = new ManageAction[ManageTypes.Length];
                for (int j = 0; j < newArray.Length; j++)
                {
                    try { newArray[j] = typeActions[j]; }
                    catch { newArray[j] = ManageAction.Skip; }
                }

                // Warn if typeActions array is re-initialized due to serialization issues.
                Debug.LogWarning("TypeActions array was re-initialized due to serialization issues.");
                typeActions = newArray;
                hasDoubleTried = true;
                goto TryAgain;
            }
        }

        private static void CheckFolders()
        {
            if (!destinationPath.StartsWith("Assets/"))
                destinationPath = "Assets/" + destinationPath;
    
            // Apply folder suffix
            string finalDestinationPath = destinationPath;
            if (!string.IsNullOrEmpty(folderSuffix))
            {
                finalDestinationPath = $"{destinationPath}{folderSuffix}";
            }
    
            ReadyPath(finalDestinationPath);
            destinationPath = finalDestinationPath; // Update destination path

            createdFolders.Clear();

            void CheckFolder(string name)
            {
                string path = $"{destinationPath}/{name}";
                if (ReadyPath(path)) createdFolders.Add(path);
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < ManageTypes.Length; i++)
                    CheckFolder(ManageTypes[i].name);
            }
            finally { AssetDatabase.StopAssetEditing(); }
        }


        private static void CheckFolders2(string dirMainAsset)
        {
            if (!destinationPath.StartsWith("Assets/"))
                destinationPath = "Assets/" + destinationPath;
    
            // Apply folder suffix
            string finalDestinationPath = destinationPath;
            if (!string.IsNullOrEmpty(folderSuffix))
            {
                finalDestinationPath = $"{destinationPath}{folderSuffix}";
            }
    
            ReadyPath(finalDestinationPath);
            destinationPath = finalDestinationPath; // Update destination path
    
            createdFolders.Clear();

            void CheckFolder2(string name)
            {
                string path = $"{destinationPath}/{name}";
                if (ReadyPath(path)) createdFolders.Add(path);
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                int count = assets.Length;
                float progressPerAsset = 1f / count;
                for (var i = 0; i < count; i++)
                {
                    EditorUtility.DisplayProgressBar("checking", $"checking Assets ({i + 1}/{count})", (i + 1) * progressPerAsset);
                    var a = assets[i];
                    string dirA = Path.GetDirectoryName(a.path).Replace("\\", "/");
                    string dirMiddle = dirA.Replace(dirMainAsset, "");
                    CheckFolder2(dirMiddle);
                }
            }
            finally { EditorUtility.ClearProgressBar(); AssetDatabase.StopAssetEditing(); }
        }

        public static void DeleteIfEmptyFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                folderPath = Path.GetDirectoryName(folderPath);
            while (DirectoryIsEmpty(folderPath) && folderPath != "Assets")
            {
                var parentDirectory = Path.GetDirectoryName(folderPath);
                FileUtil.DeleteFileOrDirectory(folderPath);
                FileUtil.DeleteFileOrDirectory(folderPath + ".meta");
                folderPath = parentDirectory;
            }
        }
        public static bool DirectoryIsEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();
        #endregion
        #region Automated Methods
        /// <summary>
        /// Called when the window is enabled. Loads settings from EditorPrefs or sets defaults.
        /// </summary>
        private void OnEnable()
        {
            string data = EditorPrefs.GetString(PrefsKey, JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(data, this);
            if (!EditorPrefs.HasKey(PrefsKey))
            {
                // Default folder-based actions.
                specialFolders = new List<CustomFolder>
                {
                    new CustomFolder("__Generated", ManageAction.Copy),
                    new CustomFolder("VRCSDK"),
                    new CustomFolder("Packages"),
                    new CustomFolder("Plugins"),
                    new CustomFolder("Editor")
                };

                // Default type-based actions.
                typeActions = new ManageAction[]
                {
                    ManageAction.Copy, // 0: Animations
                    ManageAction.Copy, // 1: Controllers  
                    ManageAction.Copy, // 2: Textures
                    ManageAction.Copy, // 3: Materials
                    ManageAction.Copy, // 4: Models
                    ManageAction.Copy, // 5: Prefabs
                    ManageAction.Copy, // 6: Audio
                    ManageAction.Copy, // 7: Masks
                    ManageAction.Copy, // 8: Scenes
                    ManageAction.Skip, // 9: Presets
                    ManageAction.Copy, // 10: VRC
                    ManageAction.Skip, // 11: Shaders
                    ManageAction.Skip, // 12: Scripts
                    ManageAction.Copy, // 13: Fonts
                    ManageAction.Copy, // 14: Physics
                    ManageAction.Copy, // 15: Lighting
                    ManageAction.Copy, // 16: Other
                };
            }

            createdFolders = new List<string>();
        }

        /// <summary>
        /// Called when the window is disabled. Saves settings to EditorPrefs.
        /// </summary>
        private void OnDisable()
        {
            string data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(PrefsKey, data);
        }

        /// <summary>
        /// Ensures the specified folder path exists in the project.
        /// </summary>
        private static bool ReadyPath(string folderPath)
        {
            if (Directory.Exists(folderPath)) return false;

            Directory.CreateDirectory(folderPath);
            AssetDatabase.ImportAsset(folderPath);
            return true;
        }

        /// <summary>
        /// Recursively gets all asset paths in a folder (excluding .meta files).
        /// </summary>
        public static List<string> GetAssetPathsInFolder(string path, bool deep = true)
        {
            string[] fileEntries = Directory.GetFiles(path);
            string[] subDirectories = deep ? AssetDatabase.GetSubFolders(path) : null;

            List<string> list =
                (from fileName in fileEntries
                    where !fileName.EndsWith(".meta")
                          && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(fileName.Replace('\\', '/')))
                    select fileName.Replace('\\', '/')
                ).ToList(); // Filter out .meta files and ensure valid GUID.
            if (deep)
                foreach (var sd in subDirectories)
                    list.AddRange(GetAssetPathsInFolder(sd));

            return list;
        }
        #endregion
        #region Helper Methods
        #endregion
        #endregion

        #region Classes & Structs

        /// <summary>
        /// Represents a user-defined folder with a specific action.
        /// </summary>
        [System.Serializable]
        private class CustomFolder
        {
            public string name;
            public bool active = true;
            public ManageAction action;
            public CustomFolder() { }
            public CustomFolder(string newName, ManageAction action = ManageAction.Skip)
            {
                name = newName;
                this.action = action;
            }
        }
        /// <summary>
        /// Represents an asset and its metadata for organization.
        /// </summary>
        private class DependencyAsset
        {
            public readonly Object asset;
            public readonly string path;
            public readonly string guid;
            public readonly Type type;
            public readonly GUIContent icon;
            public ManageAction action;
            public ManageType associatedType;

            public DependencyAsset(string path)
            {
                this.path = path;
                asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                guid = AssetDatabase.AssetPathToGUID(path);
                action = ManageAction.Skip;
                type = asset.GetType();
                icon = new GUIContent(AssetPreview.GetMiniTypeThumbnail(type), type.Name);
            }
        }
        /// <summary>
        /// Defines a type of asset to manage, with associated types and extensions.
        /// </summary>
        private readonly struct ManageType
        {
            public readonly int actionIndex;
            public readonly string name;
            public readonly Type[] associatedTypes;
            private readonly string[] associatedExtensions;

            public ManageType(int actionIndex, string name)
            {
                this.actionIndex = actionIndex;
                this.name = name;
                this.associatedTypes = Array.Empty<Type>();
                this.associatedExtensions = Array.Empty<string>();
            }
            public ManageType(int actionIndex, string name, params string[] associatedTypes)
            {
                this.actionIndex = actionIndex;
                this.name = name;
                this.associatedTypes = new Type[associatedTypes.Length];
                for (int i = 0; i < associatedTypes.Length; i++)
                    this.associatedTypes[i] = System.Type.GetType(associatedTypes[i]);

                this.associatedExtensions = Array.Empty<string>();
            }

            public ManageType(int actionIndex, string name, params Type[] associatedTypes)
            {
                this.actionIndex = actionIndex;
                this.name = name;

                this.associatedTypes = associatedTypes;
                this.associatedExtensions = Array.Empty<string>();
            }

            public ManageType(int actionIndex, string name, string[] associatedExtensions, params string[] associatedTypes)
            {
                this.actionIndex = actionIndex;
                this.name = name;

                this.associatedTypes = new Type[associatedTypes.Length];
                for (int i = 0; i < associatedTypes.Length; i++)
                    this.associatedTypes[i] = System.Type.GetType(associatedTypes[i]);

                this.associatedExtensions = associatedExtensions;
            }

            public ManageType(int actionIndex, string name, string[] associatedExtensions, params Type[] associatedTypes)
            {
                this.actionIndex = actionIndex;
                this.name = name;

                int count = associatedTypes.Length;
                this.associatedTypes = associatedTypes;
                this.associatedExtensions = associatedExtensions;
            }

            public bool IsAppliedTo(DependencyAsset a)
            {
                bool applies = a.type != null &&
                               (associatedTypes.Any(t => t != null && (a.type == t || a.type.IsSubclassOf(t)))
                                || associatedExtensions.Any(e => !string.IsNullOrWhiteSpace(e) && a.path.EndsWith(e)));

                return applies;
            }

        }
        /// <summary>
        /// Utility for drawing colored backgrounds in the GUI.
        /// </summary>
        private class BGColoredScope : System.IDisposable
        {
            private readonly Color ogColor;
            public BGColoredScope(Color setColor)
            {
                ogColor = GUI.backgroundColor;
                GUI.backgroundColor = setColor;
            }
            public BGColoredScope(Color setColor, bool isActive)
            {
                ogColor = GUI.backgroundColor;
                GUI.backgroundColor = isActive ? setColor : ogColor;
            }
            public BGColoredScope(Color active, Color inactive, bool isActive)
            {
                ogColor = GUI.backgroundColor;
                GUI.backgroundColor = isActive ? active : inactive;
            }

            public BGColoredScope(int selectedIndex, params Color[] colors)
            {
                ogColor = GUI.backgroundColor;
                GUI.backgroundColor = colors[selectedIndex];
            }
            public void Dispose()
            {
                GUI.backgroundColor = ogColor;
            }
        }
        #endregion
    }
}