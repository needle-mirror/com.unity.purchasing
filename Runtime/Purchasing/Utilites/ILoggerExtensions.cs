namespace UnityEngine.Purchasing
{
    static class LoggerExtensions
    {
        const string IAPLogTag = "Unity IAP";

        public static void LogIAPWarning(this ILogger logger, string message)
        {
            logger.LogWarning(IAPLogTag, message);
        }

        public static void LogIAPError(this ILogger logger, string message)
        {
            logger.LogError(IAPLogTag, message);
        }
    }
}
