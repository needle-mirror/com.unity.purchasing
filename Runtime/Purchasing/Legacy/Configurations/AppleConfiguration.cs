using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    class AppleConfiguration : IAppleConfiguration
    {
        public string appReceipt => UnityIAPServices.DefaultPurchase().Apple?.appReceipt;

        public bool canMakePayments => UnityIAPServices.DefaultStore().Apple?.canMakePayments != null && UnityIAPServices.DefaultStore().Apple!.canMakePayments;

        public void SetApplePromotionalPurchaseInterceptorCallback(Action<Product> callback)
        {
            UnityIAPServices.DefaultPurchase().Apple?.SetPromotionalPurchaseInterceptorCallback(callback);
        }

        public void SetEntitlementsRevokedListener(Action<List<Product>> callback)
        {
            UnityIAPServices.DefaultPurchase().Apple?.AddEntitlementsRevokedAction(productIds =>
            {
                var products = UnityIAPServices.DefaultProduct().GetProducts().Where(product => productIds.Contains(product.definition.storeSpecificId));
                callback?.Invoke((List<Product>)products);
            });
        }
    }
}
