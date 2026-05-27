using System;
using System.Linq;
using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
// Obsolete: IGooglePlayStoreExtensions
#pragma warning disable 618, 612
    class GoogleExtensions : IGooglePlayStoreExtensions
#pragma warning restore 618, 612
    {
        public void RestoreTransactions(Action<bool, string> callback)
        {
            UnityIAPServices.Purchase(GooglePlay.Name)?.RestoreTransactions(callback);
        }

        public void ConfirmSubscriptionPriceChange(string productId, Action<bool> callback)
        {
        }

        public void UpgradeDowngradeSubscription(string oldSku, string newSku)
        {
// Obsolete: GooglePlayProrationMode
#pragma warning disable 618, 612
            UpgradeDowngradeSubscription(oldSku, newSku, GooglePlayProrationMode.ImmediateWithoutProration);
#pragma warning restore 618, 612
        }

        public void UpgradeDowngradeSubscription(string oldSku, string newSku, int desiredProrationMode)
        {
// Obsolete: GooglePlayProrationMode
#pragma warning disable 618, 612
            UpgradeDowngradeSubscription(oldSku, newSku, (GooglePlayProrationMode)desiredProrationMode);
#pragma warning restore 618, 612
        }

// Obsolete: GooglePlayProrationMode
#pragma warning disable 618, 612
        public void UpgradeDowngradeSubscription(string oldSku, string newSku, GooglePlayProrationMode desiredProrationMode)
#pragma warning restore 618, 612
        {
            var purchaseService = UnityIAPServices.Purchase(GooglePlay.Name);
            var productService = UnityIAPServices.Product(GooglePlay.Name);

            var candidateOrders = purchaseService.GetPurchases();
            Product oldProduct = null;
            foreach (var candidateOrder in candidateOrders)
            {
                var foundProduct = candidateOrder.CartOrdered.Items().FirstOrDefault(cartItem => cartItem.Product.definition.storeSpecificId == oldSku);
                if (foundProduct != null)
                {
                    oldProduct = foundProduct.Product;
                    break;
                }
            }

            var newProduct = productService.GetProducts().FirstOrDefault(product => product.definition.storeSpecificId == newSku);

// Obsolete: IGooglePlayStoreExtendedPurchaseService.UpgradeDowngradeSubscription(Product, Product, GooglePlayProrationMode)
#pragma warning disable 618, 612
            UnityIAPServices.Purchase(GooglePlay.Name).Google?.UpgradeDowngradeSubscription(oldProduct, newProduct, desiredProrationMode);
#pragma warning restore 618, 612
        }

        public bool IsPurchasedProductDeferred(Product product)
        {
            var purchaseService = UnityIAPServices.Purchase(GooglePlay.Name);
            var candidateOrders = purchaseService.GetPurchases().Where(order => (order is PendingOrder || order is DeferredOrder));
            foreach (var candidateOrder in candidateOrders)
            {
                var foundProduct = candidateOrder.CartOrdered.Items().FirstOrDefault(cartItem => cartItem.Product.definition.id == product.definition.id);
                if (foundProduct != null)
                {
// Obsolete: IGooglePlayStoreExtendedPurchaseService.IsOrderDeferred(Order)
#pragma warning disable 618, 612
                    return (purchaseService.Google?.IsOrderDeferred(candidateOrder) == true);
#pragma warning restore 618, 612
                }
            }

            return false;
        }

        public GooglePurchaseState GetPurchaseState(Product product)
        {
            var purchaseService = UnityIAPServices.Purchase(GooglePlay.Name);
            if (purchaseService.Google == null)
            {
                throw new Exception("Google purchase service unavailable");
            }

            var candidateOrders = purchaseService.GetPurchases();
            foreach (var candidateOrder in candidateOrders)
            {
                var foundProduct = candidateOrder.CartOrdered.Items().FirstOrDefault(cartItem => cartItem.Product.definition.id == product.definition.id);
                if (foundProduct != null)
                {
// Obsolete: IGooglePlayStoreExtendedPurchaseService.GetPurchaseState(Order)
#pragma warning disable 618, 612
                    var purchaseState = purchaseService.Google!.GetPurchaseState(candidateOrder);
#pragma warning restore 618, 612
                    if (purchaseState != null)
                    {
                        return (GooglePurchaseState)purchaseState;
                    }
                }
            }

            throw new Exception($"Product {product.definition.id} was not purchased.");
        }

        public string GetObfuscatedAccountId(Product product)
        {
            var purchaseService = UnityIAPServices.Purchase(GooglePlay.Name);
            if (purchaseService.Google == null)
            {
                return null;
            }

            var candidateOrders = purchaseService.GetPurchases();
            foreach (var candidateOrder in candidateOrders)
            {
                var foundProduct = candidateOrder.CartOrdered.Items().FirstOrDefault(cartItem => cartItem.Product.definition.id == product.definition.id);
                if (foundProduct != null)
                {
                    return purchaseService.Google!.GetObfuscatedAccountId(candidateOrder);
                }
            }

            return null;
        }

        public string GetObfuscatedProfileId(Product product)
        {
            var purchaseService = UnityIAPServices.Purchase(GooglePlay.Name);
            if (purchaseService.Google == null)
            {
                return null;
            }

            var candidateOrders = purchaseService.GetPurchases();
            foreach (var candidateOrder in candidateOrders)
            {
                var foundProduct = candidateOrder.CartOrdered.Items().FirstOrDefault(cartItem => cartItem.Product.definition.id == product.definition.id);
                if (foundProduct != null)
                {
                    return purchaseService.Google!.GetObfuscatedProfileId(candidateOrder);
                }
            }

            return null;
        }
    }
}
