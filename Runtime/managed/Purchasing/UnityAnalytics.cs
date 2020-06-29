using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Forward transaction information to Unity Analytics.
    /// </summary>
    internal class UnityAnalytics : IUnityAnalytics
    {
        public void Transaction(string productId, decimal price, string currency, string receipt, string signature)
        {
            Analytics.Analytics.Transaction(productId, price, currency, receipt, signature, true);
        }

        public void CustomEvent(string name, Dictionary<string, object> data)
        {
            Analytics.Analytics.CustomEvent(name, data);
        }
    }
}
