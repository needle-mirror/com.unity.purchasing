using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreService : IGooglePlayStoreService
    {
        const int k_MaxConnectionAttempts = 1;

        GoogleBillingConnectionState m_GoogleConnectionState = GoogleBillingConnectionState.Disconnected;
        int m_CurrentConnectionAttempts = 0;

        IGoogleBillingClient m_BillingClient;
        IBillingClientStateListener m_BillingClientStateListener;
        IQuerySkuDetailsService m_QuerySkuDetailsService;
        Queue<ProductDescriptionQuery> m_ProductsToQuery = new Queue<ProductDescriptionQuery>();
        Queue<Action<List<GooglePurchase>>> m_OnPurchaseSucceededQueue = new Queue<Action<List<GooglePurchase>>>();
        IGooglePurchaseService m_GooglePurchaseService;
        IGoogleFinishTransactionService m_GoogleFinishTransactionService;
        IGoogleQueryPurchasesService m_GoogleQueryPurchasesService;
        IGooglePriceChangeService m_GooglePriceChangeService;
        IGoogleLastKnownProductService m_GoogleLastKnownProductService;
        ITelemetryMetrics m_TelemetryMetrics;

        internal GooglePlayStoreService(
            IGoogleBillingClient billingClient,
            IQuerySkuDetailsService querySkuDetailsService,
            IGooglePurchaseService purchaseService,
            IGoogleFinishTransactionService finishTransactionService,
            IGoogleQueryPurchasesService queryPurchasesService,
            IBillingClientStateListener billingClientStateListener,
            IGooglePriceChangeService priceChangeService,
            IGoogleLastKnownProductService lastKnownProductService,
            ITelemetryMetrics telemetryMetrics)
        {
            m_BillingClient = billingClient;
            m_QuerySkuDetailsService = querySkuDetailsService;
            m_GooglePurchaseService = purchaseService;
            m_GoogleFinishTransactionService = finishTransactionService;
            m_GoogleQueryPurchasesService = queryPurchasesService;
            m_GooglePriceChangeService = priceChangeService;
            m_GoogleLastKnownProductService = lastKnownProductService;
            m_BillingClientStateListener = billingClientStateListener;
            m_TelemetryMetrics = telemetryMetrics;

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
            m_GoogleConnectionState = GoogleBillingConnectionState.Connecting;
            m_CurrentConnectionAttempts++;
            m_BillingClient.StartConnection(m_BillingClientStateListener);
        }

        public void ResumeConnection()
        {
            if (m_GoogleConnectionState == GoogleBillingConnectionState.Disconnected)
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
            m_GoogleConnectionState = GoogleBillingConnectionState.Connected;
            m_CurrentConnectionAttempts = 0;

            DequeueQueryProducts();
            DequeueFetchPurchases();
        }

        void DequeueQueryProducts()
        {
            var dequeueQueryProductsMetric = m_TelemetryMetrics.CreateAndStartMetricEvent(TelemetryMetricTypes.Histogram, TelemetryMetricNames.dequeueQueryProductsTimeName);
            var productsFailedToDequeue = new Queue<ProductDescriptionQuery>();
            var stop = false;

            while (m_ProductsToQuery.Count > 0 && !stop)
            {
                switch (m_GoogleConnectionState)
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
                        Debug.LogErrorFormat("GooglePlayStoreService state ({0}) unrecognized, cannot process ProductDescriptionQuery",
                            m_GoogleConnectionState);
                        stop = true;
                        break;
                    }
                }
            }

            foreach (var product in productsFailedToDequeue)
            {
                m_ProductsToQuery.Enqueue(product);
            }
            dequeueQueryProductsMetric.StopAndSendMetric();
        }

        void DequeueFetchPurchases()
        {
            var dequeueQueryPurchasesMetric = m_TelemetryMetrics.CreateAndStartMetricEvent(TelemetryMetricTypes.Histogram, TelemetryMetricNames.dequeueQueryPurchasesTimeName);
            while (m_OnPurchaseSucceededQueue.Count > 0)
            {
                var onPurchaseSucceed = m_OnPurchaseSucceededQueue.Dequeue();
                FetchPurchases(onPurchaseSucceed);
            }
            dequeueQueryPurchasesMetric.StopAndSendMetric();
        }

        void OnDisconnected()
        {
            m_GoogleConnectionState = GoogleBillingConnectionState.Disconnected;
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
            m_GoogleConnectionState = GoogleBillingConnectionState.Disconnected;
            DequeueQueryProducts();
        }

        public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleRetrieveProductsFailureReason> onRetrieveProductsFailed)
        {
            var retrieveProductsMetric = m_TelemetryMetrics.CreateAndStartMetricEvent(TelemetryMetricTypes.Histogram, TelemetryMetricNames.retrieveProductsName);
            if (m_GoogleConnectionState == GoogleBillingConnectionState.Connected)
            {
                m_QuerySkuDetailsService.QueryAsyncSku(products, onProductsReceived);
            }
            else
            {
                HandleRetrieveProductsNotConnected(products, onProductsReceived, onRetrieveProductsFailed);
            }
            retrieveProductsMetric.StopAndSendMetric();
        }

        void HandleRetrieveProductsNotConnected(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleRetrieveProductsFailureReason> onRetrieveProductsFailed)
        {
            if (m_GoogleConnectionState == GoogleBillingConnectionState.Disconnected)
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

        public void Purchase(ProductDefinition product, Product oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            var initPurchaseMetric = m_TelemetryMetrics.CreateAndStartMetricEvent(TelemetryMetricTypes.Histogram, TelemetryMetricNames.initPurchaseName);
            m_GoogleLastKnownProductService.SetLastKnownProductId(product.storeSpecificId);
            m_GoogleLastKnownProductService.SetLastKnownProrationMode(desiredProrationMode);
            m_GooglePurchaseService.Purchase(product, oldProduct, desiredProrationMode);
            initPurchaseMetric.StopAndSendMetric();
        }

        public void FinishTransaction(ProductDefinition product, string purchaseToken, Action<ProductDefinition, GooglePurchase, IGoogleBillingResult, string> onConsume, Action<ProductDefinition, GooglePurchase, IGoogleBillingResult> onAcknowledge)
        {
            m_GoogleFinishTransactionService.FinishTransaction(product, purchaseToken, onConsume, onAcknowledge);
        }

        public void FetchPurchases(Action<List<GooglePurchase>> onQueryPurchaseSucceed)
        {
            if (m_GoogleConnectionState == GoogleBillingConnectionState.Connected)
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

        public void ConfirmSubscriptionPriceChange(ProductDefinition product, Action<IGoogleBillingResult> onPriceChangeAction)
        {
            var confirmSubscriptionPriceChangeMetric = m_TelemetryMetrics.CreateAndStartMetricEvent(TelemetryMetricTypes.Histogram, TelemetryMetricNames.confirmSubscriptionPriceChangeName);
            m_GooglePriceChangeService.PriceChange(product, onPriceChangeAction);
            confirmSubscriptionPriceChangeMetric.StopAndSendMetric();
        }
    }
}
