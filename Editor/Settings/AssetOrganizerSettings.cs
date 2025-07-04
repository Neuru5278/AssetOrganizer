using System.Collections.Generic;
using com.neuru5278.assetorganizer.Data;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDK3.Avatars.ScriptableObjects;

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

            manageTypes = new ManageType[]
            {
                new ManageType { actionIndex = 0, name = "Animations", typeNames = new[] { typeof(AnimationClip).AssemblyQualifiedName, typeof(BlendTree).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 1, name = "Controllers", typeNames = new[] { typeof(AnimatorController).AssemblyQualifiedName, typeof(AnimatorOverrideController).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 2, name = "Textures", typeNames = new[] { typeof(Texture).AssemblyQualifiedName, typeof(Texture2D).AssemblyQualifiedName, typeof(RenderTexture).AssemblyQualifiedName, typeof(Cubemap).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 3, name = "Materials", typeNames = new[] { typeof(Material).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 4, name = "Models", typeNames = new[] { typeof(Mesh).AssemblyQualifiedName }, extensions = new[] { ".fbx", ".obj", ".dae", ".3ds", ".dxf", ".blend" } },
                new ManageType { actionIndex = 5, name = "Prefabs", typeNames = new[] { typeof(GameObject).AssemblyQualifiedName }, extensions = new[] { ".prefab" } },
                new ManageType { actionIndex = 6, name = "Audio", typeNames = new[] { typeof(AudioClip).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 7, name = "Masks", typeNames = new[] { typeof(AvatarMask).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 8, name = "Scenes", typeNames = new[] { typeof(UnityEditor.SceneAsset).AssemblyQualifiedName }, extensions = new[] { ".unity" } },
                new ManageType { actionIndex = 9, name = "Presets", typeNames = new[] { typeof(UnityEditor.Presets.Preset).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 10, name = "VRC", typeNames = new[] { typeof(VRCExpressionParameters).AssemblyQualifiedName, typeof(VRCExpressionsMenu).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 11, name = "Shaders", typeNames = new[] { typeof(Shader).AssemblyQualifiedName, typeof(ComputeShader).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 12, name = "Scripts", typeNames = new[] { typeof(UnityEditor.MonoScript).AssemblyQualifiedName }, extensions = new[] { ".dll", ".cs" } },
                new ManageType { actionIndex = 13, name = "Fonts", typeNames = new[] { typeof(Font).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 14, name = "Physics", typeNames = new[] { typeof(PhysicMaterial).AssemblyQualifiedName, typeof(PhysicsMaterial2D).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 15, name = "Lighting", typeNames = new[] { typeof(LightingDataAsset).AssemblyQualifiedName }, extensions = new string[0] },
                new ManageType { actionIndex = 16, name = "Other", typeNames = new[] { typeof(ScriptableObject).AssemblyQualifiedName }, extensions = new string[0] }
            };

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
    }
} 