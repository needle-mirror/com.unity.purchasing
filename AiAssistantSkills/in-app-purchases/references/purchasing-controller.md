# PurchasingController (v5.5+)

`PurchasingController` is the controller to use for **all code you write**: one object for every store the app sells through — the platform store (Apple App Store / Google Play), `PaymentProvider.Name` for IAP D2C Capabilities, or both. A single-store app uses it too: `new PurchasingController(UnityIAPServices.GetDefaultStore())`. Do not write new code against `StoreController`.

**Version:** `PurchasingController` is in `com.unity.purchasing` v5.5+. Check the installed package actually contains the type. If it doesn't, recommend upgrading the package; fall back to `StoreController` only if the user can't or won't upgrade.

**Existing projects on `StoreController`:** offer to migrate (see [Migrating from StoreController](#migrating-from-storecontroller)) the whole project, and do it only if the user agrees. Never migrate unasked. If the user declines, stay on `StoreController` throughout, new code included: never add a `PurchasingController` next to existing `StoreController`s.

## Table of Contents

- [Anti-Hallucination: PurchasingController](#anti-hallucination-purchasingcontroller)
- [Key Members](#key-members)
- [Routing Rules](#routing-rules)
- [Errors](#errors)
- [Initialization Example (platform store + D2C)](#initialization-example-platform-store--d2c)
- [Fetching Products per Store](#fetching-products-per-store)
- [Payment Options Picker](#payment-options-picker)
- [Migrating from StoreController](#migrating-from-storecontroller)
- [Validation](#validation)

## Anti-Hallucination: PurchasingController

These do NOT exist — do not use them:

| WRONG (does not exist) | CORRECT |
|---|---|
| `UnityIAPServices.PurchasingController(...)` | `new PurchasingController(storeNames)` |
| `IPurchasingController` | `PurchasingController` (concrete class only) |
| Passing a `PurchasingController` where `IStoreService` / `IProductService` / `IPurchaseService` is expected | It does not implement them. Code that needs them stays on `StoreController` — don't mix it with a `PurchasingController` |
| `controller.GetProducts()` / `GetPurchases()` / `GetProductById(id)` without a store name | `GetProducts(storeName)`, `GetPurchases(storeName)`, `GetProductById(id, storeName)` |
| `controller.GetConnectionState()` without a store name | `GetConnectionState(storeName)` — no combined state; use the connection events |
| `controller.PurchaseProduct(product, storeName)` / `Purchase(cart, storeName)` / `CheckEntitlement(product, storeName)` | `PurchaseProduct(product)`, `Purchase(cart)`, `CheckEntitlement(product)` — routed by `product.StoreName` |
| `controller.OnPurchasePending += order => ...` | `controller.OnPurchasePending += (storeName, order) => ...` — every event takes the store name first |
| `controller.ForStore(name).OnPurchasePending` / per-store subscription | One subscription covers every store — filter on the `storeName` parameter |
| `controller.RestoreTransactions((success, error) => ...)` | `RestoreTransactions((storeName, success, error) => ...)` |

## Key Members

```csharp
public PurchasingController(params string[] storeNames)
IReadOnlyList<string> StoreNames { get; }   // constructor order

// Background calls — storeName null (default) = every store, a name = that store only
Task Connect(string? storeName = null)
void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy, string? storeName = null)
void FetchPurchases(string? storeName = null)
void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess, string? storeName = null)
void RestoreTransactions(Action<string, bool, string?>? callback, string? storeName = null)   // callback once per store

// Product fetching — one store per call (each store needs its own definitions)
void FetchProducts(List<ProductDefinition> productDefinitions, string storeName, IRetryPolicy? retryPolicy = null)
void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions, string storeName)

// Getters — storeName required
ConnectionState GetConnectionState(string storeName)
ReadOnlyObservableCollection<Product> GetProducts(string storeName)   // that store's live collection
ReadOnlyObservableCollection<Order> GetPurchases(string storeName)
Product? GetProductById(string productId, string storeName)
Product? GetProductByCatalogListingId(string catalogListingId, string storeName)

// Purchases — routed by the object's store
void PurchaseProduct(Product product)                              // product.StoreName
void PurchaseProduct(string catalogListingId, string storeName)   // a plain id names its store
void Purchase(ICart cart)                                          // the store all cart products share
void ConfirmPurchase(PendingOrder order)                           // order.StoreName
void CheckEntitlement(Product product)                             // product.StoreName

// Events — same names as StoreController, store name first
event Action<string>? OnStoreConnected
event Action<string, StoreConnectionFailureDescription>? OnStoreDisconnected
event Action<string>? OnAuthAccountChanged
event Action<string, List<Product>>? OnProductsFetched
event Action<string, ProductFetchFailed>? OnProductsFetchFailed
event Action<string, PendingOrder>? OnPurchasePending
event Action<string, Order>? OnPurchaseConfirmed        // ConfirmedOrder or FailedOrder — pattern-match
event Action<string, FailedOrder>? OnPurchaseFailed
event Action<string, DeferredOrder>? OnPurchaseDeferred
event Action<string, Orders>? OnPurchasesFetched
event Action<string, PurchasesFetchFailureDescription>? OnPurchasesFetchFailed
event Action<string, Entitlement>? OnCheckEntitlement

// Platform extensions — same 7 properties as StoreController, from the store that provides them (null if none)
IAppleStoreExtendedService? AppleStoreExtendedService { get; }
IGooglePlayStoreExtendedService? GooglePlayStoreExtendedService { get; }
IPaymentProvidersExtendedService? PaymentProvidersExtendedService { get; }
IAppleStoreExtendedProductService? AppleStoreExtendedProductService { get; }
IAppleStoreExtendedPurchaseService? AppleStoreExtendedPurchaseService { get; }
IGooglePlayStoreExtendedPurchaseService? GooglePlayStoreExtendedPurchaseService { get; }
IPaymentProvidersExtendedPurchaseService? PaymentProvidersExtendedPurchaseService { get; }
```

`Product.StoreName` and `Order.StoreName` (v5.5+) name the store a product was fetched from or an order belongs to. They are set by Unity IAP; they are read-only.

## Routing Rules

- **Use `PurchasingController` or `StoreController`s, never both.** They share each store's services, so both receive the same events (handlers run twice), and a product handed from one to the other can produce an order the `PurchasingController` refuses to confirm.
- **One store per purchase.** A product, cart or order belongs to one store, and an order's cart products all come from that store. A cart mixing products from two stores is not purchased.
- **Connect completes when every targeted store has connected or failed.** It does not throw for a failed store — the failure arrives on `OnStoreDisconnected(storeName, ...)`. Stores connect concurrently; do not rely on an order between them.
- **Each store fetches its own products.** The same catalog item fetched by two stores is two `Product` objects with different `StoreName`s and their own prices. Purchase the one from the store the player pays through.
- **`OnAuthAccountChanged` fires once per store.** That store's caches are already cleared and the Remote Catalog is stale — fetch the catalog again, then refetch that store only: `FetchProducts(defs, storeName)`, then `FetchPurchases(storeName)` from `OnProductsFetched` (StoreKit 1 needs the products cached first).
- **Per-store handling = filter on the parameter:** `if (storeName != PaymentProvider.Name) return;`.
- **Constructor:** throws `ArgumentException` for no names, a null/empty name or a duplicate name, and `StoreException` for a name that isn't a registered store. Nothing else throws.
- **D2C:** constructing the controller with `PaymentProvider.Name` obtains the payment provider store, so construct it only after `UnityServices.InitializeAsync()` and player sign-in (see [path-implement-iap-d2c.md](path-implement-iap-d2c.md)).

## Errors

After construction, `PurchasingController` never throws. Mistakes are logged as errors (tag `InAppPurchasing`) and reported through the call's usual callback:

| Mistake | Reported as |
|---|---|
| Store name not on the controller (typo, or a product/order from another controller) | The call's failure callback under that store name: `OnStoreDisconnected`, `OnProductsFetchFailed`, `OnPurchasesFetchFailed`, `OnPurchaseFailed`, `OnCheckEntitlement` (`Unknown`), the restore callback with `success == false`. Getters return an empty collection, `null` or `ConnectionState.Unavailable` |
| Cart with products from more than one store | `OnPurchaseFailed` under the first product's store, `PurchaseFailureReason.ProductUnavailable` |
| `ConfirmPurchase` on an order whose cart has products from another store, or whose store isn't on the controller | `OnPurchaseConfirmed` with a `FailedOrder`; nothing is confirmed |
| A missing store name (`null` store name, empty cart, product/order with no store), or a `null` product, cart or order | Logged only — no event, since there is no store to report it under |
| `null` product definition list passed to `FetchProducts` / `FetchProductsWithNoRetries` | Logged only — nothing is fetched |
| `null` entries in that list | Logged; the other definitions are fetched |
| Store not on the controller for `SetStoreReconnectionRetryPolicyOnDisconnection` / `ProcessPendingOrdersOnPurchasesFetched` | Logged only — these calls have no callback |

Subscribe to the failure events (see SKILL.md Required Event Subscriptions) — they carry these reports.

## Initialization Example (platform store + D2C)

```csharp
PurchasingController m_Controller;
readonly RemoteCatalogProvider m_Catalog = new();   // one provider for the whole session, shared by every store
readonly string m_NativeStore = UnityIAPServices.GetDefaultStore();   // Apple App Store or Google Play on device

async void Awake()
{
    await UnityServices.InitializeAsync();
    await SignInPlayer();   // Unity Authentication with a persistent identity — see path-implement-iap-d2c.md

    m_Controller = new PurchasingController(m_NativeStore, PaymentProvider.Name);

    // Subscribe to ALL events BEFORE Connect — one subscription covers every store
    m_Controller.OnPurchasePending += OnPurchasePending;
    m_Controller.OnPurchaseConfirmed += (store, order) =>
    {
        if (order is FailedOrder failed)
            Debug.LogError($"[{store}] Confirmation failed: {failed.FailureReason} - {failed.Details}");
    };
    m_Controller.OnPurchaseFailed += (store, failed) => Debug.LogError($"[{store}] {failed.FailureReason} - {failed.Details}");
    m_Controller.OnPurchaseDeferred += (store, deferred) => Debug.Log($"[{store}] Purchase deferred");
    m_Controller.OnProductsFetched += (store, products) =>
    {
        Debug.Log($"[{store}] {products.Count} products ready");
        m_Controller.FetchPurchases(store);   // after the products: StoreKit 1 (iOS < 15) matches purchases against them
    };
    m_Controller.OnProductsFetchFailed += (store, failure) => Debug.LogError($"[{store}] Fetch failed: {failure.FailureReason}");
    m_Controller.OnPurchasesFetched += (store, orders) => { };
    m_Controller.OnPurchasesFetchFailed += (store, failure) => Debug.LogError($"[{store}] {failure.Message}");
    m_Controller.OnStoreConnected += FetchStore;   // each store fetches its own products when it connects
    m_Controller.OnStoreDisconnected += (store, failure) => Debug.LogError($"[{store}] Disconnected: {failure.Message}");
    m_Controller.OnAuthAccountChanged += async storeName =>
    {
        // That store's caches were cleared and the catalog is stale: fetch it again, then that store.
        // Fires once per store, so the catalog is fetched once per store — correct, just redundant.
        if (await FetchCatalog())
            FetchStore(storeName);
    };

    if (await FetchCatalog())          // once, before connecting
        await m_Controller.Connect();  // every store
}

async Task<bool> FetchCatalog()
{
    var result = await m_Catalog.FetchRemoteCatalog();   // replaces the previous catalog
    if (!result.Success)
        Debug.LogError($"Remote Catalog fetch failed: {result.Exception?.Message}");
    return result.Success;
}

void FetchStore(string storeName)
{
    // The provider hands out that store's IDs; the lambda passes them to the same store.
    m_Catalog.FetchProducts(defs => m_Controller.FetchProductsWithNoRetries(defs, storeName), storeName);
    // Purchases are fetched in OnProductsFetched, once this store's products are cached.
}

void OnPurchasePending(string storeName, PendingOrder order)
{
    GrantContent(order);                  // save before confirming
    m_Controller.ConfirmPurchase(order);  // routed by order.StoreName
}
```

Without D2C, use `new PurchasingController(UnityIAPServices.GetDefaultStore())` and skip the sign-in. Keep `UnityServices.InitializeAsync()` when using the Remote Catalog; with a local `List<ProductDefinition>`, drop it and the catalog fetch, and call `m_Controller.FetchProducts(productDefinitions, storeName)` in `FetchStore`.

## Fetching Products per Store

`FetchProducts` and `FetchProductsWithNoRetries` fetch **one store per call**: a `ProductDefinition` carries a single store-specific ID, so each store needs its own definitions. One catalog provider serves every store — the controller gives each store its own copy of the definitions. See `FetchStore` in the example above: fetch the remote catalog once, then fetch each store with its own store name.

From a local list with the same IDs everywhere: `foreach (var store in m_Controller.StoreNames) m_Controller.FetchProducts(productDefinitions, store);`

Do NOT pass the controller's method as a method group (`catalog.FetchProducts(m_Controller.FetchProductsWithNoRetries)`) — it needs a store name, so it won't compile. Use the lambda above.

## Payment Options Picker

Get it from the controller as with a `StoreController`, and keep using the catalog listing ID:

```csharp
var picker = m_Controller.PaymentProvidersExtendedService?.GetPaymentOptionProviderUGUI();   // or GetPaymentOptionProviderUITK(hostDocument)
if (picker == null)
{
    m_Controller.PurchaseProduct(catalogListingId, UnityIAPServices.GetDefaultStore());   // no Payment Provider store: buy natively
    return;
}
await picker.ShowPurchaseOption(catalogListingId);
```

Purchases made from the picker arrive on the controller's events (`OnPurchasePending(storeName, order)` etc.) because stores share their services — **as long as the controller includes every store the picker can offer**: the platform default store (`UnityIAPServices.GetDefaultStore()`) and `PaymentProvider.Name`.

Native fallback when no payment provider is eligible: `m_Controller.PurchaseProduct(catalogListingId, m_NativeStore)` — no second controller needed.

## Migrating from StoreController

Offer this when a project already uses `StoreController`; migrate only if the user agrees. `StoreController` is not deprecated — existing code keeps working.

1. Confirm the installed package contains `PurchasingController` (v5.5+).
2. Replace every `StoreController` (one per store) with **one** `new PurchasingController(storeNames...)`, created where the earliest one was — after Unity Services sign-in if one of them used `PaymentProvider.Name`.
3. Collapse the duplicated event wiring into one subscription per event and add the `storeName` parameter to each handler. Handlers that only cared about one store filter on it.
4. Apply the call mapping below.
5. Check Codeless: a `PurchasingController` creates the same store services as `StoreController`, so the Codeless auto-initialization race in [codeless-catalog.md](codeless-catalog.md) applies unchanged.

| StoreController | PurchasingController |
|---|---|
| `UnityIAPServices.StoreController()` + `UnityIAPServices.StoreController(PaymentProvider.Name)` | `new PurchasingController(UnityIAPServices.GetDefaultStore(), PaymentProvider.Name)` |
| `a.OnPurchasePending += h; b.OnPurchasePending += h;` | `c.OnPurchasePending += (storeName, order) => h(order);` — same for all 12 events |
| `OnStoreConnected += () => ...` / `OnAuthAccountChanged += () => ...` | `+= storeName => ...` |
| `await a.Connect(); await b.Connect();` | `await c.Connect();` (or `Connect(storeName)` for one) |
| `a.FetchProducts(defs); b.FetchProducts(defs);` | `c.FetchProducts(defsA, storeA); c.FetchProducts(defsB, storeB);` — one call per store (see above) |
| `catalog.FetchProducts(a.FetchProductsWithNoRetries)` | `catalog.FetchProducts(defs => c.FetchProductsWithNoRetries(defs, store), store)` for each store |
| `a.FetchPurchases()` / `a.RestoreTransactions(cb)` | `c.FetchPurchases(storeName?)` / `c.RestoreTransactions((storeName, ok, err) => ..., storeName?)` |
| `a.GetProducts()` / `a.GetPurchases()` / `a.GetProductById(id)` | `c.GetProducts(storeName)` / `c.GetPurchases(storeName)` / `c.GetProductById(id, storeName)` |
| `a.GetConnectionState()` | `c.GetConnectionState(storeName)` |
| `a.PurchaseProduct(product)` / `a.Purchase(cart)` / `a.CheckEntitlement(product)` | Same call on `c` — goes to `product.StoreName` |
| `a.PurchaseProduct(catalogListingId)` | `c.PurchaseProduct(catalogListingId, storeName)` |
| `a.ConfirmPurchase(order)` | `c.ConfirmPurchase(order)` — goes to `order.StoreName` |
| `a.AppleStoreExtendedService` etc. | Same property on `c` |
| Code needing `IStoreService` / `IProductService` / `IPurchaseService` | Rewrite it against `PurchasingController`. If something requires the interfaces (e.g. a third-party plugin), don't migrate: keep the project on `StoreController` |

Watch for: code that passed a product from one controller to the other (it now goes to the product's own store), and handlers that assumed "this controller = this store" (read `storeName` instead).

## Validation

1. Every event handler takes the store name first; every success event has its failure event subscribed **before** `Connect()`.
2. Getters always pass a store name; purchases of a `Product` / cart / order do not.
3. Products are fetched once per store, each call naming its store; catalog providers are called with the same store name as the controller.
4. The controller includes the platform default store when the payment options picker is used.
5. With `PaymentProvider.Name`, the controller is constructed after Unity Services sign-in.
6. No "is not on this PurchasingController" / "has no store name" errors in the log during a test purchase.
