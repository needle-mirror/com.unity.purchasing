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

        /// <summary>
        /// Apple Specific Purchase Extensions
        /// </summary>
        public virtual IAppleStoreExtendedPurchaseService? Apple => m_BaseInternalPurchaseService.Apple;

        /// <summary>
        /// Google Play Store Specific Purchase Extensions
        /// </summary>
        public virtual IGooglePlayStoreExtendedPurchaseService? Google => m_BaseInternalPurchaseService.Google;

        /// <summary>
        /// Purchases a product.
        /// </summary>
        /// <param name="product">The product to purchase.</param>
        public virtual void PurchaseProduct(Product product)
        {
            m_BaseInternalPurchaseService.PurchaseProduct(product);
        }

        /// <summary>
        /// Purchases a cart.
        /// </summary>
        /// <param name="cart">The cart to purchase.</param>
        public virtual void Purchase(ICart cart)
        {
            m_BaseInternalPurchaseService.Purchase(cart);
        }

        /// <summary>
        /// Confirms a purchase.
        /// </summary>
        /// <param name="order">The pending order to confirm.</param>
        public virtual void ConfirmPurchase(PendingOrder order)
        {
            m_BaseInternalPurchaseService.ConfirmPurchase(order);
        }

        /// <summary>
        /// Fetches purchases.
        /// </summary>
        public virtual void FetchPurchases()
        {
            m_BaseInternalPurchaseService.FetchPurchases();
        }

        /// <summary>
        /// Checks the entitlement for a product.
        /// </summary>
        /// <param name="product">The product to check entitlement for.</param>
        public virtual void CheckEntitlement(Product product)
        {
            m_BaseInternalPurchaseService.CheckEntitlement(product);
        }

        /// <summary>
        /// Restores transactions for the store.
        /// </summary>
        /// <param name="callback"> An optional callback to handle the result of the restore operation.</param>
        public virtual void RestoreTransactions(Action<bool, string?>? callback)
        {
            m_BaseInternalPurchaseService.RestoreTransactions(callback);
        }

        /// <summary>
        /// Gets the products that have been fetched.
        /// </summary>
        /// <returns>Returns a read-only collection of products.</returns>
        public virtual ReadOnlyObservableCollection<Order> GetPurchases()
        {
            return m_BaseInternalPurchaseService.GetPurchases();
        }

        /// <summary>
        /// Processes pending orders on purchases fetched.
        /// </summary>
        /// <param name="shouldProcess">A boolean indicating whether to process pending orders on purchases fetched.</param>
        public void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess)
        {
            m_BaseInternalPurchaseService.ProcessPendingOrdersOnPurchasesFetched(shouldProcess);
        }

        /// <summary>
        /// Event that is triggered when a purchase is pending.
        /// </summary>
        public event Action<PendingOrder>? OnPurchasePending
        {
            add => m_BaseInternalPurchaseService.OnPurchasePending += value;
            remove => m_BaseInternalPurchaseService.OnPurchasePending -= value;
        }

        /// <summary>
        /// Event that is triggered when a purchase is confirmed.
        /// </summary>
        public event Action<Order>? OnPurchaseConfirmed
        {
            add => m_BaseInternalPurchaseService.OnPurchaseConfirmed += value;
            remove => m_BaseInternalPurchaseService.OnPurchaseConfirmed -= value;
        }

        /// <summary>
        /// Event that is triggered when a purchase fails.
        /// </summary>
        public event Action<FailedOrder>? OnPurchaseFailed
        {
            add => m_BaseInternalPurchaseService.OnPurchaseFailed += value;
            remove => m_BaseInternalPurchaseService.OnPurchaseFailed -= value;
        }

        /// <summary>
        /// Event that is triggered when a purchase is deferred.
        /// </summary>
        public event Action<DeferredOrder>? OnPurchaseDeferred
        {
            add => m_BaseInternalPurchaseService.OnPurchaseDeferred += value;
            remove => m_BaseInternalPurchaseService.OnPurchaseDeferred -= value;
        }

        /// <summary>
        /// Event that is triggered when purchases are fetched.
        /// </summary>
        public event Action<Orders>? OnPurchasesFetched
        {
            add => m_BaseInternalPurchaseService.OnPurchasesFetched += value;
            remove => m_BaseInternalPurchaseService.OnPurchasesFetched -= value;
        }

        /// <summary>
        /// Event that is triggered when fetching purchases fails.
        /// </summary>
        public event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed
        {
            add => m_BaseInternalPurchaseService.OnPurchasesFetchFailed += value;
            remove => m_BaseInternalPurchaseService.OnPurchasesFetchFailed -= value;
        }

        /// <summary>
        /// Event that is triggered when a checking a product's entitlement status.
        /// </summary>
        public event Action<Entitlement>? OnCheckEntitlement
        {
            add => m_BaseInternalPurchaseService.OnCheckEntitlement += value;
            remove => m_BaseInternalPurchaseService.OnCheckEntitlement -= value;
        }
    }
}
