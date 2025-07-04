using System.Collections.Generic;
using System.IO;
using System.Linq;
using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Settings;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.neuru5278.assetorganizer.Services
{
    public class DependencyAnalyzer
    {
        private readonly AssetOrganizerSettings _settings;

        public DependencyAnalyzer(AssetOrganizerSettings settings)
        {
            _settings = settings;
        }

        public List<DependencyAsset> GetDependencies(Object mainAsset, out string initialPath)
        {
            initialPath = AssetDatabase.GetAssetPath(mainAsset);
            bool isFolder = AssetDatabase.IsValidFolder(initialPath);
            
            string[] assetPaths = isFolder 
                ? GetAssetPathsInFolder(initialPath).ToArray() 
                : AssetDatabase.GetDependencies(initialPath);
            
            var dependencyAssets = assetPaths.Select(p => new DependencyAsset(p)).ToList();

            if (!isFolder)
            {
                initialPath = Path.GetDirectoryName(initialPath)?.Replace('\\', '/');
            }

            ApplyActionsAndSort(dependencyAssets);
            
            return dependencyAssets;
        }

        private void ApplyActionsAndSort(List<DependencyAsset> assets)
        {
            foreach (var asset in assets)
            {
                // 1. Apply type-based action first as a default.
                if (!TrySetActionFromType(asset))
                {
                    // If no type matches, assign the 'Other' type.
                    // Assuming 'Other' is the last one in the list.
                    var otherType = _settings.manageTypes.LastOrDefault();
                    if (otherType.name == "Other")
                    {
                        asset.associatedType = otherType;
                        asset.action = _settings.typeActions[otherType.actionIndex];
                    }
                }
                
                // 2. Override with folder-based action if a rule matches (higher priority).
                string[] subFolders = asset.path.Split('/');
                foreach (var folderRule in _settings.specialFolders)
                {
                    if (!folderRule.active) continue;
                    if (subFolders.Contains(folderRule.name))
                    {
                        asset.action = folderRule.action;
                        break; 
                    }
                }
            }
            
            // 3. Sort the final list.
            switch (_settings.sortByOption)
            {
                case SortOptions.AlphabeticalPath:
                    assets.Sort((a, b) => string.Compare(a.path, b.path, System.StringComparison.Ordinal));
                    break;
                case SortOptions.AlphabeticalAsset:
                    assets.Sort((a, b) => string.Compare(a.asset.name, b.asset.name, System.StringComparison.Ordinal));
                    break;
                case SortOptions.AssetType:
                    assets.Sort((a, b) => string.Compare(a.type.Name, b.type.Name, System.StringComparison.Ordinal));
                    break;
            }
        }

        private bool TrySetActionFromType(DependencyAsset asset)
        {
            foreach (var manageType in _settings.manageTypes)
            {
                if (manageType.IsAppliedTo(asset))
                {
                    asset.action = _settings.typeActions[manageType.actionIndex];
                    asset.associatedType = manageType;
                    return true;
                }
            }
            return false;
        }

        private static List<string> GetAssetPathsInFolder(string path, bool deep = true)
        {
            string[] fileEntries = Directory.GetFiles(path, "*", deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            return fileEntries
                .Where(fileName => !fileName.EndsWith(".meta") && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(fileName.Replace('\\', '/'))))
                .Select(fileName => fileName.Replace('\\', '/'))
                .ToList();
        }
    }
} 