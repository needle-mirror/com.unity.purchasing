#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Controller over several stores at once, for example Google Play and a Payment Provider.
    /// Every event reports the store it came from as its first parameter.
    /// </summary>
    /// <remarks>
    /// Calls that start work in the background (connecting, fetching purchases, restoring, settings) take an optional
    /// <c>storeName</c>: null applies them to every store, a name to that store only.
    /// Fetching products requires <c>storeName</c>, because each store needs its own product definitions.
    /// Getters require <c>storeName</c>, so the caller always says which store answers.
    /// Calls that take a product, a cart or an order go to the store it belongs to
    /// (<see cref="Product.StoreName"/>, <see cref="Order.StoreName"/>).
    /// After construction nothing throws. Every failure the controller detects itself is logged as an error. A store
    /// that isn't on this controller, a cart mixing stores, or an order whose cart has products from another store is
    /// also reported through the call's usual failure callback, or as an empty result for getters. A missing store
    /// name is only logged, since there is no store to report it under.
    /// </remarks>
    public class PurchasingController
    {
        readonly List<(string Name, StoreController Controller)> m_Stores;

        readonly StoreEvent<Action, Action<string>> m_OnStoreConnected;
        readonly StoreEvent<Action<StoreConnectionFailureDescription>, Action<string, StoreConnectionFailureDescription>> m_OnStoreDisconnected;
        readonly StoreEvent<Action, Action<string>> m_OnAuthAccountChanged;
        readonly StoreEvent<Action<List<Product>>, Action<string, List<Product>>> m_OnProductsFetched;
        readonly StoreEvent<Action<ProductFetchFailed>, Action<string, ProductFetchFailed>> m_OnProductsFetchFailed;
        readonly StoreEvent<Action<PendingOrder>, Action<string, PendingOrder>> m_OnPurchasePending;
        readonly StoreEvent<Action<Order>, Action<string, Order>> m_OnPurchaseConfirmed;
        readonly StoreEvent<Action<FailedOrder>, Action<string, FailedOrder>> m_OnPurchaseFailed;
        readonly StoreEvent<Action<DeferredOrder>, Action<string, DeferredOrder>> m_OnPurchaseDeferred;
        readonly StoreEvent<Action<Orders>, Action<string, Orders>> m_OnPurchasesFetched;
        readonly StoreEvent<Action<PurchasesFetchFailureDescription>, Action<string, PurchasesFetchFailureDescription>> m_OnPurchasesFetchFailed;
        readonly StoreEvent<Action<Entitlement>, Action<string, Entitlement>> m_OnCheckEntitlement;

        /// <summary>
        /// Creates a controller over the given stores.
        /// </summary>
        /// <param name="storeNames">The names of the stores to control, e.g. <c>GooglePlay.Name</c>, <c>PaymentProvider.Name</c>.</param>
        /// <exception cref="ArgumentException">No store names, a null or empty name, or the same name twice.</exception>
        /// <exception cref="StoreException">A name that isn't a registered store.</exception>
        public PurchasingController(params string[] storeNames)
            : this(Validated(storeNames).Select(name => (name, new StoreController(name))).ToList())
        {
        }

        internal PurchasingController(List<(string Name, StoreController Controller)> stores)
        {
            Validated(stores.Select(s => s.Name).ToArray());

            m_Stores = stores;
            StoreNames = stores.Select(s => s.Name).ToList().AsReadOnly();

            // Each event is wired by how to subscribe a handler to one store's StoreController event, and how to
            // unsubscribe it; StoreEvent calls these for every store while the developer has handlers.
            m_OnStoreConnected = Event(m_Stores,
                (controller, handler) => controller.OnStoreConnected += handler,
                (controller, handler) => controller.OnStoreConnected -= handler);
            m_OnStoreDisconnected = Event<StoreConnectionFailureDescription>(m_Stores,
                (controller, handler) => controller.OnStoreDisconnected += handler,
                (controller, handler) => controller.OnStoreDisconnected -= handler);
            m_OnAuthAccountChanged = Event(m_Stores,
                (controller, handler) => controller.OnAuthAccountChanged += handler,
                (controller, handler) => controller.OnAuthAccountChanged -= handler);
            m_OnProductsFetched = Event<List<Product>>(m_Stores,
                (controller, handler) => controller.OnProductsFetched += handler,
                (controller, handler) => controller.OnProductsFetched -= handler);
            m_OnProductsFetchFailed = Event<ProductFetchFailed>(m_Stores,
                (controller, handler) => controller.OnProductsFetchFailed += handler,
                (controller, handler) => controller.OnProductsFetchFailed -= handler);
            m_OnPurchasePending = Event<PendingOrder>(m_Stores,
                (controller, handler) => controller.OnPurchasePending += handler,
                (controller, handler) => controller.OnPurchasePending -= handler);
            m_OnPurchaseConfirmed = Event<Order>(m_Stores,
                (controller, handler) => controller.OnPurchaseConfirmed += handler,
                (controller, handler) => controller.OnPurchaseConfirmed -= handler);
            m_OnPurchaseFailed = Event<FailedOrder>(m_Stores,
                (controller, handler) => controller.OnPurchaseFailed += handler,
                (controller, handler) => controller.OnPurchaseFailed -= handler);
            m_OnPurchaseDeferred = Event<DeferredOrder>(m_Stores,
                (controller, handler) => controller.OnPurchaseDeferred += handler,
                (controller, handler) => controller.OnPurchaseDeferred -= handler);
            m_OnPurchasesFetched = Event<Orders>(m_Stores,
                (controller, handler) => controller.OnPurchasesFetched += handler,
                (controller, handler) => controller.OnPurchasesFetched -= handler);
            m_OnPurchasesFetchFailed = Event<PurchasesFetchFailureDescription>(m_Stores,
                (controller, handler) => controller.OnPurchasesFetchFailed += handler,
                (controller, handler) => controller.OnPurchasesFetchFailed -= handler);
            m_OnCheckEntitlement = Event<Entitlement>(m_Stores,
                (controller, handler) => controller.OnCheckEntitlement += handler,
                (controller, handler) => controller.OnCheckEntitlement -= handler);
        }

        // Checked before any store is created, so a bad list leaves nothing half set up.
        static string[] Validated(string[]? storeNames)
        {
            if (storeNames == null || storeNames.Length == 0)
            {
                throw new ArgumentException("At least one store name is required.", nameof(storeNames));
            }
            if (storeNames.Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("Store names can't be null or empty.", nameof(storeNames));
            }
            var duplicate = storeNames.GroupBy(name => name).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new ArgumentException($"Store '{duplicate.Key}' is listed more than once.", nameof(storeNames));
            }
            return storeNames;
        }

        /// <summary>
        /// The names of the stores on this controller, in constructor order.
        /// </summary>
        public IReadOnlyList<string> StoreNames { get; }

