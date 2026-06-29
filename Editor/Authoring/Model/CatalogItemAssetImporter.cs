using System;
using UnityEditor.AssetImporters;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.services.PACKAGENAME@x.y/manual/Authoring/FIX_ME.html")]
    [ScriptedImporter(1, Constants.FileExtension)]
    class CatalogItemAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = PurchasingAuthoringServices.Instance.GetService<ObservableCatalogItemAssets>()
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
