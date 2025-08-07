#nullable enable

using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    interface IGooglePlayGetGooglePurchaseUseCase
    {
        IGooglePurchase? GetGooglePurchase(string purchaseToken);
    }
}
