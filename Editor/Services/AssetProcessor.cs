using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Settings;
using UnityEditor;
using UnityEngine;

namespace com.neuru5278.assetorganizer.Services
{
    public class AssetProcessor
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        private readonly AssetOrganizerSettings _settings;

        public AssetProcessor(AssetOrganizerSettings settings)
        {
            _settings = settings;
        }

        public void ProcessByType(List<DependencyAsset> assets, string destinationPath)
        {
            string finalDestPath = PrepareDestinationPath(destinationPath);
            var createdFolders = CreateTargetFoldersByType(finalDestPath, assets);
            ProcessAssets(assets, finalDestPath, createdFolders, (a, p) => GetNewPathByType(a, p));
        }

        public void ProcessByStructure(List<DependencyAsset> assets, string mainAssetPath, string destinationPath)
        {
            string finalDestPath = PrepareDestinationPath(destinationPath);
            var createdFolders = CreateTargetFoldersByStructure(finalDestPath, assets, mainAssetPath);
            ProcessAssets(assets, finalDestPath, createdFolders, (a, p) => GetNewPathByStructure(a, p, mainAssetPath));
        }

        private void ProcessAssets(List<DependencyAsset> assets, string finalDestPath, List<string> createdFolders, System.Func<DependencyAsset, string, string> getNewPathFunc)
        {
            var affectedFolders = new HashSet<string>();
            var assetPathMap = new Dictionary<string, string>();

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < assets.Count; i++)
                {
                    var asset = assets[i];
                    EditorUtility.DisplayProgressBar("Organizing Assets", $"Processing {asset.asset.name}...", (float)i / assets.Count);

                    if (asset.action == ManageAction.Skip) continue;

                    string newPath = getNewPathFunc(asset, finalDestPath);

                    if (asset.action == ManageAction.Move)
                    {
                        affectedFolders.Add(Path.GetDirectoryName(asset.path));
                        AssetDatabase.MoveAsset(asset.path, newPath);
                    }
                    else if (asset.action == ManageAction.Copy)
                    {
                        AssetDatabase.CopyAsset(asset.path, newPath);
                        assetPathMap[asset.guid] = AssetDatabase.AssetPathToGUID(newPath);
                    }
                }
                RemapGuids(assetPathMap);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            if (_settings.deleteEmptyFolders)
            {
                CleanupEmptyFolders(createdFolders.Concat(affectedFolders));
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(finalDestPath));
        }

        private string PrepareDestinationPath(string destinationPath)
        {
            if (!destinationPath.StartsWith("Assets/"))
            {
                destinationPath = "Assets/" + destinationPath;
            }

            string finalPath = destinationPath;
            if (!string.IsNullOrEmpty(_settings.folderSuffix))
            {
                finalPath = $"{destinationPath}{_settings.folderSuffix}";
            }

            ReadyPath(finalPath);
            return finalPath;
        }

        private List<string> CreateTargetFoldersByType(string destPath, List<DependencyAsset> assets)
        {
            var createdFolders = new List<string>();
            var requiredFolders = assets.Select(a => a.associatedType.name).Distinct();
            foreach (var folderName in requiredFolders)
            {
                string path = Path.Combine(destPath, folderName);
                if (ReadyPath(path)) createdFolders.Add(path);
            }
            return createdFolders;
        }

        private List<string> CreateTargetFoldersByStructure(string destPath, List<DependencyAsset> assets, string mainAssetPath)
        {
            var createdFolders = new List<string>();
            foreach (var asset in assets)
            {
                string dirA = Path.GetDirectoryName(asset.path).Replace("\\", "/");
                string dirMiddle = dirA.Replace(mainAssetPath, "").TrimStart('/');
                string newDirPath = Path.Combine(destPath, dirMiddle);
                if (ReadyPath(newDirPath)) createdFolders.Add(newDirPath);
            }
            return createdFolders;
        }
        
        private string GetNewPathByType(DependencyAsset asset, string destPath)
        {
            string folder = Path.Combine(destPath, asset.associatedType.name);
            string filename = GetFinalFileName(asset);
            return AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, filename));
        }

        private string GetNewPathByStructure(DependencyAsset asset, string destPath, string mainAssetPath)
        {
            string dirA = Path.GetDirectoryName(asset.path).Replace("\\", "/");
            string dirMiddle = dirA.Replace(mainAssetPath, "").TrimStart('/');
            string filename = GetFinalFileName(asset);
            return AssetDatabase.GenerateUniqueAssetPath(Path.Combine(destPath, dirMiddle, filename));
        }
        
        private string GetFinalFileName(DependencyAsset asset)
        {
            if (asset.action == ManageAction.Copy && !string.IsNullOrEmpty(_settings.copySuffix))
            {
                return $"{Path.GetFileNameWithoutExtension(asset.path)}{_settings.copySuffix}{Path.GetExtension(asset.path)}";
            }
            return Path.GetFileName(asset.path);
        }

        private void RemapGuids(Dictionary<string, string> guidMap)
        {
            if (guidMap.Count == 0) return;

            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var assetsToRema = allAssetPaths.Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".mat")).ToArray();

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < assetsToRema.Length; i++)
                {
                    var assetPath = assetsToRema[i];
                    EditorUtility.DisplayProgressBar("Remapping GUIDs", $"Processing {Path.GetFileName(assetPath)}...", (float)i / assetsToRema.Length);
                    
                    string content = File.ReadAllText(assetPath, Encoding);
                    bool changed = false;
                    foreach (var kvp in guidMap)
                    {
                        if (content.Contains(kvp.Key))
                        {
                            content = content.Replace(kvp.Key, kvp.Value);
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        File.WriteAllText(assetPath, content, Encoding);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private void CleanupEmptyFolders(IEnumerable<string> folders)
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var folderPath in folders.Distinct().Where(DirectoryIsEmpty))
                {
                    AssetDatabase.DeleteAsset(folderPath);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static bool DirectoryIsEmpty(string path) => AssetDatabase.IsValidFolder(path) && !Directory.EnumerateFileSystemEntries(path).Any();
        
        private static bool ReadyPath(string folderPath)
        {
            if (Directory.Exists(folderPath)) return false;
            Directory.CreateDirectory(folderPath);
            return true;
        }
    }
} 