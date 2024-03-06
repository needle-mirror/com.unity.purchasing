using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Utils
{
    class GooglePurchaseBuilder : IGooglePurchaseBuilder
    {
        readonly IGoogleCachedQuerySkuDetailsService m_CachedQuerySkuDetailsService;
        readonly ILogger m_Logger;

        public GooglePurchaseBuilder(IGoogleCachedQuerySkuDetailsService cachedQuerySkuDetailsService, ILogger logger)
        {
            m_CachedQuerySkuDetailsService = cachedQuerySkuDetailsService;
            m_Logger = logger;
        }

        public IEnumerable<IGooglePurchase> BuildPurchases(IEnumerable<AndroidJavaObject> purchases)
        {
            return purchases.Select(BuildPurchase)
                .IgnoreExceptions<IGooglePurchase, ArgumentException>(LogWarningForException).ToList();
        }

        void LogWarningForException(Exception exception)
        {
            m_Logger.LogIAPWarning(exception.Message);
        }

        public IGooglePurchase BuildPurchase(AndroidJavaObject purchase)
        {
            var cachedSkuDetails = m_CachedQuerySkuDetailsService.GetCachedQueriedSkus();
            using var getSkusObj = purchase.Call<AndroidJavaObject>("getSkus");
            var purchaseSkus = getSkusObj.Enumerate<string>();

            try
            {
                var skuDetails = TryFindAllSkuDetails(purchaseSkus, cachedSkuDetails);
                return new GooglePurchase(purchase, skuDetails);
            }
            catch (InvalidOperationException)
            {
                var orderId = purchase.Call<string>("getOrderId");
                var purchaseToken = purchase.Call<string>("getPurchaseToken");
                throw new ArgumentException($"Unable to process purchase with order id: {orderId} and purchase token: {purchaseToken} because the product details associated with the purchased products were not found.");
            }
        }

        static IEnumerable<AndroidJavaObject> TryFindAllSkuDetails(IEnumerable<string> skus, IEnumerable<AndroidJavaObject> skuDetails)
        {
            return skus.Select(sku => skuDetails.First(
                skuDetail => sku == skuDetail.Call<string>("getSku")));
        }
    }
}
