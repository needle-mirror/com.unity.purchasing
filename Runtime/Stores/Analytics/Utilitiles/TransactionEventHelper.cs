#if IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT || IAP_ANALYTICS_SERVICE_ENABLED

using System;
using Unity.Services.Analytics;

namespace UnityEngine.Purchasing
{
    class TransactionEventHelper
    {
        readonly IAnalyticsServiceWrapper m_Analytics;
        readonly ILogger m_Logger;

        internal TransactionEventHelper(IAnalyticsServiceWrapper analytics, ILogger logger)
        {
            m_Analytics = analytics;
            m_Logger = logger;
        }

        internal long CheckCurrencyCodeAndExtractRealCurrencyAmount(Product product)
        {
            if (product.metadata.isoCurrencyCode != null)
            {
                return ExtractRealCurrencyAmount(product);
            }
            else
            {
                m_Logger.LogIAPWarning($"The isoCurrencyCode for product ID {product.definition.id} is null. Were you trying to purchase an unavailable product? The price will be recorded as 0.");
                return 0;
            }
        }

        long ExtractRealCurrencyAmount(Product product)
        {
            try
            {
                return m_Analytics.AnalyticsServiceInstance()?.ConvertCurrencyToMinorUnits(product.metadata.isoCurrencyCode, (double)product.metadata.localizedPrice) ?? 0;
            }
            catch (Exception)
            {
                m_Logger.LogIAPWarning($"Could not convert real currency amount payable for product ID {product.definition.id}. The price will be recorded as 0.");
                return 0;
            }
        }
    }
}

#endif
