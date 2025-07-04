using System.Collections.Generic;
using com.neuru5278.assetorganizer.Data;
using UnityEditor.Animations;
using UnityEngine;

namespace com.neuru5278.assetorganizer.Settings
{
    [CreateAssetMenu(fileName = "AssetOrganizerSettings", menuName = "Asset Organizer/Settings")]
    public class AssetOrganizerSettings : ScriptableObject
    {
        [Header("Folder-based Rules")]
        public List<CustomFolder> specialFolders;

        [Header("Type-based Rules")]
        public ManageType[] manageTypes;
        public ManageAction[] typeActions;

        [Header("General Options")]
        public SortOptions sortByOption = SortOptions.AlphabeticalPath;
        public bool deleteEmptyFolders = true;
        
        [Header("Suffixes")]
        public string copySuffix = "";
        public string folderSuffix = "_copy";
        
        public void ResetToDefaults()
        {
            copySuffix = "";
            folderSuffix = "_copy";

            specialFolders = new List<CustomFolder>
            {
                new CustomFolder("__Generated", ManageAction.Copy),
                new CustomFolder("VRCSDK"),
                new CustomFolder("Packages"),
                new CustomFolder("Plugins"),
                new CustomFolder("Editor")
            };

            var types = new List<ManageType>();
            var actions = new List<ManageAction>();

            void AddRule(string name, ManageAction action, string[] typeNames, string[] extensions = null)
            {
                types.Add(new ManageType {
                    actionIndex = actions.Count,
                    name = name,
                    typeNames = typeNames,
                    extensions = extensions ?? new string[0]
                });
                actions.Add(action);
            }

            // Define all rules dynamically
            AddRule("Animations", ManageAction.Copy, new[] { typeof(AnimationClip).AssemblyQualifiedName, typeof(BlendTree).AssemblyQualifiedName });
            AddRule("Controllers", ManageAction.Copy, new[] { typeof(AnimatorController).AssemblyQualifiedName, typeof(AnimatorOverrideController).AssemblyQualifiedName });
            AddRule("Textures", ManageAction.Copy, new[] { typeof(Texture).AssemblyQualifiedName, typeof(Texture2D).AssemblyQualifiedName, typeof(RenderTexture).AssemblyQualifiedName, typeof(Cubemap).AssemblyQualifiedName });
            AddRule("Materials", ManageAction.Copy, new[] { typeof(Material).AssemblyQualifiedName });
            AddRule("Models", ManageAction.Copy, new[] { typeof(Mesh).AssemblyQualifiedName }, new[] { ".fbx", ".obj", ".dae", ".3ds", ".dxf", ".blend" });
            AddRule("Prefabs", ManageAction.Copy, new[] { typeof(GameObject).AssemblyQualifiedName }, new[] { ".prefab" });
            AddRule("Audio", ManageAction.Copy, new[] { typeof(AudioClip).AssemblyQualifiedName });
            AddRule("Masks", ManageAction.Copy, new[] { typeof(AvatarMask).AssemblyQualifiedName });
            AddRule("Scenes", ManageAction.Copy, new[] { typeof(UnityEditor.SceneAsset).AssemblyQualifiedName }, new[] { ".unity" });
            AddRule("Presets", ManageAction.Skip, new[] { typeof(UnityEditor.Presets.Preset).AssemblyQualifiedName });
            AddRule("VRC", ManageAction.Copy, new[] { 
                "VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters, VRCSDK3A", 
                "VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu, VRCSDK3A" 
            });
            AddRule("Shaders", ManageAction.Skip, new[] { typeof(Shader).AssemblyQualifiedName, typeof(ComputeShader).AssemblyQualifiedName });
            AddRule("Scripts", ManageAction.Skip, new[] { typeof(UnityEditor.MonoScript).AssemblyQualifiedName }, new[] { ".dll", ".cs" });
            AddRule("Fonts", ManageAction.Copy, new[] { typeof(Font).AssemblyQualifiedName });
            AddRule("Physics", ManageAction.Copy, new[] { typeof(PhysicMaterial).AssemblyQualifiedName, typeof(PhysicsMaterial2D).AssemblyQualifiedName });
            AddRule("Other", ManageAction.Copy, new[] { typeof(ScriptableObject).AssemblyQualifiedName });

            // Assign the generated lists back to the arrays
            manageTypes = types.ToArray();
            typeActions = actions.ToArray();
        }
    }
} 