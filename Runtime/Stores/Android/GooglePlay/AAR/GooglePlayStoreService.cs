using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreService : IGooglePlayStoreService
    {
        const int k_MaxConnectionAttempts = 1;

        int m_CurrentConnectionAttempts;

        IGoogleBillingClient m_BillingClient;
        IBillingClientStateListener m_BillingClientStateListener;
        IQuerySkuDetailsService m_QuerySkuDetailsService;
        Queue<ProductDescriptionQuery> m_ProductsToQuery = new Queue<ProductDescriptionQuery>();
        ConcurrentQueue<Action<List<IGooglePurchase>>> m_OnPurchaseSucceededQueue = new ConcurrentQueue<Action<List<IGooglePurchase>>>();
        IGooglePurchaseService m_GooglePurchaseService;
        IGoogleFinishTransactionService m_GoogleFinishTransactionService;
        IGoogleQueryPurchasesService m_GoogleQueryPurchasesService;
        IGooglePriceChangeService m_GooglePriceChangeService;
        IGoogleLastKnownProductService m_GoogleLastKnownProductService;
        ITelemetryDiagnostics m_TelemetryDiagnostics;
        ILogger m_Logger;

        internal GooglePlayStoreService(IGoogleBillingClient billingClient,
            IQuerySkuDetailsService querySkuDetailsService,
            IGooglePurchaseService purchaseService,
            IGoogleFinishTransactionService finishTransactionService,
            IGoogleQueryPurchasesService queryPurchasesService,
            IBillingClientStateListener billingClientStateListener,
            IGooglePriceChangeService priceChangeService,
            IGoogleLastKnownProductService lastKnownProductService,
            ITelemetryDiagnostics telemetryDiagnostics,
            ILogger logger)
        {
            m_BillingClient = billingClient;
            m_QuerySkuDetailsService = querySkuDetailsService;
            m_GooglePurchaseService = purchaseService;
            m_GoogleFinishTransactionService = finishTransactionService;
            m_GoogleQueryPurchasesService = queryPurchasesService;
            m_GooglePriceChangeService = priceChangeService;
            m_GoogleLastKnownProductService = lastKnownProductService;
            m_BillingClientStateListener = billingClientStateListener;
            m_TelemetryDiagnostics = telemetryDiagnostics;
            m_Logger = logger;

            InitConnectionWithGooglePlay();
        }

        void InitConnectionWithGooglePlay()
        {
            m_BillingClientStateListener.RegisterOnConnected(OnConnected);
            m_BillingClientStateListener.RegisterOnDisconnected(OnDisconnected);

            StartConnection();
        }

        void StartConnection()
        {
            m_CurrentConnectionAttempts++;
            m_BillingClient.StartConnection(m_BillingClientStateListener);
        }

        public void ResumeConnection()
        {
            if (m_BillingClient.GetConnectionState() == GoogleBillingConnectionState.Disconnected)
            {
                StartConnection();
            }
        }

        public bool IsConnectionReady()
        {
            return m_BillingClient.IsReady();
        }

        void OnConnected()
        {
            m_CurrentConnectionAttempts = 0;

            DequeueQueryProducts();
            DequeueFetchPurchases();
        }

        protected virtual void DequeueQueryProducts()
        {
            var productsFailedToDequeue = new Queue<ProductDescriptionQuery>();
            var stop = false;

            while (m_ProductsToQuery.Count > 0 && !stop)
            {
                var currentConnectionState = m_BillingClient.GetConnectionState();
                switch (currentConnectionState)
                {
                    case GoogleBillingConnectionState.Connected:
                        {
                            var productDescriptionQuery = m_ProductsToQuery.Dequeue();
                            m_QuerySkuDetailsService.QueryAsyncSku(productDescriptionQuery.products, productDescriptionQuery.onProductsReceived);
                            break;
                        }
                    case GoogleBillingConnectionState.Disconnected:
                        {
                            var productDescriptionQuery = m_ProductsToQuery.Dequeue();
                            var reason = AreConnectionAttemptsExhausted() ? GoogleRetrieveProductsFailureReason.BillingServiceUnavailable : GoogleRetrieveProductsFailureReason.BillingServiceDisconnected;
                            productDescriptionQuery.onRetrieveProductsFailed(reason);

                            productsFailedToDequeue.Enqueue(productDescriptionQuery);
                            break;
                        }
                    case GoogleBillingConnectionState.Connecting:
                        {
                            stop = true;
                            break;
                        }
                    default:
                        {
                            m_Logger.LogIAPError($"GooglePlayStoreService state ({currentConnectionState}) unrecognized, cannot process ProductDescriptionQuery");
                            stop = true;
                            break;
                        }
                }
            }

            foreach (var product in productsFailedToDequeue)
            {
                m_ProductsToQuery.Enqueue(product);
            }
        }

        protected virtual void DequeueFetchPurchases()
        {
            while (m_OnPurchaseSucceededQueue.TryDequeue(out var onPurchaseSucceed))
            {
                FetchPurchases(onPurchaseSucceed);
            }
        }

        void OnDisconnected()
        {
            DequeueQueryProducts();
            AttemptReconnection();
        }

        void AttemptReconnection()
        {
            if (!AreConnectionAttemptsExhausted())
            {
                StartConnection();
            }
            else
            {
                OnReconnectionFailure();
            }
        }

        bool AreConnectionAttemptsExhausted()
        {
            return m_CurrentConnectionAttempts >= k_MaxConnectionAttempts;
        }

        void OnReconnectionFailure()
        {
            DequeueQueryProducts();
        }

        public virtual void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleRetrieveProductsFailureReason> onRetrieveProductsFailed)
        {
            var currentConnectionState = m_BillingClient.GetConnectionState();
            if (currentConnectionState == GoogleBillingConnectionState.Connected)
            {
                m_QuerySkuDetailsService.QueryAsyncSku(products, onProductsReceived);
            }
            else
            {
                HandleRetrieveProductsNotConnected(products, onProductsReceived, onRetrieveProductsFailed);
            }
        }

        void HandleRetrieveProductsNotConnected(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleRetrieveProductsFailureReason> onRetrieveProductsFailed)
        {
            if (m_BillingClient.GetConnectionState() == GoogleBillingConnectionState.Disconnected)
            {
                var reason = AreConnectionAttemptsExhausted() ? GoogleRetrieveProductsFailureReason.BillingServiceUnavailable : GoogleRetrieveProductsFailureReason.BillingServiceDisconnected;
                onRetrieveProductsFailed(reason);
            }

            m_ProductsToQuery.Enqueue(new ProductDescriptionQuery(products, onProductsReceived, onRetrieveProductsFailed));
        }

        public void Purchase(ProductDefinition product)
        {
            Purchase(product, null, null);
        }

        public virtual void Purchase(ProductDefinition product, Product oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            m_GoogleLastKnownProductService.LastKnownOldProductId = oldProduct?.definition.storeSpecificId;
            m_GoogleLastKnownProductService.LastKnownProductId = product.storeSpecificId;
            m_GoogleLastKnownProductService.LastKnownProrationMode = desiredProrationMode;
            m_GooglePurchaseService.Purchase(product, oldProduct, desiredProrationMode);
        }

        public void FinishTransaction(ProductDefinition product, string purchaseToken, Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished)
        {
            m_GoogleFinishTransactionService.FinishTransaction(product, purchaseToken, onTransactionFinished);
        }

        public async void FetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed)
        {
            try
            {
                await TryFetchPurchases(onQueryPurchaseSucceed);
            }
            catch (Exception ex)
            {
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.FetchPurchasesError, ex);
            }
        }

        async Task TryFetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed)
        {
            if (onQueryPurchaseSucceed == null)
            {
                m_Logger.LogIAPWarning("FetchPurchases called with null callback onQueryPurchaseSucceed");
                return;
            }

            if (m_BillingClient.GetConnectionState() == GoogleBillingConnectionState.Connected)
            {
                var purchases = await m_GoogleQueryPurchasesService.QueryPurchases();
                onQueryPurchaseSucceed(purchases);
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

        public virtual void ConfirmSubscriptionPriceChange(ProductDefinition product, Action<IGoogleBillingResult> onPriceChangeAction)
        {
            m_GooglePriceChangeService.PriceChange(product, onPriceChangeAction);
        }
    }
}
