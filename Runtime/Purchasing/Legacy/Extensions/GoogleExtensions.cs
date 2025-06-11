using System;
using System.Linq;
using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
    class GoogleExtensions : IGooglePlayStoreExtensions
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
            UpgradeDowngradeSubscription(oldSku, newSku, GooglePlayProrationMode.ImmediateWithoutProration);
        }

        public void UpgradeDowngradeSubscription(string oldSku, string newSku, int desiredProrationMode)
        {
            UpgradeDowngradeSubscription(oldSku, newSku, (GooglePlayProrationMode)desiredProrationMode);
        }

        public void UpgradeDowngradeSubscription(string oldSku, string newSku, GooglePlayProrationMode desiredProrationMode)
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

            UnityIAPServices.Purchase(GooglePlay.Name).Google?.UpgradeDowngradeSubscription(oldProduct, newProduct, desiredProrationMode);
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
                    return (purchaseService.Google?.IsOrderDeferred(candidateOrder) == true);
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
                    var purchaseState = purchaseService.Google!.GetPurchaseState(candidateOrder);
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
