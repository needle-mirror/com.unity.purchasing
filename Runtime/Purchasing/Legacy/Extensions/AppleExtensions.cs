using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    class AppleExtensions : IAppleExtensions
    {
        public void RefreshAppReceipt(Action<string> successCallback, Action<string> _)
        {
            successCallback.Invoke(UnityIAPServices.DefaultPurchase().Apple?.appReceipt);
        }

        public void RestoreTransactions(Action<bool, string> callback)
        {
            UnityIAPServices.DefaultPurchase()?.RestoreTransactions(callback);
        }

        public void RegisterPurchaseDeferredListener(Action<Product> callback)
        {
            UnityIAPServices.DefaultPurchase().AddPurchaseDeferredAction(deferredOrder =>
            {
                foreach (var cartItem in deferredOrder.CartOrdered.Items())
                {
                    callback.Invoke(cartItem.Product);
                }
            });
        }

        public bool simulateAskToBuy
        {
            get => UnityIAPServices.DefaultPurchase().Apple!.simulateAskToBuy;
            set
            {
                UnityIAPServices.DefaultPurchase().Apple!.simulateAskToBuy = value;
            }
        }

        public string GetTransactionReceiptForProduct(Product _)
        {
            return UnityIAPServices.DefaultPurchase().Apple?.appReceipt;
        }

        public void SetApplicationUsername(string applicationUsername)
        {
            UnityIAPServices.DefaultStore().Apple?.SetApplicationUsername(applicationUsername);
        }

        public Dictionary<string, string> GetIntroductoryPriceDictionary()
        {

            return UnityIAPServices.DefaultProduct().Apple?.GetIntroductoryPriceDictionary();
        }

        public Dictionary<string, string> GetProductDetails()
        {
            return UnityIAPServices.DefaultProduct().Apple?.GetProductDetails();
        }

        public void ContinuePromotionalPurchases()
        {
            UnityIAPServices.DefaultPurchase().Apple?.ContinuePromotionalPurchases();
        }

        public void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible)
        {
            UnityIAPServices.DefaultProduct().Apple?.SetStorePromotionVisibility(product, visible);
        }

        public void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action errorCallback)
        {
            UnityIAPServices.DefaultProduct().Apple?.FetchStorePromotionVisibility(product, successCallback, errorCallback);
        }

        public void SetStorePromotionOrder(List<Product> products)
        {
            UnityIAPServices.DefaultProduct().Apple?.SetStorePromotionOrder(products);
        }

        public void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action errorCallback)
        {
            UnityIAPServices.DefaultProduct().Apple?.FetchStorePromotionOrder(successCallback, errorCallback);
        }

        public void PresentCodeRedemptionSheet()
        {
            UnityIAPServices.DefaultPurchase().Apple?.PresentCodeRedemptionSheet();
        }
    }
}