#region Events
        /// <summary>Raised when a store connects. Receives the store name.</summary>
        public event Action<string>? OnStoreConnected { add => m_OnStoreConnected.Add(value); remove => m_OnStoreConnected.Remove(value); }

        /// <summary>Raised when a store disconnects or fails to connect.</summary>
        public event Action<string, StoreConnectionFailureDescription>? OnStoreDisconnected { add => m_OnStoreDisconnected.Add(value); remove => m_OnStoreDisconnected.Remove(value); }

        /// <summary>
        /// Raised once per store when Unity Authentication reports a different player. Each store has cleared its
        /// product and purchase caches before raising it, so refetch that store:
        /// <c>FetchProducts(defs, storeName)</c> and <c>FetchPurchases(storeName)</c>. With a remote catalog, fetch it
        /// again first.
        /// </summary>
        public event Action<string>? OnAuthAccountChanged { add => m_OnAuthAccountChanged.Add(value); remove => m_OnAuthAccountChanged.Remove(value); }

        /// <summary>Raised when a store has fetched products.</summary>
        public event Action<string, List<Product>>? OnProductsFetched { add => m_OnProductsFetched.Add(value); remove => m_OnProductsFetched.Remove(value); }

        /// <summary>Raised when a store fails to fetch products.</summary>
        public event Action<string, ProductFetchFailed>? OnProductsFetchFailed { add => m_OnProductsFetchFailed.Add(value); remove => m_OnProductsFetchFailed.Remove(value); }

        /// <summary>Raised when a purchase is pending confirmation.</summary>
        public event Action<string, PendingOrder>? OnPurchasePending { add => m_OnPurchasePending.Add(value); remove => m_OnPurchasePending.Remove(value); }

        /// <summary>Raised when a purchase has been confirmed, or with a <see cref="FailedOrder"/> when confirming failed.</summary>
        public event Action<string, Order>? OnPurchaseConfirmed { add => m_OnPurchaseConfirmed.Add(value); remove => m_OnPurchaseConfirmed.Remove(value); }

        /// <summary>Raised when a purchase fails.</summary>
        public event Action<string, FailedOrder>? OnPurchaseFailed { add => m_OnPurchaseFailed.Add(value); remove => m_OnPurchaseFailed.Remove(value); }

        /// <summary>Raised when a purchase is deferred.</summary>
        public event Action<string, DeferredOrder>? OnPurchaseDeferred { add => m_OnPurchaseDeferred.Add(value); remove => m_OnPurchaseDeferred.Remove(value); }

        /// <summary>Raised when a store has fetched purchases.</summary>
        public event Action<string, Orders>? OnPurchasesFetched { add => m_OnPurchasesFetched.Add(value); remove => m_OnPurchasesFetched.Remove(value); }

        /// <summary>Raised when a store fails to fetch purchases.</summary>
        public event Action<string, PurchasesFetchFailureDescription>? OnPurchasesFetchFailed { add => m_OnPurchasesFetchFailed.Add(value); remove => m_OnPurchasesFetchFailed.Remove(value); }

        /// <summary>Raised with the result of <see cref="CheckEntitlement"/>.</summary>
        public event Action<string, Entitlement>? OnCheckEntitlement { add => m_OnCheckEntitlement.Add(value); remove => m_OnCheckEntitlement.Remove(value); }
