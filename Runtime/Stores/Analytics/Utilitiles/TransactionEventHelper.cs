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

        internal long CheckCurrencyCodeAndExtractRealCurrencyAmount(CatalogListing listing)
        {
            if (listing?.metadata?.isoCurrencyCode != null)
            {
                return ExtractRealCurrencyAmount(listing);
            }
            else
            {
                m_Logger.LogIAPWarning($"The isoCurrencyCode for catalog listing '{listing?.id}' is null. Were you trying to purchase an unavailable product? The price will be recorded as 0.");
                return 0;
            }
        }

        long ExtractRealCurrencyAmount(CatalogListing listing)
        {
            try
            {
                return m_Analytics.AnalyticsServiceInstance()?.ConvertCurrencyToMinorUnits(listing.metadata.isoCurrencyCode, (double)listing.metadata.localizedPrice) ?? 0;
            }
            catch (Exception)
            {
                m_Logger.LogIAPWarning($"Could not convert real currency amount payable for catalog listing '{listing.id}'. The price will be recorded as 0.");
                return 0;
            }
        }
    }
}

#endif
