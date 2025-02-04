using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The main controller for Applications using Unity Purchasing.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    class PurchasingManager : IStoreController
    {
        public ProductCollection products { get; } = new();

        public void InitiatePurchase(Product product, string payload)
        {
            InitiatePurchase(product.definition.id);
        }

        public void InitiatePurchase(string productId, string payload)
        {
            InitiatePurchase(productId);
        }

        public void InitiatePurchase(Product product)
        {
            InitiatePurchase(product.definition.id);
        }

        public void InitiatePurchase(string productId)
        {
            var purchaseService = PurchaseServiceProvider.GetDefaultPurchaseService();
            purchaseService.AddPurchaseFailedAction(OnPurchaseFailed);
            purchaseService.AddConfirmedOrderUpdatedAction(OnPurchaseConfirmed);
            purchaseService.AddPendingOrderUpdatedAction(OnPendingOrderUpdatedAction);
            purchaseService.Purchase(CreateCart(productId));
        }

        void OnPurchaseFailed(FailedOrder failedOrder)
        {
            UnityPurchasing.m_StoreListener.OnPurchaseFailed(failedOrder.CartOrdered.Items().FirstOrDefault()?.Product, failedOrder.FailureReason);
        }

        void OnPurchaseConfirmed(ConfirmedOrder confirmedOrder)
        {
            var product = confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product != null)
            {
                product.receipt = confirmedOrder.Info.Receipt;
                var localProduct = ProductServiceProvider.GetDefaultProductService().GetProducts().FirstOrDefault(p => p.definition.id == product.definition.id);
                if (localProduct != null)
                {
                    localProduct.receipt = confirmedOrder.Info.Receipt;
                }

                UnityPurchasing.m_StoreListener.ProcessPurchase(new PurchaseEventArgs(product));
            }
        }

        void OnPendingOrderUpdatedAction(PendingOrder pendingOrder)
        {
            var product = pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product != null)
            {
                product.receipt = pendingOrder.Info.Receipt;
                var localProduct = ProductServiceProvider.GetDefaultProductService().GetProducts().FirstOrDefault(p => p.definition.id == product.definition.id);
                if (localProduct != null)
                {
                    localProduct.receipt = pendingOrder.Info.Receipt;
                }

                UnityPurchasing.m_StoreListener.ProcessPurchase(new PurchaseEventArgs(product));
            }
        }

        static ICart CreateCart(string productId)
        {
            var product = FindProductByProductId(productId);
            var cartItem = new CartItem(product);
            return new Cart(cartItem);
        }

        static Product FindProductByProductId(string productId)
        {
            var productService = ProductServiceProvider.GetDefaultProductService();
            return productService.GetProducts().FirstOrDefault(product => product.definition.id == productId);
        }

        public void FetchAdditionalProducts(HashSet<ProductDefinition> additionalProducts, Action successCallback, Action<InitializationFailureReason, string> failCallback)
        {
            var productService = ProductServiceProvider.GetDefaultProductService();
            productService.AddProductsUpdatedAction(_ =>
            {
                successCallback();
            });
            productService.AddProductsFetchFailedAction(failedFetch =>
            {
                failCallback(InitializationFailureReason.NoProductsAvailable, failedFetch.FailureReason);
            });

            productService.FetchProductsWithNoRetries(new List<ProductDefinition>(additionalProducts));


        }

        public void ConfirmPendingPurchase(Product product)
        {
            var purchaseService = PurchaseServiceProvider.GetDefaultPurchaseService();
            purchaseService.AddCheckEntitlementAction(entitlement =>
            {
                if (entitlement.EntitlementOrder is PendingOrder order)
                {
                    purchaseService.ConfirmOrder(order);
                }
            });
            purchaseService.IsProductEntitled(product);
            purchaseService.ConfirmOrder(CreatePendingOrderFromProduct(product));
        }

        static PendingOrder CreatePendingOrderFromProduct(Product product)
        {
            var cartItem = new CartItem(product);
            var cart = new Cart(cartItem);
            var orderInfo = new OrderInfo(string.Empty, string.Empty, string.Empty);
            return new PendingOrder(cart, orderInfo);
        }
    }
}
