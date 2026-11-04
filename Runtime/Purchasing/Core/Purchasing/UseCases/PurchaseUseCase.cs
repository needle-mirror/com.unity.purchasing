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

        // Every store reports its outcomes here, so this is where a journey ends: the
        // outcome's events still carry its impression_id, but a purchase started from
        // a handler of it begins its own journey (see ImpressionScope).
        public void OnPurchaseSucceeded(PendingOrder order)
        {
            using (ImpressionScope.Shared.EnterOutcome())
            {
                OnPurchaseSuccess?.Invoke(order);
            }
        }

        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            using (ImpressionScope.Shared.EnterOutcome())
            {
                OnPurchaseFail?.Invoke(failedOrder);
            }
        }

        public void OnPurchaseDeferred(DeferredOrder deferredOrder)
        {
            using (ImpressionScope.Shared.EnterOutcome())
            {
                OnPurchaseDefer?.Invoke(deferredOrder);
            }
        }
    }
}
