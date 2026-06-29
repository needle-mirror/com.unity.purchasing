using System.IO;
using Newtonsoft.Json;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
#if UNITY_6000_4_OR_NEWER
    abstract class CreateCatalogAssetAction : AssetCreationEndAction
    {
        protected abstract string GenerateContent();

        public override void Action(UnityEngine.EntityId instanceId, string pathName, string resourceFile)
        {
            pathName = CatalogAssetHelper.SanitizeAssetPath(pathName);
            File.WriteAllText(pathName, GenerateContent());
            AssetDatabase.ImportAsset(pathName);
        }
    }
#else
    abstract class CreateCatalogAssetAction : EndNameEditAction
    {
        protected abstract string GenerateContent();

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            pathName = CatalogAssetHelper.SanitizeAssetPath(pathName);
            File.WriteAllText(pathName, GenerateContent());
            AssetDatabase.ImportAsset(pathName);
        }
    }
#endif

    class CreateCatalogCsvAssetAction : CreateCatalogAssetAction
    {
        protected override string GenerateContent()
        {
            return CatalogCsvAsset.GenerateDefaultContent();
        }
    }

    class CreateCatalogItemAssetAction : CreateCatalogAssetAction
    {
        protected override string GenerateContent()
        {
            return JsonConvert.SerializeObject(
                CatalogItem.CreateDefaultCatalog(),
                Formatting.Indented,
                EditorCatalogItem.GetSerializationSettings());
        }
    }
}
