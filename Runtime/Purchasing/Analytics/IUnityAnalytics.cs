using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Extracted from Unity Analytics for testability.
    /// </summary>
    interface IUnityAnalytics
    {
        void SendTransactionEvent(Product product);
        void SendCustomEvent(string name, Dictionary<string, object> data);
    }
}
