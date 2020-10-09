using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Utils
{
    static class SkuDetailsConverter
    {
        internal static void ConvertOnQuerySkuDetailsResponse(List<AndroidJavaObject> skus, Action<List<ProductDescription>> onProductsReceived)
        {
            var products = ConvertSkusDetailsToProducts(skus);
            onProductsReceived(products);
        }

        static List<ProductDescription> ConvertSkusDetailsToProducts(List<AndroidJavaObject> skus)
        {
            List<ProductDescription> products = new List<ProductDescription>();
            foreach (AndroidJavaObject skuDetails in skus)
            {
                products.AddRange(skuDetails.ToListProducts());
            }

            return products;
        }

        static List<ProductDescription> ToListProducts(this AndroidJavaObject skusDetails)
        {
            return new List<ProductDescription>()
            {
                BuildProductDescription(skusDetails)
            };
        }

        /// <summary>
        /// Build a `ProductDescription` from a SkuDetails `AndroidJavaObject`
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/SkuDetails">Learn more about SkuDetails</a>
        /// </summary>
        /// <param name="skuDetails">`AndroidJavaObject` of SkuDetails</param>
        /// <returns>`ProductDescription` representation of a SkuDetails</returns>
        static ProductDescription BuildProductDescription(AndroidJavaObject skuDetails)
        {
            string sku = skuDetails.Call<string>("getSku");
            string price = skuDetails.Call<string>("getPrice");
            string title = skuDetails.Call<string>("getTitle");
            string description = skuDetails.Call<string>("getDescription");
            string priceCurrencyCode = skuDetails.Call<string>("getPriceCurrencyCode");

            ProductDescription product = new ProductDescription(
                sku,
                new ProductMetadata(
                    price,
                    title,
                    description,
                    priceCurrencyCode,
                    0
                ),
                "",
                ""
            );
            return product;
        }
    }
}
