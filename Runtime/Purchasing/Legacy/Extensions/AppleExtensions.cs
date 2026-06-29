using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
// Obsolete: IAppleExtensions
#pragma warning disable 618, 612
    class AppleExtensions : IAppleExtensions
#pragma warning restore 618, 612
    {
        public void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback)
        {
// Obsolete: IAppleStoreExtendedPurchaseService.RefreshAppReceipt(Action<string>, Action<string>)
#pragma warning disable 618, 612
            UnityIAPServices.DefaultPurchase().Apple?.RefreshAppReceipt(successCallback, errorCallback);
#pragma warning restore 618, 612
        }

        public void RestoreTransactions(Action<bool, string> callback)
        {
            UnityIAPServices.DefaultPurchase()?.RestoreTransactions(callback);
        }

        public void RegisterPurchaseDeferredListener(Action<Product> callback)
        {
            UnityIAPServices.DefaultPurchase().OnPurchaseDeferred += deferredOrder =>
            {
                foreach (var cartItem in deferredOrder.CartOrdered.Items())
                {
                    callback.Invoke(cartItem.Product);
                }
            };
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
// Obsolete: IAppleStoreExtendedPurchaseService.appReceipt
#pragma warning disable 618, 612
            return UnityIAPServices.DefaultPurchase().Apple?.appReceipt;
#pragma warning restore 618, 612
        }

        public void SetApplicationUsername(string applicationUsername)
        {
            try
            {
                var accountTokenGuid = new Guid(applicationUsername);
                UnityIAPServices.DefaultStore().Apple?.SetAppAccountToken(accountTokenGuid);
            }
            catch (FormatException)
            {
                Debug.LogError($"SetApplicationUsername failed: '{applicationUsername}' is not a valid GUID.");
            }
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
            UnityIAPServices.DefaultProduct().Apple?.SetStorePromotionVisibility(product.baseListing.id, visible);
        }

        public void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action errorCallback)
        {
            void ErrorCallbackWrapper(string _)
            {
                errorCallback.Invoke();
            }
            UnityIAPServices.DefaultProduct().Apple?.FetchStorePromotionVisibility(product.baseListing.id, successCallback, ErrorCallbackWrapper);
        }

        public void SetStorePromotionOrder(List<Product> products)
        {
            UnityIAPServices.DefaultProduct().Apple?.SetStorePromotionOrder(products);
        }

        public void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action errorCallback)
        {
            void ErrorCallbackWrapper(string _)
            {
                errorCallback.Invoke();
            }
            UnityIAPServices.DefaultProduct().Apple?.FetchStorePromotionOrder(successCallback, ErrorCallbackWrapper);
        }

        public void PresentCodeRedemptionSheet()
        {
            UnityIAPServices.DefaultPurchase().Apple?.PresentCodeRedemptionSheet();
        }
    }
}
