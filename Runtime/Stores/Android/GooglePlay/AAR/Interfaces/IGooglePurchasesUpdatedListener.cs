using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchasesUpdatedListener
    {
        event Action<IGoogleBillingResult, List<IGooglePurchase>> OnPurchaseUpdated;
    }
}
