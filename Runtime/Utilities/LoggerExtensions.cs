using System;

namespace UnityEngine.Purchasing
{
    static class LoggerExtensions
    {
        const string k_IAPLogTag = "InAppPurchasing";

        public static void LogIAP(this ILogger logger, string message)
        {
            logger.Log(k_IAPLogTag, message);
        }

        public static void LogIAPError(this ILogger logger, string message)
        {
            logger.LogError(k_IAPLogTag, message);
        }

        public static void LogIAPException(this ILogger logger, Exception exception)
        {
            logger.LogFormat(LogType.Exception, $"{k_IAPLogTag}: {exception}");
        }

        public static void LogIAPWarning(this ILogger logger, string message)
        {
            logger.LogWarning(k_IAPLogTag, message);
        }
    }
}
