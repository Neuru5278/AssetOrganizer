using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.neuru5278.assetorganizer.Data
{
    public class DependencyAsset
    {
        public readonly string guid;
        public readonly string path;
        public readonly Object asset;
        public readonly Texture icon;
        public readonly System.Type type;
        public ManageAction action;
        public ManageType associatedType;

        public string fileName => Path.GetFileNameWithoutExtension(path);
        public string extension => Path.GetExtension(path);

        public DependencyAsset(string path)
        {
            this.path = path;
            guid = AssetDatabase.AssetPathToGUID(path);
            asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            icon = AssetDatabase.GetCachedIcon(path);
            type = asset ? asset.GetType() : typeof(Object);
            action = ManageAction.Skip;
        }
    }
} 