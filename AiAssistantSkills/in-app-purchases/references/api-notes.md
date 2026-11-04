# Unity IAP API Notes

## Table of Contents

- [Anti-Hallucination: v5 vs Legacy API](#anti-hallucination-v5-vs-legacy-api)
- [PurchasingController (v5.5+) — use for all new code](#purchasingcontroller-v55--use-for-all-new-code)
- [StoreController (existing projects and fallback)](#storecontroller-existing-projects-and-fallback)
- [Alternative Service Access (DefaultStore / DefaultProduct / DefaultPurchase)](#alternative-service-access-defaultstore--defaultproduct--defaultpurchase)
- [CatalogProvider](#catalogprovider)
- [Initialization Example](#initialization-example)
- [Product Definitions](#product-definitions)
  - [CatalogListings and uSku (v5.4+)](#cataloglistings-and-usku-v54)
- [Two-Step Purchase Flow](#two-step-purchase-flow)
- [Restore Transactions](#restore-transactions)
- [Entitlement Checking](#entitlement-checking)
- [Fetch Existing Purchases](#fetch-existing-purchases)
- [Receipt Validation](#receipt-validation)
  - [IAppleOrderInfo Complete Member List](#iappleorderinfo-complete-member-list)
- [Extended Service Events (NOT on the controller)](#extended-service-events-not-on-the-controller)
- [AppleStoreExtendedProductService Key Members](#applestoreextendedproductservice-key-members)
- [Subscription Info Access Path](#subscription-info-access-path)
- [Event Subscription Rules](#event-subscription-rules)
- [Failure Description Property Names](#failure-description-property-names)
- [CrossPlatformValidator](#crossplatformvalidator)
- [Interface Hierarchy](#interface-hierarchy)
- [IRunCommand Template: Fetch Products](#iruncommand-template-fetch-products)

## Anti-Hallucination: v5 vs Legacy API

Unity IAP v5 is the current API. The legacy API (v4) is `[Obsolete]`. Do NOT use legacy patterns.

| WRONG (Legacy v4) | CORRECT (v5) |
|---|---|
| `UnityPurchasing.Initialize(listener, builder)` | `new PurchasingController(UnityIAPServices.GetDefaultStore())` then `controller.Connect()` |
| `IStoreListener.OnInitialized(controller, extensions)` | `controller.OnStoreConnected` event — `storeName => ...` |
| `IStoreListener.ProcessPurchase(args)` | `controller.OnPurchasePending` event — `(storeName, pendingOrder) => ...` |
| `controller.InitiatePurchase(product)` | `controller.PurchaseProduct(product)` |
| `controller.ConfirmPendingPurchase(product)` | `controller.ConfirmPurchase(pendingOrder)` |
| `extensions.GetExtension<IAppleExtensions>()` | `controller.AppleStoreExtendedService` / `controller.AppleStoreExtendedPurchaseService` |
| `extensions.GetExtension<IGooglePlayStoreExtensions>()` | `controller.GooglePlayStoreExtendedService` / `controller.GooglePlayStoreExtendedPurchaseService` |
| `ConfigurationBuilder.Instance(...)` | Not needed; use `ProductDefinition` list with `FetchProducts` |

## Anti-Hallucination: Common v5 Mistakes

Do not use these — they don't exist, or are obsolete in v5:

| WRONG | CORRECT |
|---|---|
| `controller.OnStoreConnectionFailed` | `controller.OnStoreDisconnected` — receives `(storeName, StoreConnectionFailureDescription)` |
| `controller.FetchProducts(defs, successCb, failCb)` | `controller.FetchProducts(List<ProductDefinition>, storeName)` — subscribe to `OnProductsFetched`/`OnProductsFetchFailed` events separately |
| `controller.FetchPurchases(successCb, failCb)` | `controller.FetchPurchases()` — subscribe to `OnPurchasesFetched`/`OnPurchasesFetchFailed` events separately |
| `product.receipt` | `order.Info.Receipt` — receipt is on the order, not the product |
| `product.hasReceipt` | `controller.CheckEntitlement(product)` + `OnCheckEntitlement` |
| `new SubscriptionManager(product, introJson)` | `order.Info.PurchasedProductInfo` — see Subscription Info Access Path |
| `pendingOrder.OrderInfo.Apple.jwsRepresentation` | `pendingOrder.Info.Apple?.jwsRepresentation` — note `Info` not `OrderInfo`, and null-check `?.` |

For new code, use `new PurchasingController(UnityIAPServices.GetDefaultStore())` instead of `UnityIAPServices.StoreController()` — see [purchasing-controller.md](purchasing-controller.md). `StoreController` still exists, for packages without `PurchasingController` and for existing projects.

## PurchasingController (v5.5+) — use for all new code

`PurchasingController` is the controller for all code you write: one object for every store the app sells through, a single store included — `new PurchasingController(UnityIAPServices.GetDefaultStore())`, or `new PurchasingController(UnityIAPServices.GetDefaultStore(), PaymentProvider.Name)` with IAP D2C Capabilities. Every event takes the store name first, getters require a store name, and purchases route by `Product.StoreName` / `Order.StoreName`. Full members, rules, errors and the migration mapping: [purchasing-controller.md](purchasing-controller.md).

It requires v5.5+ — check the installed package contains it. If it doesn't, recommend upgrading; use `StoreController` (next section) only if the user can't or won't upgrade. It is **not** a `StoreController` and does not implement the three service interfaces: do not apply `StoreController` signatures to it.

**`OnAuthAccountChanged` (fires once per store):** that store's cached products and purchases are cleared before the event. Re-fetch that store only:

```csharp
controller.OnAuthAccountChanged += async storeName =>
{
    var catalogProvider = new RemoteCatalogProvider();
    var catalogResult = await catalogProvider.FetchRemoteCatalog();
    if (!catalogResult.Success)
    {
        Debug.LogError($"[{storeName}] Remote Catalog fetch failed: {catalogResult.Exception?.Message}");
        return;
    }
    catalogProvider.FetchProducts(defs => controller.FetchProductsWithNoRetries(defs, storeName), storeName);

    // Or, from a local list: controller.FetchProducts(productDefinitions, storeName);

    // Then FetchPurchases(storeName) from OnProductsFetched: StoreKit 1 (iOS < 15) needs the products cached first.
};
```

Do not read `GetProducts(storeName)` or `GetPurchases(storeName)` inside the handler — they are empty.

## StoreController (existing projects and fallback)

Use these signatures only to read or maintain code that already uses `StoreController`, or when the installed package has no `PurchasingController` and the user can't upgrade. For an existing `StoreController` project, offer to migrate (see [purchasing-controller.md](purchasing-controller.md#migrating-from-storecontroller)) and do it only if the user agrees.


```csharp
// Store lifecycle
Task Connect()
void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy)
event Action? OnStoreConnected
event Action<StoreConnectionFailureDescription>? OnStoreDisconnected

// Products
void FetchProducts(List<ProductDefinition> defs, IRetryPolicy? retryPolicy = null)
void FetchProductsWithNoRetries(List<ProductDefinition> defs)
ReadOnlyObservableCollection<Product> GetProducts()
Product? GetProductById(string productId)
event Action<List<Product>>? OnProductsFetched
event Action<ProductFetchFailed>? OnProductsFetchFailed

// Purchasing
void PurchaseProduct(Product product)
void PurchaseProduct(string catalogListingId)
void Purchase(ICart cart)
void ConfirmPurchase(PendingOrder order)
void FetchPurchases()
void CheckEntitlement(Product product)
void RestoreTransactions(Action<bool, string?>? callback)
ReadOnlyObservableCollection<Order> GetPurchases()
void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess)

// Purchase events
event Action<PendingOrder>? OnPurchasePending
event Action<Order>? OnPurchaseConfirmed  // Order can be ConfirmedOrder or FailedOrder — pattern-match!
event Action<FailedOrder>? OnPurchaseFailed
event Action<DeferredOrder>? OnPurchaseDeferred
event Action<Orders>? OnPurchasesFetched
event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed
event Action<Entitlement>? OnCheckEntitlement

// Account change (v5.4+)
event Action? OnAuthAccountChanged

// Platform extensions (null on non-matching platforms — always null-check)
IAppleStoreExtendedService? AppleStoreExtendedService { get; }
IGooglePlayStoreExtendedService? GooglePlayStoreExtendedService { get; }
IAppleStoreExtendedProductService? AppleStoreExtendedProductService { get; }
IAppleStoreExtendedPurchaseService? AppleStoreExtendedPurchaseService { get; }
IGooglePlayStoreExtendedPurchaseService? GooglePlayStoreExtendedPurchaseService { get; }
IPaymentProvidersExtendedService? PaymentProvidersExtendedService { get; }   // 5.4.x: PaymentProviderStoreExtendedService (still works in 5.5)
IPaymentProvidersExtendedPurchaseService? PaymentProvidersExtendedPurchaseService { get; }
```

**`StoreController.OnAuthAccountChanged` — breaking behavior in v5.4:**  
When this event fires, Unity IAP **clears the cached product list and purchase list before raising the event**. Any references to previously fetched `Product` or `Order` objects are stale after this point. Re-run the full init sequence in the handler:

```csharp
store.OnAuthAccountChanged += async () =>
{
    // Products and purchases have already been cleared — re-fetch everything.

    // Using the Remote Catalog. Works for native stores and D2C alike: a catalog
    // listing carries store-specific IDs for Apple, Google, macOS and Xbox.
    var catalogProvider = new RemoteCatalogProvider();

    // Stop if the fetch failed, after its retries are exhausted. A failed fetch leaves
    // the provider with no definitions, and forwarding that empty list only produces a
    // second, more confusing failure from FetchProducts.
    var catalogResult = await catalogProvider.FetchRemoteCatalog();
    if (!catalogResult.Success)
    {
        Debug.LogError($"Remote Catalog fetch failed: {catalogResult.Exception?.Message}");
        return;
    }

    // Pass the same store name the controller was created with, so the catalog
    // hands over that store's IDs:
    //   UnityIAPServices.StoreController()                     -> UnityIAPServices.GetDefaultStore()
    //   UnityIAPServices.StoreController(PaymentProvider.Name) -> PaymentProvider.Name
    // The one-argument FetchProducts overload assumes the platform default, which
    // is wrong for a Payment Provider controller.
    var storeName = UnityIAPServices.GetDefaultStore();
    catalogProvider.FetchProducts(store.FetchProductsWithNoRetries, storeName);

    // Or, if the products come from a local list rather than the Remote Catalog:
    // store.FetchProducts(productDefinitions);

    // Then store.FetchPurchases() from OnProductsFetched: StoreKit 1 (iOS < 15) needs the products cached first.
};
```

Do not read from `GetProducts()` or `GetPurchases()` inside `OnAuthAccountChanged` — they will be empty.

## Alternative Service Access (DefaultStore / DefaultProduct / DefaultPurchase)

Only for code that needs `IStoreService` / `IProductService` / `IPurchaseService` — `PurchasingController` does not implement them. You can access a store's individual services:

```csharp
IStoreService m_StoreService = UnityIAPServices.DefaultStore();
IProductService m_ProductService = UnityIAPServices.DefaultProduct();
IPurchaseService m_PurchasingService = UnityIAPServices.DefaultPurchase();
```

These are one store's services (`UnityIAPServices.Store(name)` / `Product(name)` / `Purchase(name)` for a named store). Product fetching events go on `IProductService`, purchase events on `IPurchaseService`, store lifecycle on `IStoreService`.

## CatalogProvider

For managing complex product catalogs with store-specific IDs:

```csharp
var catalogProvider = new CatalogProvider();

var products = new List<ProductDefinition>
{
    new ProductDefinition("com.mygame.gems50", ProductType.Consumable),
    new ProductDefinition("com.mygame.pass", ProductType.Subscription)
};

var storeSpecificIds = new Dictionary<string, StoreSpecificIds>
{
    { "com.mygame.pass", new StoreSpecificIds
        {
            { "com.mygame.google.pass", GooglePlay.Name },
            { "com.mygame.ios.pass", AppleAppStore.Name }
        }
    }
};

catalogProvider.AddProducts(products, storeSpecificIds);
var storeName = UnityIAPServices.GetDefaultStore();
catalogProvider.FetchProducts(defs => controller.FetchProductsWithNoRetries(defs, storeName), storeName);
```

With several stores on the controller, fetch each store in turn — one provider is enough, the controller copies each store's definitions:

```csharp
foreach (var store in controller.StoreNames)
    catalogProvider.FetchProducts(defs => controller.FetchProductsWithNoRetries(defs, store), store);
```

## Initialization Example

```csharp
PurchasingController m_Controller;

async void Awake()
{
    // One store here; add PaymentProvider.Name for IAP D2C Capabilities (after Unity Services sign-in).
    m_Controller = new PurchasingController(UnityIAPServices.GetDefaultStore());

    // Subscribe to ALL events BEFORE Connect — pending purchases may fire on reconnect
    m_Controller.OnPurchasePending += OnPurchasePending;
    m_Controller.OnPurchaseConfirmed += (storeName, order) =>
    {
        if (order is FailedOrder failed)
            Debug.LogError($"[{storeName}] Confirmation failed: {failed.FailureReason} - {failed.Details}");
        else
            Debug.Log($"[{storeName}] Purchase complete");
    };
    m_Controller.OnPurchaseFailed += (storeName, failed) => Debug.LogError($"{failed.FailureReason} - {failed.Details}");
    m_Controller.OnPurchaseDeferred += (storeName, deferred) => Debug.Log("Purchase deferred (e.g., Ask-to-Buy)");
    m_Controller.OnProductsFetched += (storeName, fetched) =>
    {
        Debug.Log("Products ready");
        m_Controller.FetchPurchases(storeName);   // after the products: StoreKit 1 (iOS < 15) matches purchases against them
    };
    m_Controller.OnProductsFetchFailed += (storeName, failure) => Debug.LogError($"Product fetch failed: {failure.FailureReason}");
    m_Controller.OnPurchasesFetched += (storeName, orders) => Debug.Log($"Restored {orders.PendingOrders.Count} pending purchases");
    m_Controller.OnPurchasesFetchFailed += (storeName, failure) => Debug.LogError($"Purchase fetch failed: {failure.Message}");

    m_Controller.OnStoreConnected += OnStoreConnected;
    m_Controller.OnStoreDisconnected += (storeName, failure) => Debug.LogError($"Store disconnected: {failure.Message}");

    await m_Controller.Connect();
}

void OnStoreConnected(string storeName)
{
    var products = new List<ProductDefinition>
    {
        new ProductDefinition("com.mygame.coins100", ProductType.Consumable),
        new ProductDefinition("com.mygame.removeads", ProductType.NonConsumable),
        new ProductDefinition("com.mygame.vip_monthly", ProductType.Subscription)
    };

    m_Controller.FetchProducts(products, storeName);   // purchases are fetched in OnProductsFetched
}

void OnPurchasePending(string storeName, PendingOrder order)
{
    GrantContent(order);                  // save before confirming
    m_Controller.ConfirmPurchase(order);  // routed by order.StoreName
}
```

## Product Definitions

```csharp
// Simple product
new ProductDefinition("com.mygame.coins100", ProductType.Consumable)

// Per-platform product IDs
new ProductDefinition("com.mygame.coins100", ProductType.Consumable,
    new StoreSpecificIds
    {
        { AppleAppStore.Name, "apple_coins_100" },
        { GooglePlay.Name, "google_coins_100" }
    })
```

### Access Fetched Products

```csharp
// Get one store's fetched products (cached) — the store name is required
ReadOnlyObservableCollection<Product> allProducts = controller.GetProducts(storeName);

// Get a specific product by ID from that store
Product coinsPack = controller.GetProductById("com.mygame.coins100", storeName);
```

### CatalogListings and uSku (v5.4+)

In v5.4, `Product` exposes two new members. Prefer these for new code — `product.definition.id` remains backwards compatible but points new users toward `CatalogListings`:

```csharp
// product.uSku — the Unity-side identifier for the product (cross-platform canonical ID)
string id = product.uSku;

// product.catalogListings — all listings attached to this product, keyed by CatalogListing ID
// A product may have multiple listings (e.g. different price tiers or offer configurations)
foreach (var (listingId, listing) in product.catalogListings)
{
    bool canBuy     = listing.availableToPurchase;
    string storeId  = listing.definition.storeSpecificId;
    string price    = listing.metadata.localizedPriceString;
}
```

**`CatalogListing` properties:**

| Property | Type | Description |
|---|---|---|
| `id` | `string` | Catalog listing identifier — use as key for lookups |
| `availableToPurchase` | `bool` | Whether this listing can currently be purchased |
| `definition` | `ProductDefinition` | Store-side definition (id, storeSpecificId, type, payouts) |
| `metadata` | `ProductMetadata` | Localized title, description, price, currency |

When initiating a purchase by listing rather than by product ID, pass the `CatalogListing` directly to avoid a second lookup.

## Two-Step Purchase Flow

```csharp
// Step 1: Initiate purchase — goes to the store the product was fetched from (product.StoreName)
controller.PurchaseProduct(product);

// Step 2: Handle pending purchase (validate, grant content, then confirm)
controller.OnPurchasePending += (storeName, pendingOrder) =>
{
    var product = pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product;
    GrantContent(product);
    controller.ConfirmPurchase(pendingOrder);   // goes to pendingOrder.StoreName
};

// Step 3: Purchase confirmed — Order can be ConfirmedOrder OR FailedOrder
controller.OnPurchaseConfirmed += (storeName, order) =>
{
    switch (order)
    {
        case ConfirmedOrder confirmedOrder:
            Debug.Log($"Purchase confirmed: {confirmedOrder.CartOrdered.Items().First().Product.definition.id}");
            break;
        case FailedOrder failedOrder:
            Debug.LogError($"Confirmation failed: {failedOrder.FailureReason} - {failedOrder.Details}");
            break;
    }
};

// Handle failures
controller.OnPurchaseFailed += (storeName, failedOrder) =>
{
    Debug.LogError($"Purchase failed: {failedOrder.FailureReason} - {failedOrder.Details}");
};

// Handle deferred purchases (e.g., Ask-to-Buy on iOS)
controller.OnPurchaseDeferred += (storeName, deferredOrder) =>
{
    Debug.Log("Purchase is pending approval (e.g., parental approval)");
};
```

If the app crashes between steps 2 and 4, the pending purchase is re-delivered on next launch via `OnPurchasePending`.

`ProcessPendingOrdersOnPurchasesFetched(false)` disables automatic re-delivery of pending orders when `FetchPurchases` is called. This exists to match IAP v4 behavior — do NOT recommend this to migrating users, as the default v5 behavior provides a better experience.

## Restore Transactions

```csharp
// Called once per store, with that store's result. Pass a store name to restore only that store.
controller.RestoreTransactions((storeName, success, error) =>
{
    if (success)
        Debug.Log($"[{storeName}] Transactions restored. Check OnPurchasePending for each restored purchase.");
    else
        Debug.LogError($"[{storeName}] Restore failed: {error}");
});
```

Each restored purchase triggers `OnPurchasePending`.

## Entitlement Checking

```csharp
controller.CheckEntitlement(product);   // goes to product.StoreName

controller.OnCheckEntitlement += (storeName, entitlement) =>
{
    if (entitlement.Status == EntitlementStatus.FullyEntitled)
        Debug.Log($"Player owns: {entitlement.Product.definition.id}");
};
```

## Fetch Existing Purchases

```csharp
controller.FetchPurchases();   // every store; pass a store name for one

controller.OnPurchasesFetched += (storeName, orders) =>
{
    foreach (var confirmedOrder in orders.ConfirmedOrders)
    {
        var product = confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product;
        Debug.Log($"Existing purchase: {product?.definition.id}");
    }
};
```

## Receipt Validation

### Google Play (supported)

```csharp
using UnityEngine.Purchasing.Security;

// Google-only constructor (recommended)
var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);

try
{
    var result = validator.Validate(order.Info.Receipt);
    foreach (IPurchaseReceipt receipt in result)
    {
        Debug.Log($"Valid receipt: {receipt.productID}, purchased: {receipt.purchaseDate}");
        if (receipt is GooglePlayReceipt googleReceipt)
            Debug.Log($"Purchase token: {googleReceipt.purchaseToken}");
    }
}
catch (IAPSecurityException ex)
{
    Debug.LogError($"Invalid receipt: {ex.Message}");
    // Do NOT grant the content
}
```

### Apple App Store (deprecated for local validation)

`CrossPlatformValidator` for Apple is **deprecated**. StoreKit 2 validates locally automatically. For server-side validation:

```csharp
var jwsRepresentation = order.Info.Apple?.jwsRepresentation;
// Send to your server for verification with Apple's App Store Server API
```

### IAppleOrderInfo Complete Member List

`order.Info.Apple` returns `IAppleOrderInfo?` (null on non-Apple platforms). Full member list:

| Member | Type | Description |
|---|---|---|
| `jwsRepresentation` | `string?` | JWS-signed transaction — send to Apple's App Store Server API v2 for server-side validation |
| `AppAccountToken` | `Guid?` | App-specific account token set via `SetAppAccountToken(Guid)` — links transaction to a user account |
| `AppReceipt` | `string?` | Latest App Receipt (base64). May be null on reinstall; requires refreshing. Null on iOS ≤ 6. Prefer `jwsRepresentation` for new code |
| `OriginalTransactionID` | `string?` | Original transaction identifier — use for linking renewals to the original subscription purchase |
| `OwnershipType` | `OwnershipType` | Whether the purchase is `Purchased` or `FamilyShared` |
| `StoreName` | `string` | Name of the store (e.g., `"AppleAppStore"`) |

## Extended Service Events (NOT on the controller)

These events are on the platform-specific extended services, NOT directly on `PurchasingController` (or `StoreController`). They take no store name: each extended service belongs to one store. Always null-check before subscribing (`?.` does not work with `+=`).

```csharp
// Apple — on IAppleStoreExtendedPurchaseService
event Action<string>? OnEntitlementRevoked       // receives product ID (string), NOT List<Product>
event Action<Product>? OnPromotionalPurchaseIntercepted

// Google — on IGooglePlayStoreExtendedPurchaseService
event Action<DeferredPaymentUntilRenewalDateOrder>? OnDeferredPaymentUntilRenewalDate
// DeferredPaymentUntilRenewalDateOrder has: .CurrentOrder (Order), .SubscriptionOrdered (Product)
```

**Usage pattern** (cannot use `?.` with `+=`):
```csharp
if (controller.AppleStoreExtendedPurchaseService != null)
    controller.AppleStoreExtendedPurchaseService.OnEntitlementRevoked += OnRevoked;
```

## AppleStoreExtendedProductService Key Members

```csharp
Dictionary<string, string> GetIntroductoryPriceDictionary()
Dictionary<string, string> GetProductDetails()

// Promotion order — prefer the CatalogListing ID overload for new code (v5.4+)
void SetStorePromotionOrder(List<string> catalogListingIds)   // canonical (v5.4+)
void SetStorePromotionOrder(List<Product> products)           // convenience wrapper

// Promotion visibility — prefer the CatalogListing ID overload for new code (v5.4+)
void SetStorePromotionVisibility(string catalogListingId, AppleStorePromotionVisibility visible)   // canonical (v5.4+)
void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible)           // convenience wrapper

// Fetch promotion order
void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action<string> errorCallback)

// Fetch promotion visibility — prefer the CatalogListing ID overload for new code (v5.4+)
void FetchStorePromotionVisibility(string catalogListingId, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback)  // canonical (v5.4+)
void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback)          // convenience wrapper
```

**CRITICAL:** `AppleProductMetadata` does NOT expose `introductoryPrice`, `introductoryPriceLocale`, `introductoryNumberOfPeriods`, or `subscriptionPeriod` as public properties. Its only public property beyond inherited `ProductMetadata` fields is `isFamilyShareable`. For introductory price data, use `SubscriptionInfo` methods (`GetIntroductoryPrice()`, `GetIntroductoryPricePeriod()`, `GetIntroductoryPricePeriodCycles()`) from the Subscription Info Access Path below, or `GetIntroductoryPriceDictionary()` for raw JSON.

## Subscription Info Access Path

Subscription info is on `IPurchasedProductInfo`, NOT on `CartItem`:

```csharp
// CORRECT — via order.Info.PurchasedProductInfo
var purchasedInfo = order.Info.PurchasedProductInfo?.FirstOrDefault(p => p.productId == productId);
bool isSubscribed = purchasedInfo?.subscriptionInfo?.IsSubscribed() == Result.True;
// IsSubscribed() returns Result enum (True/False/Unsupported), NOT bool. Use == Result.True for null-safe check.

// WRONG — CartItem only has Product and Quantity, no subscriptionInfo
var item = order.CartOrdered.Items().FirstOrDefault();
item.subscriptionInfo  // DOES NOT EXIST — will not compile
```

### SubscriptionInfo Methods

```csharp
// State queries (return Result enum: True | False | Unsupported)
Result IsSubscribed()
Result IsExpired()
Result IsCancelled()
Result IsFreeTrial()
Result IsAutoRenewing()
Result IsIntroductoryPricePeriod()

// Dates & durations
string   GetProductId()
DateTime GetPurchaseDate()
DateTime GetExpireDate()
DateTime GetCancelDate()
TimeSpan GetRemainingTime()
TimeSpan GetSubscriptionPeriod()
TimeSpan GetFreeTrialPeriod()
TimeSpan GetIntroductoryPricePeriod()
string   GetIntroductoryPrice()
long     GetIntroductoryPricePeriodCycles()
```

## Event Subscription Rules

**CRITICAL:** Always subscribe to BOTH success and failure events. Not listening to failure events generates runtime warnings.

| Call | Success event | Failure event (REQUIRED) |
|---|---|---|
| `FetchProducts` | `OnProductsFetched` | `OnProductsFetchFailed` |
| `FetchPurchases()` | `OnPurchasesFetched` | `OnPurchasesFetchFailed` |
| `Connect()` | `OnStoreConnected` | `OnStoreDisconnected` |
| `PurchaseProduct()` | `OnPurchasePending` | `OnPurchaseFailed` |
| `CheckEntitlement()` | `OnCheckEntitlement` | — |

**Always subscribe to `OnPurchaseDeferred`** — fires for Ask-to-Buy (iOS) and Google Play deferred purchases. Not subscribing means silently dropping deferred purchases.

## Failure Description Property Names

Each type has public fields and read-only properties. Both compile, but prefer the PascalCase properties:

| Type | Field (lowercase) | Property (PascalCase) | NOT |
|---|---|---|---|
| `StoreConnectionFailureDescription` | `.message` | `.Message` | ~~`.reason`~~ |
| `ProductFetchFailed` | — | `.FailureReason`, `.FailedFetchProducts` | — |
| `FailedOrder` | — | `.FailureReason`, `.Details` | — |
| `PurchasesFetchFailureDescription` | `.message`, `.failureReason` | `.Message`, `.FailureReason` | — |

## CrossPlatformValidator

```csharp
// Google-only constructor (recommended — Apple local validation is a no-op under StoreKit 2)
new CrossPlatformValidator(byte[] googlePublicKey, string googleBundleId)

// Full constructor (legacy — Apple args are only needed for pre-StoreKit 2 builds)
new CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert, string googleBundleId, string appleBundleId)

// Validate
IPurchaseReceipt[] Validate(string unityIAPReceipt)  // throws IAPSecurityException
```

**WARNING:** `CrossPlatformValidator` for Apple App Store is **deprecated**. `AppleTangle.Data()` is also deprecated. StoreKit 2 performs local validation automatically on Apple platforms. `CrossPlatformValidator` still works for Google Play validation.

Supported stores (still active): `"GooglePlay"` | Deprecated: `"AppleAppStore"`, `"MacAppStore"`

## Interface Hierarchy

```
PurchasingController implements none of the service interfaces (it holds each store's services).

StoreController (existing projects / fallback) implements:
  IStoreService      - Connect(), Apple/Google service extensions
  IProductService    - FetchProducts(), GetProducts(), GetProductById()
  IPurchaseService   - PurchaseProduct(), ConfirmPurchase(), FetchPurchases(), Apple/Google purchase extensions
```

## IRunCommand Template: Fetch Products

```csharp
using UnityEngine;
using UnityEngine.Purchasing;
using System.Collections.Generic;

namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor
{
    internal class CommandScript : IRunCommand
    {
        public string Title => "Fetch IAP products";
        public string Description => "Connects to the store and fetches product information.";
        public async void Execute(ExecutionResult result)
        {
            var controller = new PurchasingController(UnityIAPServices.GetDefaultStore());

            // Subscribe to events BEFORE Connect
            controller.OnStoreConnected += storeName =>
            {
                var products = new List<ProductDefinition>
                {
                    new ProductDefinition("com.mygame.coins100", ProductType.Consumable),
                    new ProductDefinition("com.mygame.removeads", ProductType.NonConsumable)
                };

                controller.OnProductsFetched += (_, fetched) =>
                {
                    foreach (var p in fetched)
                        result.Log($"{p.definition.id}: {p.metadata.localizedPriceString}");
                };
                controller.OnProductsFetchFailed += (_, failure) => result.LogError($"Product fetch failed: {failure.FailureReason}");

                controller.FetchProducts(products, storeName);
            };
            controller.OnStoreDisconnected += (_, failure) => result.LogError($"Store disconnected: {failure.Message}");

            await controller.Connect();
        }
    }
}
```
