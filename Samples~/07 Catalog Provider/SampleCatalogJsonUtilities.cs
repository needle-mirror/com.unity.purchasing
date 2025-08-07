using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Samples.Purchasing.Core.CatalogProvider
{
    static class SampleCatalogJsonUtilities
    {
        /// <summary>
        /// This method is a simple way to generate a Json catalog.
        /// It can be a good way to provide an updated json file to update the catalog remotely without updating the whole project.
        /// The declaration is very similar to the hard coded method seen above, but instead of saving it directly in a catalog, we instead serialize it as a json file.
        /// </summary>
        public static void GenerateJson(string filePath)
        {
            var jsonCatalog = new List<SampleCatalogProduct>
            {
                new SampleCatalogProduct("com.example.consumable.skuid", ProductType.Consumable),
                new SampleCatalogProduct("com.example.consumable.generic.skuid", ProductType.Consumable, new StoreSpecificIds {{"com.example.consumable.google.skuid", UnityEngine.Purchasing.GooglePlay.Name}, {"com.example.consumable.ios.skuid", UnityEngine.Purchasing.AppleAppStore.Name}}),
                new SampleCatalogProduct("com.example.non-consumable.skuid", ProductType.NonConsumable),
                new SampleCatalogProduct("com.example.subscription.skuid", ProductType.Subscription)
            };
            SampleCatalog catalog = new SampleCatalog(jsonCatalog);

            SaveCatalogToJson(catalog, filePath);
        }

        public static void SaveCatalogToJson(SampleCatalog catalog, string filePath)
        {
            try
            {
                var jsonContent = JsonUtility.ToJson(catalog);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception error)
            {
                Debug.Log(error);
            }
        }

        public static SampleCatalog LoadCatalogFromJson(string filePath)
        {
            return JsonUtility.FromJson<SampleCatalog>(File.ReadAllText(filePath));
        }
    }
}
