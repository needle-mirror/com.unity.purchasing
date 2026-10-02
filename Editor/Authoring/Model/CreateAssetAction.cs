using System.IO;
using Newtonsoft.Json;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
#if UNITY_6000_4_OR_NEWER
    abstract class CreateAssetAction : AssetCreationEndAction
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
    abstract class CreateAssetAction : EndNameEditAction
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

    class CreateCatalogCsvAssetAction : CreateAssetAction
    {
        protected override string GenerateContent()
        {
            return CatalogCsvAsset.GenerateDefaultContent();
        }
    }

    class CreateCatalogItemAssetAction : CreateAssetAction
    {
        protected override string GenerateContent()
        {
            return JsonConvert.SerializeObject(
                CatalogItem.CreateDefaultCatalog(),
                Formatting.Indented,
                EditorCatalogItem.GetSerializationSettings());
        }
    }

    class CreateRoutingAssetAction : CreateAssetAction
    {
        protected override string GenerateContent()
        {
            return JsonConvert.SerializeObject(
                ProviderRoutingConfig.CreateDefault(),
                Formatting.Indented);
        }
    }
}
