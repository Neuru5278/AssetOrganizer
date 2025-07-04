using System;
using System.Linq;

namespace com.neuru5278.assetorganizer.Data
{
    [Serializable]
    public struct ManageType
    {
        public int actionIndex;
        public string name;
        public string[] typeNames; // Use string names for robust serialization
        public string[] extensions;

        [NonSerialized] private Type[] _associatedTypes;
        
        public Type[] AssociatedTypes
        {
            get
            {
                if (_associatedTypes == null)
                {
                    _associatedTypes = typeNames
                        .Select(Type.GetType)
                        .Where(t => t != null)
                        .ToArray();
                }
                return _associatedTypes;
            }
        }

        public bool IsAppliedTo(DependencyAsset asset)
        {
            if (asset?.type == null) return false;

            bool typeMatch = AssociatedTypes.Any(t => asset.type == t || asset.type.IsSubclassOf(t));
            bool extensionMatch = extensions.Any(ext => !string.IsNullOrWhiteSpace(ext) && asset.path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            
            return typeMatch || extensionMatch;
        }
    }
} 