#endregion

#region Store
        /// <summary>
        /// Connects the stores. Completes when every targeted store has either connected or failed; failures are
        /// reported through <see cref="OnStoreDisconnected"/>.
        /// </summary>
        /// <param name="storeName">The store to connect, or null for every store.</param>
        public Task Connect(string? storeName = null)
        {
            var targets = Targets(storeName, name =>
                m_OnStoreDisconnected.Handlers?.Invoke(name, new StoreConnectionFailureDescription(NotOnController(name))));
            return Task.WhenAll(targets.Select(c => c.Connect()));
        }

        /// <summary>
        /// Gets a store's connection state. Use <see cref="OnStoreConnected"/> and <see cref="OnStoreDisconnected"/>
        /// to track connection.
        /// </summary>
        /// <param name="storeName">The store to query.</param>
        /// <returns>The store's state, or <see cref="ConnectionState.Unavailable"/> if it isn't on this controller.</returns>
        public ConnectionState GetConnectionState(string storeName)
        {
            return TryGet(storeName)?.GetConnectionState() ?? ConnectionState.Unavailable;
        }

        /// <summary>
        /// Sets the reconnection retry policy. With no store name, the same instance is used by every store; each
        /// store still retries independently, but a policy that keeps its own state shares it across stores.
        /// </summary>
        /// <param name="retryPolicy">The retry policy, or null to disable reconnection.</param>
        /// <param name="storeName">The store to configure, or null for every store.</param>
        public void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy, string? storeName = null)
        {
            foreach (var controller in Targets(storeName, _ => { }))
            {
                controller.SetStoreReconnectionRetryPolicyOnDisconnection(retryPolicy);
            }
        }
