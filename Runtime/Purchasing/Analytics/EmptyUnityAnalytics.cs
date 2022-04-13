using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Forward transaction information to Unity Analytics.
    /// </summary>
    class EmptyUnityAnalytics : IUnityAnalytics
    {
        public void SendTransactionEvent(Product product)
        {
        }

        public void SendCustomEvent(string name, Dictionary<string, object> data)
        {
        }
    }
}
