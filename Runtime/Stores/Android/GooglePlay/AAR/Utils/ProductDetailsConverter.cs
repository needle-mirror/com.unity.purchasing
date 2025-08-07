#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.MiniJSON;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Utils
{
    [Preserve]
    class ProductDetailsConverter : IProductDetailsConverter
    {
        public List<ProductDescription> ConvertOnQueryProductDetailsResponse(IEnumerable<AndroidJavaObject> productDetails, IReadOnlyCollection<ProductDefinition> productDefinitions)
        {
            var productDescriptions = new List<ProductDescription>();
            var productTypes = new Dictionary<string, ProductType>();
            var processedIds = new HashSet<string>();

            foreach (var definition in productDefinitions)
            {
                try
                {
                    productTypes.Add(definition.storeSpecificId, definition.type);
                }
                catch (ArgumentException ex) when (ex.Message.Contains("same key"))
                {
                    Debug.LogError($"Duplicate product definition found with store specific ID: {definition.storeSpecificId}. " +
                        $"Only the first definition will be used.");
                }
            }

            foreach (var detail in productDetails)
            {
                try
                {
                    var productId = GetProductId(detail);
                    if (string.IsNullOrEmpty(productId))
                    {
                        Debug.LogWarning($"Skipping product with empty or null ID from Google Play store response. Detail: {detail?.ToString() ?? "null"}");
                        continue;
                    }

                    // Skip duplicate product IDs
                    if (!processedIds.Add(productId))
                    {
                        Debug.LogError($"Duplicate product ID found in Google Play store response: {productId}. " +
                            $"This product will be ignored to prevent dictionary key conflicts. " +
                            $"Please ensure each product has a unique store-specific ID.");
                        continue;
                    }

                    // Check if the product ID exists in the definitions
                    if (productTypes.TryGetValue(productId, out var productType))
                    {
                        productDescriptions.Add(ConvertToProductDescription(detail, productType));
                    }
                    else
                    {
                        Debug.LogWarning($"Product with ID {productId} found in store but not in definitions. Skipping.");
                    }
                }
                catch (Exception e)
                {
                    Debug.unityLogger.LogIAPException(e);
                }
            }
            return productDescriptions;
        }

        public string? GetProductId(AndroidJavaObject productDetails)
        {
            try
            {
                return productDetails.Call<string>("getProductId");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get product ID: {e.Message}");
                return null;
            }
        }

        public ProductDescription ConvertToProductDescription(AndroidJavaObject productDetails, ProductType productType)
        {
            try
            {
                return BuildProductDescription(productDetails, productType);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Build a `ProductDescription` from a ProductDetails `AndroidJavaObject`
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/ProductDetails">Learn more about ProductDetails</a>
        /// </summary>
        /// <param name="productDetails">`AndroidJavaObject` of ProductDetails</param>
        /// <returns>`ProductDescription` representation of a ProductDetails</returns>
        internal static ProductDescription BuildProductDescription(AndroidJavaObject productDetails, ProductType productType = ProductType.Unknown)
        {
            // TODO: IAP-2833 - Clean for one time vs subscription
            var productId = productDetails.Call<string>("getProductId");
            using var oneTimePurchaseOffer = productDetails.Call<AndroidJavaObject>("getOneTimePurchaseOfferDetails");
            using var subscriptionOffer = productDetails.Call<AndroidJavaObject>("getSubscriptionOfferDetails").Enumerate().FirstOrDefault();
            using var subscriptionPricingPhases = subscriptionOffer?.Call<AndroidJavaObject>("getPricingPhases");
            using var subscriptionPricingPhasesList = subscriptionPricingPhases?.Call<AndroidJavaObject>("getPricingPhaseList");
            var subscriptionPricingPhasesListEnum = subscriptionPricingPhasesList?.Enumerate().ToList();

            var subscriptionBasePricingPhase = subscriptionPricingPhasesListEnum?.LastOrDefault();
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
            var priceAmountMicros = oneTimePurchaseOffer != null ?
                Convert.ToDecimal(oneTimePurchaseOffer.Call<long>("getPriceAmountMicros") > 0 ? oneTimePurchaseOffer.Call<long>("getPriceAmountMicros") : 0) :
                Convert.ToDecimal(subscriptionBasePricingPhase?.Call<long>("getPriceAmountMicros") > 0 ? subscriptionBasePricingPhase.Call<long>("getPriceAmountMicros") : 0);
            var priceAmount = priceAmountMicros / (decimal)1000000.0;
            var title = productDetails.Call<string>("getTitle");
            var description = productDetails.Call<string>("getDescription");
            var priceCurrencyCode = oneTimePurchaseOffer?.Call<string>("getPriceCurrencyCode") ?? subscriptionBasePricingPhase?.Call<string>("getPriceCurrencyCode");

            var subscriptionPeriod = subscriptionBasePricingPhase?.Call<string>("getBillingPeriod");
            var freeTrialPeriod = subscriptionTrialPricingPhase?.Call<string>("getBillingPeriod");
            var introductoryPrice = subscriptionIntroPricingPhase?.Call<string>("getFormattedPrice");
            var introductoryPriceAmountMicros = subscriptionIntroPricingPhase == null ? 0 : Convert.ToDecimal(subscriptionIntroPricingPhase.Call<long>("getPriceAmountMicros"));
            var introductoryPricePeriod = subscriptionIntroPricingPhase?.Call<string>("getBillingPeriod");
            var introductoryPriceCycles = subscriptionIntroPricingPhase?.Call<int>("getBillingCycleCount") ?? 0;

            var productDetailsJsonDic = new Dictionary<string, object>();
            productDetailsJsonDic["productId"] = productId;
            productDetailsJsonDic["type"] = productDetails.Call<string>("getProductType");
            productDetailsJsonDic["title"] = title;
            productDetailsJsonDic["name"] = productDetails.Call<string>("getName");
            productDetailsJsonDic["description"] = description;
            productDetailsJsonDic["price"] = price ?? "";
            productDetailsJsonDic["price_amount_micros"] = priceAmountMicros.ToString();
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
                "",
                productType
            );

            return product;
        }
    }
}