#endregion

#region Products
        /// <summary>
        /// Fetches products from one store. Results are reported through <see cref="OnProductsFetched"/> and
        /// <see cref="OnProductsFetchFailed"/>. Fetch each store with its own definitions, since store-specific ids
        /// can differ per store, e.g. from a catalog provider:
        /// <c>catalogProvider.FetchProducts(defs => controller.FetchProducts(defs, store), store)</c> for each store.
        /// </summary>
        /// <remarks>
        /// The store gets its own copy of the definitions, so a catalog provider that reuses its list for the next
        /// store can't change the store-specific ids of this store's products.
        /// </remarks>
        /// <param name="productDefinitions">The products to fetch.</param>
        /// <param name="storeName">The store to fetch from.</param>
        /// <param name="retryPolicy">The retry policy, or null for the default.</param>
        public void FetchProducts(List<ProductDefinition> productDefinitions, string storeName, IRetryPolicy? retryPolicy = null)
        {
            var controller = TryGetForFetch(nameof(FetchProducts), storeName, productDefinitions);
            controller?.FetchProducts(CopyOf(productDefinitions), retryPolicy);
        }

        /// <summary>
        /// Fetches products from one store without retrying on failure. See
        /// <see cref="FetchProducts(List{ProductDefinition}, string, IRetryPolicy)"/>.
        /// </summary>
        /// <param name="productDefinitions">The products to fetch.</param>
        /// <param name="storeName">The store to fetch from.</param>
        public void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions, string storeName)
        {
            var controller = TryGetForFetch(nameof(FetchProductsWithNoRetries), storeName, productDefinitions);
            controller?.FetchProductsWithNoRetries(CopyOf(productDefinitions));
        }

        StoreController? TryGetForFetch(string call, string storeName, List<ProductDefinition> productDefinitions)
        {
            if (productDefinitions == null)
            {
                LogError(NullArgument(call, "product definition list"));
                return null;
            }
            if (productDefinitions.Any(definition => definition == null))
            {
                LogError($"PurchasingController.{call} was called with null product definitions; they were skipped.");
            }
            var controller = TryGet(storeName);
            if (controller == null && storeName != null)
            {
                m_OnProductsFetchFailed.Handlers?.Invoke(storeName, new ProductFetchFailed(productDefinitions, NotOnController(storeName)));
            }
            return controller;
        }

        // A fetched product keeps the definition it was fetched with, and catalog providers rewrite the
        // store-specific ids of one shared list for each store they serve. Each store gets its own copies.
        // Null entries are skipped (TryGetForFetch logs them).
        static List<ProductDefinition> CopyOf(List<ProductDefinition> productDefinitions)
        {
            return productDefinitions.Where(definition => definition != null)
                .Select(definition => new ProductDefinition(definition.id, definition.storeSpecificId, definition.type,
                    definition.catalogListingId, definition.enabled, definition.payouts))
                .ToList();
        }

        /// <summary>
        /// Gets the products a store has fetched. The collection stays up to date with that store.
        /// </summary>
        /// <param name="storeName">The store to read.</param>
        /// <returns>The store's products, or an empty collection if it isn't on this controller.</returns>
        public ReadOnlyObservableCollection<Product> GetProducts(string storeName)
        {
            return TryGet(storeName)?.GetProducts() ?? new ReadOnlyObservableCollection<Product>(new ObservableCollection<Product>());
        }

        /// <summary>
        /// Gets a product a store has fetched, by its product id.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <param name="storeName">The store to search.</param>
        /// <returns>The product, or null if the store doesn't have it or isn't on this controller.</returns>
        public Product? GetProductById(string productId, string storeName)
        {
            return TryGet(storeName)?.GetProductById(productId);
        }

        /// <summary>
        /// Gets a product a store has fetched, by its catalog listing id.
        /// </summary>
        /// <param name="catalogListingId">The catalog listing id.</param>
        /// <param name="storeName">The store to search.</param>
        /// <returns>The product, or null if the store doesn't have it or isn't on this controller.</returns>
        public Product? GetProductByCatalogListingId(string catalogListingId, string storeName)
        {
            return TryGet(storeName)?.GetProductByCatalogListingId(catalogListingId);
        }
