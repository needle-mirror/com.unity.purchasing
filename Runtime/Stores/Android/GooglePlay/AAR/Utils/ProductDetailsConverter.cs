#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.MiniJSON;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Utils
{
    class ProductDetailsConverter : IProductDetailsConverter
    {
        public List<ProductDescription> ConvertOnQueryProductDetailsResponse(IEnumerable<AndroidJavaObject> productDetails)
        {
            return productDetails.Select(ToProductDescription).ToList();
        }

        static ProductDescription ToProductDescription(AndroidJavaObject productDetails)
        {
            try
            {
                return BuildProductDescription(productDetails);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Build a `ProductDescription` from a ProductDetails `AndroidJavaObject`
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/ProductDetails">Learn more about ProductDetails</a>
        /// </summary>
        /// <param name="productDetails">`AndroidJavaObject` of ProductDetails</param>
        /// <returns>`ProductDescription` representation of a ProductDetails</returns>
        internal static ProductDescription BuildProductDescription(AndroidJavaObject productDetails)
        {
            // TODO: IAP-2833 - Clean for one time vs subscription
            var productId = productDetails.Call<string>("getProductId");
            using var oneTimePurchaseOffer = productDetails.Call<AndroidJavaObject>("getOneTimePurchaseOfferDetails");
            using var subscriptionOffer = productDetails.Call<AndroidJavaObject>("getSubscriptionOfferDetails").Enumerate().FirstOrDefault();
            using var subscriptionPricingPhases = subscriptionOffer?.Call<AndroidJavaObject>("getPricingPhases");
            using var subscriptionPricingPhasesList = subscriptionPricingPhases?.Call<AndroidJavaObject>("getPricingPhaseList");
            var subscriptionPricingPhasesListEnum = subscriptionPricingPhasesList?.Enumerate().ToList();

            var subscriptionBasePricingPhase = subscriptionPricingPhasesListEnum?.LastOrDefault();
            var offerCount = subscriptionPricingPhasesListEnum?.Count;
            AndroidJavaObject? subscriptionTrialPricingPhase = null;
            AndroidJavaObject? subscriptionIntroPricingPhase = null;
            if (subscriptionPricingPhasesListEnum != null)
            {
                for (var i = subscriptionPricingPhasesListEnum.Count - 2; i >= 0; i--)
                {
                    var offer = subscriptionPricingPhasesListEnum[i];
                    var isFreeTrial = offer.Call<long>("getPriceAmountMicros") == 0;
                    if (isFreeTrial)
                    {
                        subscriptionTrialPricingPhase = offer;
                    }
                    else
                    {
                        subscriptionIntroPricingPhase = offer;
                    }
                }
            }

            var price = oneTimePurchaseOffer?.Call<string>("getFormattedPrice") ?? subscriptionBasePricingPhase?.Call<string>("getFormattedPrice");
            var priceAmount = oneTimePurchaseOffer != null ?
                Convert.ToDecimal(oneTimePurchaseOffer.Call<long>("getPriceAmountMicros") > 0 ? oneTimePurchaseOffer.Call<long>("getPriceAmountMicros") / 1000000.0 : 0) :
                Convert.ToDecimal(subscriptionBasePricingPhase?.Call<long>("getPriceAmountMicros") > 0 ? subscriptionBasePricingPhase.Call<long>("getPriceAmountMicros") / 1000000.0 : 0);
            var title = productDetails.Call<string>("getTitle");
            var description = productDetails.Call<string>("getDescription");
            var priceCurrencyCode = oneTimePurchaseOffer?.Call<string>("getPriceCurrencyCode") ?? subscriptionBasePricingPhase?.Call<string>("getPriceCurrencyCode");

            var subscriptionPeriod = subscriptionBasePricingPhase?.Call<string>("getBillingPeriod");
            var freeTrialPeriod = subscriptionTrialPricingPhase?.Call<string>("getBillingPeriod");
            var introductoryPrice = subscriptionIntroPricingPhase?.Call<string>("getFormattedPrice");
            var introductoryPriceAmountMicros = subscriptionIntroPricingPhase == null ? 0 : Convert.ToDecimal((subscriptionIntroPricingPhase?.Call<long>("getPriceAmountMicros") ?? 0.0) / 1000000.0);
            var introductoryPricePeriod = subscriptionIntroPricingPhase?.Call<string>("getBillingPeriod");
            var introductoryPriceCycles = subscriptionIntroPricingPhase?.Call<int>("getBillingCycleCount") ?? 0;

            var productDetailsJsonDic = new Dictionary<string, object>();
            productDetailsJsonDic["productId"] = productId;
            productDetailsJsonDic["type"] = productDetails.Call<string>("getProductType");
            productDetailsJsonDic["title"] = title;
            productDetailsJsonDic["name"] = productDetails.Call<string>("getName");
            productDetailsJsonDic["description"] = description;
            productDetailsJsonDic["price"] = price ?? "";
            productDetailsJsonDic["price_amount_micros"] = priceAmount.ToString();
            productDetailsJsonDic["price_currency_code"] = priceCurrencyCode ?? "";

            if (subscriptionBasePricingPhase != null)
            {
                productDetailsJsonDic["subscriptionPeriod"] = subscriptionPeriod ?? "";
            }

            if (subscriptionTrialPricingPhase != null)
            {
                productDetailsJsonDic["freeTrialPeriod"] = freeTrialPeriod ?? "";
            }

            if (subscriptionIntroPricingPhase != null)
            {
                productDetailsJsonDic["introductoryPrice"] = introductoryPrice ?? "";
                productDetailsJsonDic["introductoryPricePeriod"] = introductoryPricePeriod ?? "";
                productDetailsJsonDic["introductoryPriceCycles"] = introductoryPriceCycles;
                productDetailsJsonDic["introductoryPriceAmountMicros"] = introductoryPriceAmountMicros;
            }
            var originalJson = productDetailsJsonDic.toJson();

            var productMetadata = new GoogleProductMetadata(
                price,
                title,
                description,
                priceCurrencyCode,
                priceAmount)
            {
                originalJson = originalJson,
                subscriptionPeriod = subscriptionPeriod,
                introductoryPrice = introductoryPrice,
                introductoryPriceCycles = introductoryPriceCycles,
                introductoryPricePeriod = introductoryPricePeriod,
                freeTrialPeriod = freeTrialPeriod
            };

            var product = new ProductDescription(
                productId,
                productMetadata,
                "",
                ""
            );

            return product;
        }
    }
}
