using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
// Obsolete: IAppleConfiguration
#pragma warning disable 618, 612
    class AppleConfiguration: IAppleConfiguration
#pragma warning restore 618, 612
    {
// Obsolete: IAppleStoreExtendedPurchaseService.appReceipt
#pragma warning disable 618, 612
        public string appReceipt => UnityIAPServices.DefaultPurchase().Apple?.appReceipt;
#pragma warning restore 618, 612

        public bool canMakePayments => UnityIAPServices.DefaultStore().Apple?.canMakePayments != null && UnityIAPServices.DefaultStore().Apple!.canMakePayments;

        public void SetApplePromotionalPurchaseInterceptorCallback(Action<Product> callback)
        {
            var appleStoreExtendedPurchaseService = UnityIAPServices.DefaultPurchase().Apple;
            if (appleStoreExtendedPurchaseService != null)
            {
                appleStoreExtendedPurchaseService.OnPromotionalPurchaseIntercepted += callback;
            }
        }

        public void SetEntitlementsRevokedListener(Action<List<Product>> callback)
        {
            var appleStoreExtendedPurchaseService = UnityIAPServices.DefaultPurchase().Apple;
            if (appleStoreExtendedPurchaseService != null)
            {
                appleStoreExtendedPurchaseService.OnEntitlementRevoked += productIds =>
                {
                    var products = UnityIAPServices.DefaultProduct().GetProducts().Where(product => productIds.Contains(product.baseListing?.definition.storeSpecificId));
                    callback?.Invoke((List<Product>)products);
                };
            }
        }
    }
}
