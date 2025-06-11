using System.Collections.Generic;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchaseBuilder
    {
        IEnumerable<IGooglePurchase> BuildPurchases(IEnumerable<AndroidJavaObject> purchases);
    }
}
