using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Settings;
using com.neuru5278.assetorganizer.Utils;
using UnityEditor;
using UnityEngine;

namespace com.neuru5278.assetorganizer.UI
{
    public class OptionsTabDrawer
    {
        private int _optionsToolbarIndex;
        private readonly string[] _optionTabs = { "Folders", "Types" };
        private readonly AssetOrganizerSettings _settings;

        public OptionsTabDrawer(AssetOrganizerSettings settings)
        {
            _settings = settings;
        }

        public void Draw()
        {
            _optionsToolbarIndex = GUILayout.Toolbar(_optionsToolbarIndex, _optionTabs);
            
            EditorGUILayout.Space();

            switch (_optionsToolbarIndex)
            {
                case 0:
                    DrawFolderOptions();
                    break;
                case 1:
                    DrawTypeOptions();
                    break;
            }

            AssetOrganizerGUI.DrawSeparator();
            DrawCommonOptions();
        }

        private void DrawFolderOptions()
        {
            for (var i = 0; i < _settings.specialFolders.Count; i++)
            {
                var f = _settings.specialFolders[i];
                using (new GUILayout.HorizontalScope("box"))
                {
                    f.active = GUILayout.Toggle(f.active, f.active ? "On" : "Off", GUI.skin.button, GUILayout.Width(50));
                    
                    using (new EditorGUI.DisabledScope(!f.active))
                    {
                        f.name = EditorGUILayout.TextField(f.name);
                        f.action = (ManageAction)EditorGUILayout.EnumPopup(f.action, GUILayout.Width(70));
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            _settings.specialFolders.RemoveAt(i);
                        }
                    }
                }
            }

            if (GUILayout.Button("Add Folder Rule"))
            {
                _settings.specialFolders.Add(new CustomFolder());
            }
        }

        private void DrawTypeOptions()
        {
            using (new GUILayout.HorizontalScope())
            {
                // Left Column
                using (new GUILayout.VerticalScope("box"))
                {
                    for (int i = 0; i < _settings.manageTypes.Length; i += 2)
                        DrawTypeGUI(_settings.manageTypes[i]);
                }
                // Right Column
                using (new GUILayout.VerticalScope("box"))
                {
                    for (int i = 1; i < _settings.manageTypes.Length; i += 2)
                        DrawTypeGUI(_settings.manageTypes[i]);
                }
            }
        }

        private void DrawTypeGUI(ManageType t)
        {
            using (new GUILayout.HorizontalScope())
            {
                var types = t.AssociatedTypes;
                var icon = types.Length > 0 ? new GUIContent(AssetPreview.GetMiniTypeThumbnail(types[0])) : GUIContent.none;

                GUILayout.Label(icon, GUILayout.Height(18), GUILayout.Width(18));
                GUILayout.Label(t.name, GUILayout.ExpandWidth(true));
                _settings.typeActions[t.actionIndex] = (ManageAction)EditorGUILayout.EnumPopup(_settings.typeActions[t.actionIndex], GUILayout.Width(70));
            }
        }

        private void DrawCommonOptions()
        {
            using (new GUILayout.VerticalScope("box"))
            {
                _settings.deleteEmptyFolders = EditorGUILayout.Toggle(new GUIContent("Delete Empty Folders", "After moving assets, delete source folders if they are empty."), _settings.deleteEmptyFolders);
                _settings.sortByOption = (SortOptions)EditorGUILayout.EnumPopup("Sort Search By", _settings.sortByOption);
                
                AssetOrganizerGUI.DrawSeparator();
                
                _settings.copySuffix = EditorGUILayout.TextField(new GUIContent("Copy Suffix", "Suffix to add to copied asset names."), _settings.copySuffix);
                _settings.folderSuffix = EditorGUILayout.TextField(new GUIContent("Folder Suffix", "Suffix to add to the main destination folder."), _settings.folderSuffix);
            }
        }
    }
} 