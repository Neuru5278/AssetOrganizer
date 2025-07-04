using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.neuru5278.assetorganizer.Data
{
    public class DependencyAsset
    {
        public readonly Object asset;
        public readonly string path;
        public readonly string guid;
        public readonly Type type;
        public readonly GUIContent icon;
        public ManageAction action;
        public ManageType associatedType;

        public string fileName => Path.GetFileNameWithoutExtension(path);
        public string extension => Path.GetExtension(path);

        public DependencyAsset(string path)
        {
            this.path = path;
            asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            guid = AssetDatabase.AssetPathToGUID(path);
            action = ManageAction.Skip;
            type = asset.GetType();
            icon = new GUIContent(AssetPreview.GetMiniTypeThumbnail(type), type.Name);
        }
    }
} 