#endregion

#region Purchases
        /// <summary>
        /// Purchases a product from the store it was fetched from (<see cref="Product.StoreName"/>). If that store isn't
        /// on this controller, the failure is reported through <see cref="OnPurchaseFailed"/>.
        /// </summary>
        /// <param name="product">The product to purchase.</param>
        public void PurchaseProduct(Product product)
        {
            if (product == null)
            {
                LogError(NullArgument(nameof(PurchaseProduct), "product"));
                return;
            }
            if (product.StoreName == null)
            {
                LogError(NoStoreName("The product"));
                return;
            }
            var controller = TryGet(product.StoreName);
            if (controller == null)
            {
                ReportPurchaseFailed(product.StoreName, new Cart(product), NotOnController(product.StoreName));
                return;
            }
            controller.PurchaseProduct(product);
        }

        /// <summary>
        /// Purchases a product from a store by its catalog listing id. If the store has no such product or isn't on
        /// this controller, the failure is reported through <see cref="OnPurchaseFailed"/>.
        /// </summary>
        /// <param name="catalogListingId">The catalog listing id of the product.</param>
        /// <param name="storeName">The store the player pays through.</param>
        public void PurchaseProduct(string catalogListingId, string storeName)
        {
            var controller = TryGet(storeName);
            if (controller == null)
            {
                if (storeName != null)
                {
                    ReportPurchaseFailed(storeName, new Cart(Product.CreateUnknownProduct(catalogListingId)), NotOnController(storeName));
                }
                return;
            }
            controller.PurchaseProduct(catalogListingId);
        }

        /// <summary>
        /// Purchases a cart from the store its products were fetched from. A cart with products from more than one
        /// store, or from a store that isn't on this controller, is reported through <see cref="OnPurchaseFailed"/>
        /// under its first product's store.
        /// </summary>
        /// <param name="cart">The cart to purchase.</param>
        public void Purchase(ICart cart)
        {
            if (cart == null)
            {
                LogError(NullArgument(nameof(Purchase), "cart"));
                return;
            }
            var storeNames = Order.StoreNamesOf(cart);
            if (storeNames.Count == 0 || storeNames.Contains(null))
            {
                LogError(NoStoreName("The cart is empty or has a product that"));
                return;
            }
            var names = storeNames.OfType<string>().ToList();
            if (names.Count > 1)
            {
                var message = MixedStores(names);
                LogError(message);
                ReportPurchaseFailed(names[0], cart, message);
                return;
            }

            var controller = TryGet(names[0]);
            if (controller == null)
            {
                ReportPurchaseFailed(names[0], cart, NotOnController(names[0]));
                return;
            }
            controller.Purchase(cart);
        }

        void ReportPurchaseFailed(string storeName, ICart cart, string message)
        {
            var failedOrder = new FailedOrder(cart, PurchaseFailureReason.ProductUnavailable, message) { StoreName = storeName };
            m_OnPurchaseFailed.Handlers?.Invoke(storeName, failedOrder);
        }

        /// <summary>
        /// Confirms a pending order with the store it belongs to (<see cref="Order.StoreName"/>). An order whose store
        /// isn't on this controller, or whose cart has products from another store, is not confirmed: the error is logged
        /// and a <see cref="FailedOrder"/> is reported through <see cref="OnPurchaseConfirmed"/> instead. An order with no
        /// store name is not confirmed and only logged.
        /// </summary>
        /// <param name="order">The pending order.</param>
        public void ConfirmPurchase(PendingOrder order)
        {
            if (order == null)
            {
                LogError(NullArgument(nameof(ConfirmPurchase), "order"));
                return;
            }
            // An order is bought in one store, and every product in its cart comes from that store.
            var storeName = order.StoreName;
            var cartStores = Order.StoreNamesOf(order.CartOrdered);
            if (storeName == null)
            {
                // Order.StoreName falls back to the store the cart's products share, so only a mix is left to report.
                var names = cartStores.OfType<string>().ToList();
                if (names.Count > 1 && !cartStores.Contains(null))
                {
                    var mixed = MixedStores(names);
                    LogError(mixed);
                    ReportConfirmFailed(names[0], order, mixed);
                }
                else
                {
                    LogError(NoStoreName("The order"));
                }
                return;
            }
            if (cartStores.Any(cartStore => cartStore != storeName))
            {
                var mismatch = $"The order belongs to store '{storeName}', but its cart has products from {string.Join(" and ", cartStores.Select(name => name ?? "no store"))}.";
                LogError(mismatch);
                ReportConfirmFailed(storeName, order, mismatch);
                return;
            }

            var controller = TryGet(storeName);
            if (controller == null)
            {
                ReportConfirmFailed(storeName, order, NotOnController(storeName));
                return;
            }
            controller.ConfirmPurchase(order);
        }

        void ReportConfirmFailed(string storeName, PendingOrder order, string message)
        {
            var failedOrder = new FailedOrder(order, PurchaseFailureReason.Unknown, message) { StoreName = storeName };
            m_OnPurchaseConfirmed.Handlers?.Invoke(storeName, failedOrder);
        }

        /// <summary>
        /// Checks the entitlement for a product with the store it was fetched from (<see cref="Product.StoreName"/>).
        /// The result is reported through <see cref="OnCheckEntitlement"/>; if that store isn't on this controller, with
        /// status <see cref="EntitlementStatus.Unknown"/> and the reason in <see cref="Entitlement.ErrorMessage"/>.
        /// </summary>
        /// <param name="product">The product to check.</param>
        public void CheckEntitlement(Product product)
        {
            if (product == null)
            {
                LogError(NullArgument(nameof(CheckEntitlement), "product"));
                return;
            }
            if (product.StoreName == null)
            {
                LogError(NoStoreName("The product"));
                return;
            }
            var controller = TryGet(product.StoreName);
            if (controller == null)
            {
                m_OnCheckEntitlement.Handlers?.Invoke(product.StoreName, new Entitlement(product, null, EntitlementStatus.Unknown, NotOnController(product.StoreName)));
                return;
            }
            controller.CheckEntitlement(product);
        }

        /// <summary>
        /// Fetches purchases. Results are reported through <see cref="OnPurchasesFetched"/> and <see cref="OnPurchasesFetchFailed"/>, once per store.
        /// </summary>
        /// <param name="storeName">The store to fetch from, or null for every store.</param>
        public void FetchPurchases(string? storeName = null)
        {
            var targets = Targets(storeName, name => m_OnPurchasesFetchFailed.Handlers?.Invoke(name,
                new PurchasesFetchFailureDescription(PurchasesFetchFailureReason.Unknown, NotOnController(name))));
            foreach (var controller in targets)
            {
                controller.FetchPurchases();
            }
        }

        /// <summary>
        /// Configures whether pending orders are processed when purchases are fetched.
        /// </summary>
        /// <param name="shouldProcess">True to process pending orders after fetching purchases.</param>
        /// <param name="storeName">The store to configure, or null for every store.</param>
        public void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess, string? storeName = null)
        {
            foreach (var controller in Targets(storeName, _ => { }))
            {
                controller.ProcessPendingOrdersOnPurchasesFetched(shouldProcess);
            }
        }

        /// <summary>
        /// Restores transactions. The callback is invoked once per store, as each finishes, with the store name,
        /// whether the restore succeeded, and an error message if it didn't.
        /// </summary>
        /// <param name="callback">Invoked with (storeName, success, error) for each store.</param>
        /// <param name="storeName">The store to restore, or null for every store.</param>
        public void RestoreTransactions(Action<string, bool, string?>? callback, string? storeName = null)
        {
            if (storeName != null && TryGet(storeName) == null)
            {
                callback?.Invoke(storeName, false, NotOnController(storeName));
                return;
            }
            foreach (var store in m_Stores.Where(s => storeName == null || s.Name == storeName))
            {
                var name = store.Name;
                store.Controller.RestoreTransactions(callback == null ? null : (success, error) => callback(name, success, error));
            }
        }

        /// <summary>
        /// Gets a store's purchases. The collection stays up to date with that store.
        /// </summary>
        /// <param name="storeName">The store to read.</param>
        /// <returns>The store's purchases, or an empty collection if it isn't on this controller.</returns>
        public ReadOnlyObservableCollection<Order> GetPurchases(string storeName)
        {
            return TryGet(storeName)?.GetPurchases() ?? new ReadOnlyObservableCollection<Order>(new ObservableCollection<Order>());
        }
