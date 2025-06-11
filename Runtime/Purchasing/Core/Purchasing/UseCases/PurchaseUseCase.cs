using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class PurchaseUseCase : IPurchaseUseCase, IStorePurchaseCallback
    {
        protected IStore m_Store { get; }

        /// <summary>
        /// Create the use case object for a store.
        /// </summary>
        /// <param name="storeResponsible">The store responsible for the purchases to be retrieved</param>
        internal PurchaseUseCase(IStore storeResponsible)
        {
            m_Store = storeResponsible;
            m_Store.SetPurchaseCallback(this);
        }

        public void Purchase(ICart cart)
        {
            m_Store.Purchase(cart);
        }

        public event Action<PendingOrder> OnPurchaseSuccess;
        public event Action<FailedOrder> OnPurchaseFail;
        public event Action<DeferredOrder> OnPurchaseDefer;

        public void OnPurchaseSucceeded(PendingOrder order)
        {
            OnPurchaseSuccess?.Invoke(order);
        }

        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            OnPurchaseFail?.Invoke(failedOrder);
        }

        public void OnPurchaseDeferred(DeferredOrder deferredOrder)
        {
            OnPurchaseDefer?.Invoke(deferredOrder);
        }
    }
}
