using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    [ScriptedImporter(1, Constants.CsvFileExtension)]
    class CatalogCsvAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<CatalogCsvAsset>();
            asset.name = StripCompoundExtension(ctx.assetPath);

            ctx.AddObjectToAsset("MainAsset", asset);
            ctx.SetMainObject(asset);
        }

        static string StripCompoundExtension(string path)
        {
            var fileName = Path.GetFileName(path);
            if (fileName.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return fileName.Substring(0, fileName.Length - Constants.CsvFileExtension.Length);
            }

            return Path.GetFileNameWithoutExtension(path);
        }

        void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;
        }
    }
}