#endregion

#region ExtendedServices
        /// <summary>The Apple store extended service of this controller's App Store store, or null if it has none.</summary>
        public IAppleStoreExtendedService? AppleStoreExtendedService => Extension(c => c.AppleStoreExtendedService);

        /// <summary>The Google Play store extended service of this controller's Google Play store, or null if it has none.</summary>
        public IGooglePlayStoreExtendedService? GooglePlayStoreExtendedService => Extension(c => c.GooglePlayStoreExtendedService);

        /// <summary>The Payment Provider extended store service of this controller's Payment Provider store, or null if it has none.</summary>
        public IPaymentProvidersExtendedService? PaymentProvidersExtendedService => Extension(c => c.PaymentProvidersExtendedService);

        /// <summary>The Apple extended product service of this controller's App Store store, or null if it has none.</summary>
        public IAppleStoreExtendedProductService? AppleStoreExtendedProductService => Extension(c => c.AppleStoreExtendedProductService);

        /// <summary>The Apple extended purchase service of this controller's App Store store, or null if it has none.</summary>
        public IAppleStoreExtendedPurchaseService? AppleStoreExtendedPurchaseService => Extension(c => c.AppleStoreExtendedPurchaseService);

        /// <summary>The Google Play extended purchase service of this controller's Google Play store, or null if it has none.</summary>
        public IGooglePlayStoreExtendedPurchaseService? GooglePlayStoreExtendedPurchaseService => Extension(c => c.GooglePlayStoreExtendedPurchaseService);

        /// <summary>The Payment Provider extended purchase service of this controller's Payment Provider store, or null if it has none.</summary>
        public IPaymentProvidersExtendedPurchaseService? PaymentProvidersExtendedPurchaseService => Extension(c => c.PaymentProvidersExtendedPurchaseService);
