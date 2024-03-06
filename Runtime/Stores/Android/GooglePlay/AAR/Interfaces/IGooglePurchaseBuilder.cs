using System.Collections.Generic;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchaseBuilder
    {
        IEnumerable<IGooglePurchase> BuildPurchases(IEnumerable<AndroidJavaObject> purchases);
        IGooglePurchase BuildPurchase(AndroidJavaObject purchase);
    }
}
