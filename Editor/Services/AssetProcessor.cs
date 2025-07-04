using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly Dictionary<string, string> _guidMap = new Dictionary<string, string>();

        public AssetProcessor(AssetOrganizerSettings settings)
        {
            _settings = settings;
        }

        public void ProcessByType(List<DependencyAsset> assets, string destinationRoot)
        {
            Process(assets, (asset, root) => GetNewPathByType(asset, root), destinationRoot);
        }

        public void ProcessByStructure(List<DependencyAsset> assets, string sourceRoot, string destinationRoot)
        {
            Process(assets, (asset, root) => GetNewPathByStructure(asset, sourceRoot, root), destinationRoot);
        }

        private void Process(List<DependencyAsset> assets, System.Func<DependencyAsset, string, string> getNewPathFunc, string destinationRoot)
        {
            _guidMap.Clear();
            var copiedAssets = new List<string>();
            var affectedFolders = new HashSet<string>();

            try
            {
                AssetDatabase.StartAssetEditing();
                
                var createdFolders = CreateRequiredFolders(assets, destinationRoot, getNewPathFunc);
                affectedFolders.UnionWith(createdFolders);

                foreach (var asset in assets)
                {
                    if (asset.action == ManageAction.Skip) continue;

                    string newPath = getNewPathFunc(asset, destinationRoot);

                    if (string.IsNullOrEmpty(newPath))
                    {
                        Debug.LogWarning($"Could not determine new path for asset: {asset.path}. Skipping.");
                        continue;
                    }
                    
                    string oldPath = asset.path;
                    if (asset.action == ManageAction.Move)
                    {
                        string dir = Path.GetDirectoryName(oldPath);
                        if(dir != null) affectedFolders.Add(dir);
                        AssetDatabase.MoveAsset(oldPath, newPath);
                        _guidMap[asset.guid] = asset.guid; // GUID doesn't change on move
                        copiedAssets.Add(newPath);
                    }
                    else if (asset.action == ManageAction.Copy)
                    {
                        AssetDatabase.CopyAsset(oldPath, newPath);
                        string newGuid = AssetDatabase.AssetPathToGUID(newPath);
                        if (!string.IsNullOrEmpty(newGuid))
                        {
                            _guidMap[asset.guid] = newGuid;
                        }
                        copiedAssets.Add(newPath);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            RemapGuids(copiedAssets);
            
            if (_settings.deleteEmptyFolders)
            {
                CleanupEmptyFolders(affectedFolders);
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(destinationRoot));
        }

        private void RemapGuids(List<string> assetPaths)
        {
            if (assetPaths.Count == 0) return;
            
            try
            {
                AssetDatabase.StartAssetEditing();
                string[] allGuids = _guidMap.Keys.ToArray();
                string[] allAssetPaths = assetPaths.ToArray();

                for (int i = 0; i < allAssetPaths.Length; i++)
                {
                    string assetPath = allAssetPaths[i];
                    
                    EditorUtility.DisplayProgressBar("Remapping GUIDs", $"Processing {Path.GetFileName(assetPath)}", (float)i / allAssetPaths.Length);

                    if (!assetPath.EndsWith(".asset") && !assetPath.EndsWith(".prefab") && !assetPath.EndsWith(".unity") && !assetPath.EndsWith(".mat"))
                        continue;
                    
                    string content = File.ReadAllText(assetPath);
                    
                    foreach (string oldGuid in allGuids)
                    {
                        if (content.Contains(oldGuid))
                        {
                            content = content.Replace(oldGuid, _guidMap[oldGuid]);
                        }
                    }
                    File.WriteAllText(assetPath, content);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }
        
        private HashSet<string> CreateRequiredFolders(List<DependencyAsset> assets, string destinationRoot, System.Func<DependencyAsset, string, string> getNewPathFunc)
        {
            var createdFolders = new HashSet<string>();
            foreach (var asset in assets)
            {
                if(asset.action == ManageAction.Skip) continue;
                
                string newPath = getNewPathFunc(asset, destinationRoot);
                string newDir = Path.GetDirectoryName(newPath);

                if (!string.IsNullOrEmpty(newDir) && !Directory.Exists(newDir))
                {
                    Directory.CreateDirectory(newDir);
                    createdFolders.Add(newDir);
                }
            }
            return createdFolders;
        }

        private void CleanupEmptyFolders(HashSet<string> folders)
        {
            foreach (string folder in folders.OrderByDescending(f => f.Length))
            {
                if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                {
                    Directory.Delete(folder, false);
                    File.Delete(folder + ".meta");
                }
            }
            AssetDatabase.Refresh();
        }

        private string GetNewPathByType(DependencyAsset asset, string destinationRoot)
        {
            var rule = _settings.FindRuleFor(asset.type) ?? _settings.defaultRule;
            string newDir = Path.Combine(destinationRoot, rule.folder).Replace('\\', '/');
            return Path.Combine(newDir, asset.fileName + _settings.copySuffix + asset.extension).Replace('\\', '/');
        }

        private string GetNewPathByStructure(DependencyAsset asset, string sourceRoot, string destinationRoot)
        {
            string relativePath = asset.path.Replace(sourceRoot, "").TrimStart('/');
            string newPath = Path.Combine(destinationRoot + _settings.folderSuffix, relativePath).Replace('\\', '/');
            string newFileName = Path.GetFileNameWithoutExtension(newPath) + _settings.copySuffix + Path.GetExtension(newPath);
            return Path.Combine(Path.GetDirectoryName(newPath) ?? "", newFileName).Replace('\\', '/');
        }
    }
} 