#endregion

#region Routing
        // The store's controller, or null with an error logged: when no store name is given, or the store isn't on
        // this controller.
        StoreController? TryGet(string? storeName)
        {
            if (storeName == null)
            {
                LogError(NoStoreName("The call"));
                return null;
            }
            foreach (var store in m_Stores)
            {
                if (store.Name == storeName)
                {
                    return store.Controller;
                }
            }
            LogError(NotOnController(storeName));
            return null;
        }

        // Every store when no name is given; otherwise that store, or none after reporting it isn't on this controller.
        IEnumerable<StoreController> Targets(string? storeName, Action<string> reportNotOnController)
        {
            if (storeName == null)
            {
                return m_Stores.Select(s => s.Controller);
            }
            var controller = TryGet(storeName);
            if (controller == null)
            {
                reportNotOnController(storeName);
                return Enumerable.Empty<StoreController>();
            }
            return new[] { controller };
        }

        // Every failure this controller reports itself is logged, so none is silent when no handler is subscribed.
        static void LogError(string message)
        {
            Debug.unityLogger.LogIAPError(message);
        }

        string NotOnController(string storeName)
        {
            return $"Store '{storeName}' is not on this PurchasingController (stores: {string.Join(", ", StoreNames)}).";
        }

        // Products and orders from the SDK always know their store, so this only happens for a missing store name
        // argument or objects that never came from a store. With no store to name, it is logged but not reported.
        string NoStoreName(string subject)
        {
            return $"{subject} has no store name, so this PurchasingController can't route it (stores: {string.Join(", ", StoreNames)}).";
        }

        // Like a missing store name: there is no store to report it under, so it is logged only.
        static string NullArgument(string call, string argument)
        {
            return $"PurchasingController.{call} was called with a null {argument}; nothing was sent to any store.";
        }

        static string MixedStores(List<string> storeNames)
        {
            return $"A cart can only be purchased from one store, but its products come from {string.Join(" and ", storeNames)}.";
        }

        // Each kind of extension is provided by exactly one store: stores can't repeat and each kind is one store.
        T? Extension<T>(Func<StoreController, T?> extension) where T : class
        {
            return m_Stores.Select(s => extension(s.Controller)).FirstOrDefault(e => e != null);
        }
