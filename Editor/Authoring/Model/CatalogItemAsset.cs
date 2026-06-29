using System.IO;
using Newtonsoft.Json;
using Unity.Purchasing.Editor.Shared.Assets;
using UnityEditor.Purchasing.Authoring;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;
using Constants = UnityEditor.Purchasing.Editor.Authoring.Core.Model.Constants;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.services.PACKAGENAME@x.y/manual/Authoring/FIX_ME.html")]
    class CatalogItemAsset : ScriptableObject, IPath, ISerializationCallbackReceiver
    {
        const string k_DefaultFileName = "MyCatalogItem";

        string m_Path;

        public string Name { get; set; }
        public string Path { get => m_Path; set => SetPath(value); }
        public CatalogEntryDeploymentItem CatalogEntryDeploymentItem { get; set; }
        void ISerializationCallbackReceiver.OnBeforeSerialize() { /* Not needed */ }
        void ISerializationCallbackReceiver.OnAfterDeserialize() { Name = System.IO.Path.GetFileName(Path); }

        void SetPath(string path)
        {
            var oldFileNameWithoutExtension = string.IsNullOrEmpty(m_Path) ? null
                    : System.IO.Path.GetFileNameWithoutExtension(m_Path);
            var newFileNameWithoutExtension = string.IsNullOrEmpty(path) ? null
                    : System.IO.Path.GetFileNameWithoutExtension(path);

            CatalogEntryDeploymentItem ??= new CatalogEntryDeploymentItem(path);
            
            m_Path = path;
            
            if (string.IsNullOrEmpty(Name) || Name == oldFileNameWithoutExtension)
            {
                Name = newFileNameWithoutExtension;
            }

            var di = CatalogEntryDeploymentItem;
            di.Path = path;
            di.Name = System.IO.Path.GetFileName(path);
            di.CatalogItem ??= new EditorCatalogItem();

            di.CatalogItem.CatalogListingId = string.IsNullOrEmpty(newFileNameWithoutExtension)
                ? null
                : CatalogItem.CatalogListingIdPrefix + newFileNameWithoutExtension;

            if (string.IsNullOrEmpty(di.CatalogItem.uSku) || di.CatalogItem.uSku == oldFileNameWithoutExtension)
            {
                di.CatalogItem.uSku = newFileNameWithoutExtension;
            }

            if (di.CatalogItem.ProductDetails != null || di.CatalogItem.PricingDetails != null)
            {
                di.Validate(null);
            }
        }

        [MenuItem("Assets/Create/Services/IAP Catalog Item", false, 81)]
        public static void CreateConfig()
        {
            var folder = CatalogAssetHelper.GetActiveFolderPath();
            var path = CatalogAssetHelper.GenerateUniquePath(folder, k_DefaultFileName, Constants.FileExtension);

            var endAction = CreateInstance<CreateCatalogItemAssetAction>();
#if UNITY_6000_4_OR_NEWER
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(default(UnityEngine.EntityId), endAction, path, null, null);
#else
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, path, null, null);
#endif
        }

        public void CopyFrom(CatalogItemInspectorConfig config)
        {
            if (CatalogEntryDeploymentItem.CatalogItem is EditorCatalogItem editorCatalog)
            {
                editorCatalog.CopyFrom(config);
            }
        }

        public void SaveToDisk()
        {
            var catalogItem = new CatalogItem(CatalogEntryDeploymentItem.CatalogItem);
            if (catalogItem.uSku == System.IO.Path.GetFileNameWithoutExtension(Path))
            {
                catalogItem.uSku = null;
            }
            var serializedContent = JsonConvert.SerializeObject(catalogItem, EditorCatalogItem.GetSerializationSettings());
            File.WriteAllText(CatalogEntryDeploymentItem.Path, serializedContent);
        }
    }
}
