using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace UnityEngine.Purchasing
{
    static class LoggerExtensions
    {
        const string k_IAPLogTag = "InAppPurchasing";

        const string k_VerboseLoggingDefine = "ENABLE_UNITY_IN_APP_PURCHASING_VERBOSE_LOGGING";


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

#if !ENABLE_UNITY_SERVICES_VERBOSE_LOGGING
        [Conditional(k_VerboseLoggingDefine)]
#endif
        public static void LogIAPVerbose(this ILogger logger, string message)
        {
            logger.Log(k_IAPLogTag, message);
        }

#if !ENABLE_UNITY_SERVICES_VERBOSE_LOGGING
        [Conditional(k_VerboseLoggingDefine)]
#endif
        public static void LogIAPCallVerbose(this ILogger logger, string message, string entityName, [CallerMemberName] string callerName = "")
        {
            logger.Log(k_IAPLogTag, $"[{entityName}.{callerName}] {message}");
        }
    }
}
