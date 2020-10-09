using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreService : IGooglePlayStoreService
    {
        const int k_NullProrationMode = -1;

        IGoogleBillingClient m_BillingClient;
        bool m_IsConnectedToGoogle;
        IQuerySkuDetailsService m_QuerySkuDetailsService;
        Queue<ProductDescriptionQuery> m_ProductsToQuery = new Queue<ProductDescriptionQuery>();
        Queue<Action<List<GooglePurchase>>> m_OnPurchaseSucceededQueue = new Queue<Action<List<GooglePurchase>>>();
        IGooglePurchaseService m_GooglePurchaseService;
        IGoogleFinishTransactionService m_GoogleFinishTransactionService;
        IGoogleQueryPurchasesService m_GoogleQueryPurchasesService;
        IGooglePriceChangeService m_GooglePriceChangeService;

        internal GooglePlayStoreService(
            IGoogleBillingClient billingClient,
            IQuerySkuDetailsService querySkuDetailsService,
            IGooglePurchaseService purchaseService,
            IGoogleFinishTransactionService finishTransactionService,
            IGoogleQueryPurchasesService queryPurchasesService,
            IBillingClientStateListener billingClientStateListener,
            IGooglePriceChangeService priceChangeService)
        {
            m_BillingClient = billingClient;
            m_QuerySkuDetailsService = querySkuDetailsService;
            m_GooglePurchaseService = purchaseService;
            m_GoogleFinishTransactionService = finishTransactionService;
            m_GoogleQueryPurchasesService = queryPurchasesService;
            m_GooglePriceChangeService = priceChangeService;

            InitConnectionWithGooglePlay(billingClientStateListener);
        }

        void InitConnectionWithGooglePlay(IBillingClientStateListener billingClientStateListener)
        {
            billingClientStateListener.RegisterOnConnected(OnConnected);
            billingClientStateListener.RegisterOnDisconnected(OnDisconnected);
            m_BillingClient.StartConnection(billingClientStateListener);
        }

        void OnConnected()
        {
            m_IsConnectedToGoogle = true;
            DequeueQueryProducts();
            DequeueFetchPurchases();
        }

        void DequeueQueryProducts()
        {
            while (m_ProductsToQuery.Count > 0)
            {
                ProductDescriptionQuery productDescriptionQuery = m_ProductsToQuery.Dequeue();
                m_QuerySkuDetailsService.QueryAsyncSku(productDescriptionQuery.products, productDescriptionQuery.onProductsReceived);
            }
        }

        void DequeueFetchPurchases()
        {
            while (m_OnPurchaseSucceededQueue.Count > 0)
            {
                Action<List<GooglePurchase>> onPurchaseSucceed = m_OnPurchaseSucceededQueue.Dequeue();
                FetchPurchases(onPurchaseSucceed);
            }
        }

        void OnDisconnected()
        {
            m_IsConnectedToGoogle = false;
        }

        public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived)
        {
            if (m_IsConnectedToGoogle)
            {
                m_QuerySkuDetailsService.QueryAsyncSku(products, onProductsReceived);
            }
            else
            {
                m_ProductsToQuery.Enqueue(new ProductDescriptionQuery(products, onProductsReceived));
            }
        }

        public void Purchase(ProductDefinition product)
        {
            Purchase(product, null, k_NullProrationMode);
        }

        public void Purchase(ProductDefinition product, Product oldProduct, int desiredProrationMode)
        {
            m_GooglePurchaseService.Purchase(product, oldProduct, desiredProrationMode);
        }

        public void FinishTransaction(ProductDefinition product, string purchaseToken, Action<ProductDefinition, GooglePurchase, GoogleBillingResult, string> onConsume, Action<ProductDefinition, GooglePurchase, GoogleBillingResult> onAcknowledge)
        {
            m_GoogleFinishTransactionService.FinishTransaction(product, purchaseToken, onConsume, onAcknowledge);
        }

        public void FetchPurchases(Action<List<GooglePurchase>> onQueryPurchaseSucceed)
        {
            if (m_IsConnectedToGoogle)
            {
                m_GoogleQueryPurchasesService.QueryPurchases(onQueryPurchaseSucceed);
            }
            else
            {
                m_OnPurchaseSucceededQueue.Enqueue(onQueryPurchaseSucceed);
            }
        }

        public void SetObfuscatedAccountId(string obfuscatedAccountId)
        {
            m_BillingClient.SetObfuscationAccountId(obfuscatedAccountId);
        }

        public void SetObfuscatedProfileId(string obfuscatedProfileId)
        {
            m_BillingClient.SetObfuscationProfileId(obfuscatedProfileId);
        }

        public void EndConnection()
        {
            m_IsConnectedToGoogle = false;
            m_BillingClient.EndConnection();
        }

        public void ConfirmSubscriptionPriceChange(ProductDefinition product, Action<GoogleBillingResult> onPriceChangeAction)
        {
            m_GooglePriceChangeService.PriceChange(product, onPriceChangeAction);
        }
    }
}
