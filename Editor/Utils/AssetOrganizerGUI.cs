using UnityEditor;
using UnityEngine;

namespace com.neuru5278.assetorganizer.Utils
{
    public static class AssetOrganizerGUI
    {
        public static string AssetFolderPath(string variable, string title)
        {
            using (new GUILayout.HorizontalScope())
            {
                Rect dropArea = EditorGUILayout.GetControlRect();
                variable = EditorGUI.TextField(dropArea, title, variable);

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
                        Debug.LogWarning("Selected path must be inside the Assets folder.");
                        return variable;
                    }

                    variable = newPath;
                }
            }

            return variable;
        }

        public static void DrawSeparator(int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(thickness + padding));
            r.height = thickness;
            r.y += padding / 2f;
            r.x -= 2;
            r.width += 6;
            ColorUtility.TryParseHtmlString(EditorGUIUtility.isProSkin ? "#595959" : "#858585", out Color lineColor);
            EditorGUI.DrawRect(r, lineColor);
        }
        
        public class BGColoredScope : GUI.Scope
        {
            private readonly Color _originalColor;

            public BGColoredScope(Color color)
            {
                _originalColor = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            public BGColoredScope(Color active, Color inactive, bool isActive)
            {
                _originalColor = GUI.backgroundColor;
                GUI.backgroundColor = isActive ? active : inactive;
            }

            protected override void CloseScope()
            {
                GUI.backgroundColor = _originalColor;
            }
        }
    }
} 