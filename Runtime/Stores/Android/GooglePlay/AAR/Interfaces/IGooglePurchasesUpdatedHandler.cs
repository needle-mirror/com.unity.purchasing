using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Utils;

namespace Stores.Android.GooglePlay.AAR.Interfaces
{
    interface IGooglePurchasesUpdatedHandler
    {
        void HandleUpdatedPurchases(IGoogleBillingResult result, List<IGooglePurchase> purchases);
        void SetProductCache(IProductCache productCache);
    }
}