#endregion

        static StoreEvent<Action<T>, Action<string, T>> Event<T>(List<(string Name, StoreController Controller)> stores,
            Action<StoreController, Action<T>> subscribe, Action<StoreController, Action<T>> unsubscribe)
        {
            return new StoreEvent<Action<T>, Action<string, T>>(stores, subscribe, unsubscribe, (name, handlers) => arg => handlers()?.Invoke(name, arg));
        }

        static StoreEvent<Action, Action<string>> Event(List<(string Name, StoreController Controller)> stores,
            Action<StoreController, Action> subscribe, Action<StoreController, Action> unsubscribe)
        {
            return new StoreEvent<Action, Action<string>>(stores, subscribe, unsubscribe, (name, handlers) => () => handlers()?.Invoke(name));
        }

        // Subscribes to every store's event while the developer has at least one handler, so per-store subscriber
        // checks (DEBUG warnings, auth-change cache clearing) see the same state as with a single-store controller.
        sealed class StoreEvent<TStore, TPublic> where TStore : Delegate where TPublic : Delegate
        {
            readonly List<(string Name, StoreController Controller)> m_Stores;
            readonly Action<StoreController, TStore> m_Subscribe;
            readonly Action<StoreController, TStore> m_Unsubscribe;
            readonly Func<string, Func<TPublic?>, TStore> m_MakeForwarder;
            readonly List<(StoreController Controller, TStore Forwarder)> m_Forwarders = new();
            TPublic? m_Handlers;

            public StoreEvent(List<(string Name, StoreController Controller)> stores, Action<StoreController, TStore> subscribe,
                Action<StoreController, TStore> unsubscribe, Func<string, Func<TPublic?>, TStore> makeForwarder)
            {
                m_Stores = stores;
                m_Subscribe = subscribe;
                m_Unsubscribe = unsubscribe;
                m_MakeForwarder = makeForwarder;
            }

            // The developer's handlers, for failures the controller reports itself (no store raises them).
            public TPublic? Handlers => m_Handlers;

            public void Add(TPublic? handler)
            {
                if (handler == null)
                {
                    return;
                }
                if (m_Handlers == null)
                {
                    foreach (var (name, controller) in m_Stores)
                    {
                        var forwarder = m_MakeForwarder(name, () => m_Handlers);
                        m_Subscribe(controller, forwarder);
                        m_Forwarders.Add((controller, forwarder));
                    }
                }
                m_Handlers = (TPublic)Delegate.Combine(m_Handlers, handler);
            }

            public void Remove(TPublic? handler)
            {
                m_Handlers = (TPublic?)Delegate.Remove(m_Handlers, handler);
                if (m_Handlers == null)
                {
                    foreach (var (controller, forwarder) in m_Forwarders)
                    {
                        m_Unsubscribe(controller, forwarder);
                    }
                    m_Forwarders.Clear();
                }
            }
        }
    }
}
