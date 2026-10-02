using UnityEditor.AssetImporters;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    [ScriptedImporter(1, Constants.RoutingFileExtension)]
    class RoutingAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = PurchasingAuthoringServices.Instance
                .GetService<ObservableRoutingAssets>()
                .GetOrCreateInstance(ctx.assetPath);

            ctx.AddObjectToAsset("MainAsset", asset);
            ctx.SetMainObject(asset);
        }

        void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;
        }
    }
}
