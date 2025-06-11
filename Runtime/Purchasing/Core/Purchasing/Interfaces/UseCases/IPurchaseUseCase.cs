using System;

namespace UnityEngine.Purchasing
{
    // Interface for the use case of making a purchase of a cart
    interface IPurchaseUseCase
    {
        /// <summary>
        /// Purchase a cart, usually asynchronously. Success or failure is signalled via the actions passed.
        /// </summary>
        /// <param name="cart">The cart to be purchased.</param>
        void Purchase(ICart cart);

        event Action<PendingOrder> OnPurchaseSuccess;
        event Action<FailedOrder> OnPurchaseFail;
        event Action<DeferredOrder> OnPurchaseDefer;
    }
}
