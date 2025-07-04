using System.Collections.Generic;
using com.neuru5278.assetorganizer.Data;
using com.neuru5278.assetorganizer.Settings;
using UnityEngine;

namespace com.neuru5278.assetorganizer.Services
{
    public enum ProcessMode
    {
        ByType,
        ByStructure
    }

    public class AssetOrganizationService
    {
        private readonly DependencyAnalyzer _analyzer;
        private readonly AssetProcessor _processor;
        private readonly AssetOrganizerSettings _settings;

        public AssetOrganizationService(AssetOrganizerSettings settings)
        {
            _settings = settings;
            _analyzer = new DependencyAnalyzer(settings);
            _processor = new AssetProcessor(settings);
        }

        public List<DependencyAsset> RunAnalysis(Object mainAsset, out string initialPath)
        {
            if (mainAsset == null)
            {
                initialPath = "Assets";
                return new List<DependencyAsset>();
            }
            return _analyzer.GetDependencies(mainAsset, out initialPath);
        }

        public void RunProcessing(List<DependencyAsset> assets, string mainAssetPath, string destinationPath, ProcessMode mode)
        {
            if (assets == null || assets.Count == 0)
            {
                Debug.LogWarning("Asset Organizer: No assets to process.");
                return;
            }

            if (string.IsNullOrEmpty(destinationPath))
            {
                Debug.LogError("Asset Organizer: Destination path cannot be empty.");
                return;
            }
            
            if (mode == ProcessMode.ByType)
            {
                _processor.ProcessByType(assets, destinationPath);
            }
            else
            {
                _processor.ProcessByStructure(assets, mainAssetPath, destinationPath);
            }
        }
    }
} 