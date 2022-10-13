using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The core abstract implementation for Unity Purchasing.
    /// </summary>
    public abstract class UnityPurchasing
    {
        /// <summary>
        /// The main initialization call for Unity Purchasing.
        /// </summary>
        /// <param name="listener"> The <c>IStoreListener</c> to receive callbacks for future transactions </param>
        /// <param name="builder"> The <c>ConfigurationBuilder</c> containing the product definitions mapped to stores </param>
        public static void Initialize(IStoreListener listener, ConfigurationBuilder builder)
        {
            var logger = Debug.unityLogger;
            var unityServicesInitializationChecker = new UnityServicesInitializationChecker(logger);
            var legacyAnalyticsWrapper = new LegacyAnalyticsWrapper(GenerateLegacyUnityAnalytics(), new EmptyAnalyticsAdapter());

            Initialize(listener, builder, logger, Application.persistentDataPath,
                GenerateUnityAnalytics(logger), legacyAnalyticsWrapper, builder.factory.GetCatalogProvider(),
                unityServicesInitializationChecker);
        }

        private static IAnalyticsAdapter GenerateUnityAnalytics(ILogger logger)
        {
#if DISABLE_RUNTIME_IAP_ANALYTICS
            return new EmptyAnalyticsAdapter();
#else
            return new AnalyticsAdapter(AnalyticsService.Instance, logger);
#endif
        }

        static IAnalyticsAdapter GenerateLegacyUnityAnalytics()
        {
#if DISABLE_RUNTIME_IAP_ANALYTICS || !ENABLE_CLOUD_SERVICES_ANALYTICS
            return new EmptyAnalyticsAdapter();
#else
            return new LegacyAnalyticsAdapter(new LegacyUnityAnalytics());
#endif
        }

        /// <summary>
        /// This is useful in certain test scenarios, such as repeatedly testing
        /// an App's behaviour when purchases are restored.
        ///
        /// This is a static method since developers may wish to clear the log before
        /// initialising IAP.
        /// </summary>
        public static void ClearTransactionLog()
        {
            var log = new TransactionLog(Debug.unityLogger, Application.persistentDataPath);
            log.Clear();
        }

        /// <summary>
        /// Created for integration testing.
        /// </summary>
        internal static void Initialize(IStoreListener listener, ConfigurationBuilder builder,
            ILogger logger, string persistentDatapath, IAnalyticsAdapter ugsAnalytics, IAnalyticsAdapter legacyAnalytics,
            ICatalogProvider catalog, IUnityServicesInitializationChecker unityServicesInitializationChecker)
        {
            unityServicesInitializationChecker.CheckAndLogWarning();

            var transactionLog = new TransactionLog(logger, persistentDatapath);
            var manager = new PurchasingManager(transactionLog, logger, builder.factory.service,
                builder.factory.storeName, unityServicesInitializationChecker);

            var analyticsClient = new AnalyticsClient(ugsAnalytics, legacyAnalytics);

            // Proxy the PurchasingManager's callback interface to forward Transactions to Analytics.
            var proxy = new StoreListenerProxy(listener, analyticsClient, builder.factory);
            FetchAndMergeProducts(builder.useCatalogProvider, builder.products, catalog, response =>
                {
                    manager.Initialize(proxy, response);
                });
        }

        internal static void FetchAndMergeProducts(bool useCatalog,
            HashSet<ProductDefinition> localProductSet, ICatalogProvider catalog, Action<HashSet<ProductDefinition>> callback)
        {
            if (useCatalog && catalog != null)
            {
                catalog.FetchProducts(cloudProducts =>
                {
                    var updatedProductSet = new HashSet<ProductDefinition>(localProductSet);

                    foreach (var product in cloudProducts)
                    {
                        // Products are hashed by id, so this should remove the local product with the same id before adding the cloud product
                        updatedProductSet.Remove(product);
                        updatedProductSet.Add(product);
                    }

                    callback(updatedProductSet);
                });
            }
            else
            {
                callback(localProductSet);
            }
        }
    }
}
