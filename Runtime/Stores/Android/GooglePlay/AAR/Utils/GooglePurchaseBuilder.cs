using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Utils
{
    class GooglePurchaseBuilder : IGooglePurchaseBuilder
    {
        IGoogleCachedQuerySkuDetailsService m_CachedQuerySkuDetailsService;
        ILogger m_Logger;

        public GooglePurchaseBuilder(IGoogleCachedQuerySkuDetailsService cachedQuerySkuDetailsService, ILogger logger)
        {
            m_CachedQuerySkuDetailsService = cachedQuerySkuDetailsService;
            m_Logger = logger;
        }

        public IEnumerable<IGooglePurchase> BuildPurchases(IEnumerable<IAndroidJavaObjectWrapper> purchases)
        {
            return purchases.Select(BuildPurchase)
                .IgnoreExceptions<IGooglePurchase, ArgumentException>(LogWarningForException);
        }

        void LogWarningForException(Exception exception)
        {
            m_Logger.LogIAPWarning(exception.Message);
        }

        public IGooglePurchase BuildPurchase(IAndroidJavaObjectWrapper purchase)
        {
            var cachedSkuDetails = m_CachedQuerySkuDetailsService.GetCachedQueriedSkus().Wrap();
            var purchaseSkus = purchase.Call<AndroidJavaObject>("getSkus").Enumerate<string>();

            try
            {
                var skuDetails = TryFindAllSkuDetails(purchaseSkus, cachedSkuDetails);
                return new GooglePurchase(purchase, skuDetails);
            }
            catch (InvalidOperationException)
            {
                var transactionId = purchase.Call<string>("getPurchaseToken");
                throw new ArgumentException($"Unable to process purchase with transaction id: {transactionId} because the product details associated with the purchased products were not found.");
            }
        }

        static IEnumerable<IAndroidJavaObjectWrapper> TryFindAllSkuDetails(IEnumerable<string> skus, IEnumerable<IAndroidJavaObjectWrapper> skuDetails)
        {
            return skus.Select(sku => skuDetails.First(
                skuDetail => sku == skuDetail.Call<string>("getSku")));
        }
    }
}
