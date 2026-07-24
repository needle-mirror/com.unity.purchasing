using System.Collections.Generic;
using Unity.Purchasing.Editor.Shared.Assets;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    class CatalogCsvAsset : ScriptableObject, IPath
    {
        internal const string DefaultFileName = "MyCatalog";
        static readonly CatalogCsvParser s_Parser = new();

        public string Path { get; set; }

        internal static string GenerateDefaultContent()
        {
            return s_Parser.Serialize(CatalogItem.CreateDefaultCsvCatalog());
        }

        [MenuItem("Assets/Create/Services/IAP Catalog CSV", false, 82)]
        public static void CreateCatalogCsv()
        {
            var folder = CatalogAssetHelper.GetActiveFolderPath();
            var path = CatalogAssetHelper.GenerateUniquePath(folder, DefaultFileName, Constants.CsvFileExtension);

            var endAction = CreateInstance<CreateCatalogCsvAssetAction>();
#if UNITY_6000_4_OR_NEWER
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(default(UnityEngine.EntityId), endAction, path, null, null);
#else
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, path, null, null);
#endif
        }
    }
}
