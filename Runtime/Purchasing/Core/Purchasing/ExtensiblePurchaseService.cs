using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Apple Specific Purchase Extensions
        /// </summary>
        public virtual IAppleStoreExtendedPurchaseService Apple => m_BaseInternalPurchaseService.Apple;

        /// <summary>
        /// Google Specific Purchase Extensions
        /// </summary>
        public virtual IGooglePlayStoreExtendedPurchaseService Google => m_BaseInternalPurchaseService.Google;

        public virtual void PurchaseProduct(Product product)
        {
            m_BaseInternalPurchaseService.PurchaseProduct(product);
        }

        public virtual void Purchase(ICart cart)
        {
            m_BaseInternalPurchaseService.Purchase(cart);
        }

        public virtual void ConfirmOrder(PendingOrder order)
        {
            m_BaseInternalPurchaseService.ConfirmOrder(order);
        }

        public virtual void FetchPurchases()
        {
            m_BaseInternalPurchaseService.FetchPurchases();
        }

        public virtual void IsProductEntitled(Product product)
        {
            m_BaseInternalPurchaseService.IsProductEntitled(product);
        }

        public virtual void RestoreTransactions(Action<bool, string> callback)
        {
            m_BaseInternalPurchaseService.RestoreTransactions(callback);
        }

        public virtual ReadOnlyObservableCollection<Order> GetPurchases()
        {
            return m_BaseInternalPurchaseService.GetPurchases();
        }

        public virtual void AddPendingOrderUpdatedAction(Action<PendingOrder> updatedAction)
        {
            m_BaseInternalPurchaseService.AddPendingOrderUpdatedAction(updatedAction);
        }

        public virtual void AddConfirmedOrderUpdatedAction(Action<ConfirmedOrder> updatedAction)
        {
            m_BaseInternalPurchaseService.AddConfirmedOrderUpdatedAction(updatedAction);
        }

        public virtual void AddPurchaseFailedAction(Action<FailedOrder> failedAction)
        {
            m_BaseInternalPurchaseService.AddPurchaseFailedAction(failedAction);
        }

        public virtual void AddPurchaseDeferredAction(Action<DeferredOrder> deferredAction)
        {
            m_BaseInternalPurchaseService.AddPurchaseDeferredAction(deferredAction);
        }

        public virtual void AddFetchedPurchasesAction(Action<Orders> updatedAction)
        {
            m_BaseInternalPurchaseService.AddFetchedPurchasesAction(updatedAction);
        }

        public virtual void AddFetchPurchasesFailedAction(Action<PurchasesFetchFailureDescription> failedAction)
        {
            m_BaseInternalPurchaseService.AddFetchPurchasesFailedAction(failedAction);
        }

        public virtual void AddCheckEntitlementAction(Action<Entitlement> checkEntitlementAction)
        {
            m_BaseInternalPurchaseService.AddCheckEntitlementAction(checkEntitlementAction);
        }

        public virtual void RemovePendingOrderUpdatedAction(Action<PendingOrder> updatedAction)
        {
            m_BaseInternalPurchaseService.RemovePendingOrderUpdatedAction(updatedAction);
        }

        public virtual void RemoveConfirmedOrderUpdatedAction(Action<ConfirmedOrder> updatedAction)
        {
            m_BaseInternalPurchaseService.RemoveConfirmedOrderUpdatedAction(updatedAction);
        }

        public virtual void RemovePurchaseFailedAction(Action<FailedOrder> failedAction)
        {
            m_BaseInternalPurchaseService.RemovePurchaseFailedAction(failedAction);
        }

        public virtual void RemoveFetchedPurchasesAction(Action<Orders> updatedAction)
        {
            m_BaseInternalPurchaseService.RemoveFetchedPurchasesAction(updatedAction);
        }

        public virtual void RemoveFetchPurchasesFailedAction(Action<PurchasesFetchFailureDescription> failedAction)
        {
            m_BaseInternalPurchaseService.RemoveFetchPurchasesFailedAction(failedAction);
        }

        public virtual void RemoveCheckEntitlementAction(Action<Entitlement> checkEntitlementAction)
        {
            m_BaseInternalPurchaseService.RemoveCheckEntitlementAction(checkEntitlementAction);
        }
    }
}
