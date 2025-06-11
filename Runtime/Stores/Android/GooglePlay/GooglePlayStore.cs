#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Purchasing.Extension;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStore : InternalStore, IGooglePlayStore
    {
        readonly IGooglePlayStoreConnectionService m_ConnectionService;
        readonly IGooglePlayStoreFetchProductsService m_FetchProductsService;
        readonly IGooglePlayStorePurchaseService m_StorePurchaseService;
        readonly IGooglePlayStoreFetchPurchasesService m_PlayStoreFetchPurchasesService;
        readonly IGooglePlayStoreCheckEntitlementService m_CheckEntitlementsService;
        readonly IGooglePlayStoreFinishTransactionService m_FinishTransactionService;
        readonly IGooglePlayStoreChangeSubscriptionService m_ChangeSubscriptionService;
        readonly IGooglePurchaseCallback m_GooglePurchaseCallback;
        readonly ICartValidator m_CartValidator;
        internal IGoogleBillingClient m_BillingClient;

        [Preserve]
        internal GooglePlayStore(IGooglePlayStoreFetchProductsService fetchProductsService,
            IGooglePlayStorePurchaseService storePurchaseService,
            IGooglePlayStoreFetchPurchasesService playStoreFetchPurchasesService,
            IGooglePlayStoreFinishTransactionService transactionService,
            IGooglePlayStoreChangeSubscriptionService changeSubscriptionService,
            IGooglePlayStoreCheckEntitlementService checkEntitlementsService,
            IGooglePurchaseCallback googlePurchaseCallback,
            ICartValidator cartValidator,
            IGooglePlayStoreConnectionService connectionService,
            IGoogleBillingClient billingClient)
        {
            m_FetchProductsService = fetchProductsService;
            m_StorePurchaseService = storePurchaseService;
            m_PlayStoreFetchPurchasesService = playStoreFetchPurchasesService;
            m_CheckEntitlementsService = checkEntitlementsService;
            m_FinishTransactionService = transactionService;
            m_ChangeSubscriptionService = changeSubscriptionService;
            m_GooglePurchaseCallback = googlePurchaseCallback;
            m_CartValidator = cartValidator;
            m_ConnectionService = connectionService;
            m_BillingClient = billingClient;
        }

        /// <summary>
        /// Call the Google Play Store to retrieve the store products. The `IStoreCallback` will be call with the retrieved products.
        /// </summary>
        /// <param name="products">The catalog of products to retrieve the store information from</param>
        public override void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            m_FetchProductsService.FetchProducts(products);
        }

        /// <summary>
        /// Fetch previously existing purchases.
        /// </summary>
        public override void FetchPurchases()
        {
            FetchPurchasesInternal();
        }

        void FetchPurchasesInternal()
        {
            m_PlayStoreFetchPurchasesService.FetchPurchases();
        }

        /// <summary>
        /// Call the Google Play Store to purchase a cart. The `IStoreCallback` will be call when the purchase is successful.
        /// </summary>
        /// <param name="cart">The cart to purchase</param>
        public override void Purchase(ICart cart)
        {
            m_CartValidator.Validate(cart);
            var productDefinition = cart.Items().First().Product.definition;
            m_StorePurchaseService.Purchase(productDefinition);
        }

        /// <summary>
        /// Call the Google Play Store to change a subscription. The `IStorePurchaseCallback` will be called.
        /// </summary>
        /// <param name="product">The new subscription to buy</param>
        /// <param name="currentOrder">The current order containing the subscription to be unsubscribed to</param>
        /// <param name="desiredReplacementMode">The desired proration mode for the subscription change</param>
        public void ChangeSubscription(ProductDefinition product, Order currentOrder,
            GooglePlayReplacementMode? desiredReplacementMode)
        {
            m_ChangeSubscriptionService.ChangeSubscription(product, currentOrder, desiredReplacementMode);
        }

        public override void FinishTransaction(PendingOrder pendingOrder)
        {
            m_CartValidator.Validate(pendingOrder.CartOrdered);
            var productDefinition = pendingOrder.CartOrdered.Items().First().Product.definition;
            m_FinishTransactionService.FinishTransaction(productDefinition, pendingOrder.Info.TransactionID);
        }

        public override void Connect()
        {
            m_ConnectionService.Connect();
        }

        public override void CheckEntitlement(ProductDefinition product)
        {
            m_CheckEntitlementsService.CheckEntitlement(product);
        }

        /// <summary>
        /// Add an additional fetch Callback for FetchPurchases calls from the GooglePlay Store
        /// </summary>
        /// <param name="fetchPurchaseCallback">Implementation of the purchase Callback Interface</param>
        public override void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchPurchaseCallback)
        {
            m_PlayStoreFetchPurchasesService.SetPurchaseFetchCallback(fetchPurchaseCallback);
            m_GooglePurchaseCallback.SetPurchaseFetchCallback(fetchPurchaseCallback);
        }

        public override void SetPurchaseCallback(IStorePurchaseCallback purchaseCallback)
        {
            m_GooglePurchaseCallback.SetPurchaseCallback(purchaseCallback);
        }

        public void SetChangeSubscriptionCallback(IGooglePlayChangeSubscriptionCallback changeSubscriptionCallback)
        {
            m_GooglePurchaseCallback.SetChangeSubscriptionCallback(changeSubscriptionCallback);
        }

        public override void SetPurchaseConfirmCallback(IStorePurchaseConfirmCallback confirmCallback)
        {
            m_FinishTransactionService.SetConfirmCallback(confirmCallback);
        }

        public override void SetProductsCallback(IStoreProductsCallback productsCallback)
        {
            m_FetchProductsService.SetProductsCallback(productsCallback);
        }

        public override void SetEntitlementCheckCallback(IStoreCheckEntitlementCallback entitlementCallback)
        {
            m_CheckEntitlementsService.SetCheckEntitlementCallback(entitlementCallback);
        }

        public override void SetStoreConnectionCallback(IStoreConnectCallback storeConnectCallback)
        {
            m_ConnectionService.SetConnectionCallback(storeConnectCallback);
        }

        public void OnPause(bool isPaused)
        {
            if (!isPaused)
            {
                FetchPurchasesInternal();
            }
        }

        public IGooglePurchase GetGooglePurchase(string purchaseToken)
        {
            return m_PlayStoreFetchPurchasesService.GetGooglePurchase(purchaseToken);
        }
    }
}
