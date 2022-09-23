using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Utils
{
    class SkuDetailsConverter : ISkuDetailsConverter
    {
        public List<ProductDescription> ConvertOnQuerySkuDetailsResponse(IEnumerable<AndroidJavaObject> skus)
        {
            return skus.Select(ToProductDescription).ToList();
        }

        static ProductDescription ToProductDescription(AndroidJavaObject skusDetails)
        {
            return BuildProductDescription(skusDetails.Wrap());
        }

        /// <summary>
        /// Build a `ProductDescription` from a SkuDetails `AndroidJavaObject`
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/SkuDetails">Learn more about SkuDetails</a>
        /// </summary>
        /// <param name="skuDetails">`AndroidJavaObject` of SkuDetails</param>
        /// <returns>`ProductDescription` representation of a SkuDetails</returns>
        internal static ProductDescription BuildProductDescription(IAndroidJavaObjectWrapper skuDetails)
        {
            var sku = skuDetails.Call<string>("getSku");
            var price = skuDetails.Call<string>("getPrice");
            var priceAmount = Convert.ToDecimal(skuDetails.Call<long>("getPriceAmountMicros") > 0 ? skuDetails.Call<long>("getPriceAmountMicros") / 1000000.0 : 0);
            var title = skuDetails.Call<string>("getTitle");
            var description = skuDetails.Call<string>("getDescription");
            var priceCurrencyCode = skuDetails.Call<string>("getPriceCurrencyCode");
            var originalJson = skuDetails.Call<string>("getOriginalJson");
            var subscriptionPeriod = skuDetails.Call<string>("getSubscriptionPeriod");
            var freeTrialPeriod = skuDetails.Call<string>("getFreeTrialPeriod");
            var introductoryPrice = skuDetails.Call<string>("getIntroductoryPrice");
            var introductoryPricePeriod = skuDetails.Call<string>("getIntroductoryPricePeriod");
            var introductoryPriceCycles = skuDetails.Call<int>("getIntroductoryPriceCycles");

            var productMetadata = new GoogleProductMetadata(
                price,
                title,
                description,
                priceCurrencyCode,
                priceAmount)
            {
                originalJson = originalJson,
                introductoryPrice = introductoryPrice,
                subscriptionPeriod = subscriptionPeriod,
                freeTrialPeriod = freeTrialPeriod,
                introductoryPricePeriod = introductoryPricePeriod,
                introductoryPriceCycles = introductoryPriceCycles
            };

            var product = new ProductDescription(
                sku,
                productMetadata,
                "",
                ""
            );
            return product;
        }
    }
}
