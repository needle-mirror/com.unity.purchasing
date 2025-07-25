#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An abstract store service to extend an existing Purchase Service which will handle all of the basic IPurchaseService implementations
    /// The main purpose of this is to allow a custom store to add implementations of extended features to this service.
    /// The calls to IPurchaseService are kept virtual so that the derivations of the base store implementing them can be added to or overridden.
    /// </summary>
    public abstract class ExtensiblePurchaseService : IPurchaseService
    {
        IPurchaseService m_BaseInternalPurchaseService;

        /// <summary>
        /// Constructor to be used by derived classes
        /// </summary>
        /// <param name="basePurchaseService"> The base service implementation which implements IProductService </param>
        protected ExtensiblePurchaseService(IPurchaseService basePurchaseService)
        {
            m_BaseInternalPurchaseService = basePurchaseService;
        }

        public virtual IAppleStoreExtendedPurchaseService? Apple => m_BaseInternalPurchaseService.Apple;

        public virtual IGooglePlayStoreExtendedPurchaseService? Google => m_BaseInternalPurchaseService.Google;

        public virtual void PurchaseProduct(Product product)
        {
            m_BaseInternalPurchaseService.PurchaseProduct(product);
        }

        public virtual void Purchase(ICart cart)
        {
            m_BaseInternalPurchaseService.Purchase(cart);
        }

        public virtual void ConfirmPurchase(PendingOrder order)
        {
            m_BaseInternalPurchaseService.ConfirmPurchase(order);
        }

        public virtual void FetchPurchases()
        {
            m_BaseInternalPurchaseService.FetchPurchases();
        }

        public virtual void CheckEntitlement(Product product)
        {
            m_BaseInternalPurchaseService.CheckEntitlement(product);
        }

        public virtual void RestoreTransactions(Action<bool, string?>? callback)
        {
            m_BaseInternalPurchaseService.RestoreTransactions(callback);
        }

        public virtual ReadOnlyObservableCollection<Order> GetPurchases()
        {
            return m_BaseInternalPurchaseService.GetPurchases();
        }

        public void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess)
        {
            m_BaseInternalPurchaseService.ProcessPendingOrdersOnPurchasesFetched(shouldProcess);
        }

        public event Action<PendingOrder>? OnPurchasePending
        {
            add => m_BaseInternalPurchaseService.OnPurchasePending += value;
            remove => m_BaseInternalPurchaseService.OnPurchasePending -= value;
        }

        public event Action<Order>? OnPurchaseConfirmed
        {
            add => m_BaseInternalPurchaseService.OnPurchaseConfirmed += value;
            remove => m_BaseInternalPurchaseService.OnPurchaseConfirmed -= value;
        }

        public event Action<FailedOrder>? OnPurchaseFailed
        {
            add => m_BaseInternalPurchaseService.OnPurchaseFailed += value;
            remove => m_BaseInternalPurchaseService.OnPurchaseFailed -= value;
        }

        public event Action<DeferredOrder>? OnPurchaseDeferred
        {
            add => m_BaseInternalPurchaseService.OnPurchaseDeferred += value;
            remove => m_BaseInternalPurchaseService.OnPurchaseDeferred -= value;
        }

        public event Action<Orders>? OnPurchasesFetched
        {
            add => m_BaseInternalPurchaseService.OnPurchasesFetched += value;
            remove => m_BaseInternalPurchaseService.OnPurchasesFetched -= value;
        }

        public event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed
        {
            add => m_BaseInternalPurchaseService.OnPurchasesFetchFailed += value;
            remove => m_BaseInternalPurchaseService.OnPurchasesFetchFailed -= value;
        }

        public event Action<Entitlement>? OnCheckEntitlement
        {
            add => m_BaseInternalPurchaseService.OnCheckEntitlement += value;
            remove => m_BaseInternalPurchaseService.OnCheckEntitlement -= value;
        }
    }
}
