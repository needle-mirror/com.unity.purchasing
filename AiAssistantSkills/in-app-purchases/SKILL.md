---
name: implement-in-app-purchases
description: Implement, configure, and debug Unity In-App Purchases (IAP) — store connection, product catalog, consumable/non-consumable/subscription purchases, two-step pending-confirm flow, receipt validation, entitlement checking, restore transactions, Apple extensions (promotional purchases, Ask-to-Buy, code redemption), and Google Play extensions (subscription upgrade/downgrade), D2C Capabilities(direct to customer), 3rd party payment provider (Stripe/Coda) via Unity IAP/Unity Cloud. Use when the user needs to add, modify, debug, or migrate from native Android/iOS billing, 3rd party packages(RevenueCat/Adapty/Essential Kit/Unipay supported) to IAP. Triggers on microtransactions (MTX), monetization, real-money purchases, store purchases, buying items, support D2C, purchase via Stripe/Coda, migrate from native billing(Google's BillingClient or Apple's StoreKit/SKPaymentQueue/SKProduct)/RevenueCat/Adapty/EssentialKit/Unipay.
---

# Unity In-App Purchasing

Namespace: `UnityEngine.Purchasing` | Security: `UnityEngine.Purchasing.Security`
Package: `com.unity.purchasing`

**Unity IAP has its own initialization path** via `new PurchasingController(UnityIAPServices.GetDefaultStore())` → `controller.Connect()`. It does not require `UnityServices.InitializeAsync()`, but they can coexist if your project uses other UGS services (IAP D2C Capabilities do need it). If Analytics is present and `InitializeAsync()` is called, IAP will automatically send transaction events.

**Always write code against `PurchasingController`** (v5.5+) — one object for every store the app sells through: the platform store, a D2C payment provider, or both. Single-store apps use it too. Every event takes the store name first. See [references/purchasing-controller.md](references/purchasing-controller.md).

- **Package without `PurchasingController`** (5.4.x and earlier — check the installed package contains the type): recommend upgrading `com.unity.purchasing`. Fall back to `StoreController` only if the user can't or won't upgrade.
- **Project already on `StoreController`:** offer to migrate the whole project to `PurchasingController` ([migration guide](references/purchasing-controller.md#migrating-from-storecontroller)) and do it only if the user agrees — never migrate unasked. If the user declines, stay on `StoreController` throughout, new code included: never add a `PurchasingController` next to existing `StoreController`s.

## Before You Start

**Always read [references/pre-check.md](references/pre-check.md) first.** It scans the project for third-party IAP packages, native Google Billing, and existing Unity IAP versions, then routes to the correct path. Do not read any other reference file or make any changes until routing is resolved.

## Detailed References

- **Project scan and path routing (read first):** See [references/pre-check.md](references/pre-check.md)
- **API signatures & code examples:** See [references/api-notes.md](references/api-notes.md)
- **`PurchasingController` (the controller for all new code, v5.5+) and migrating from `StoreController`:** See [references/purchasing-controller.md](references/purchasing-controller.md)
- **Platform extensions (Apple, Google):** See [references/platform-notes.md](references/platform-notes.md)
- **Editing `IAPProductCatalog.json` (schema, decimal serialization, refresh):** See [references/codeless-catalog.md](references/codeless-catalog.md)
- **v4 → v5 migration:** See [references/migration-v4-to-v5.md](references/migration-v4-to-v5.md)
- **Convert native Google BillingClient to Unity IAP 5:** See [references/path-convert-native-google-billing.md](references/path-convert-native-google-billing.md)
- **Convert native iOS StoreKit plugin to Unity IAP 5:** See [references/path-convert-native-storekit.md](references/path-convert-native-storekit.md)
- **Convert Essential Kit billing to Unity IAP 5:** See [references/convert-essentialkit.md](references/convert-essentialkit.md)
- **UniPay (FLOBUK) — assessment and guidance:** See [references/convert-unipay.md](references/convert-unipay.md)
- **RevenueCat — conversion assessment and guidance:** See [references/convert-revenuecat.md](references/convert-revenuecat.md)
- **Adapty — conversion assessment and guidance:** See [references/convert-adapty.md](references/convert-adapty.md)
- **Add Unity IAP 5 to a project with no existing IAP:** See [references/path-add-iap-to-new-project.md](references/path-add-iap-to-new-project.md)
- **Implement IAP D2C Capabilities (third-party payment provider — Stripe/Coda, requires v5.4+):** See [references/path-implement-iap-d2c.md](references/path-implement-iap-d2c.md)

Read reference files on demand — only when you need specific API signatures, platform extension details, or migration mappings.

## Initialization Flow

1. Create `new PurchasingController(UnityIAPServices.GetDefaultStore())` — add `PaymentProvider.Name` for IAP D2C Capabilities, after Unity Services sign-in
2. Subscribe to **all** required events (see Required Event Subscriptions below) **before** calling `Connect()` — handlers take the store name first: `(storeName, order) => ...`
3. `await controller.Connect()` to connect every store
4. On `OnStoreConnected(storeName)`, call `controller.FetchProducts(productDefinitions, storeName)` to load that store's catalog
5. On `OnProductsFetched(storeName, products)`, that store's products are ready for display and purchase

Use `Awake()` for initialization — ensures IAP is ready before other `Start()` methods.

Product types: `ProductType.Consumable`, `ProductType.NonConsumable`, `ProductType.Subscription`.

## Fetching Products

Define products as `List<ProductDefinition>` and pass them to `controller.FetchProducts(definitions, storeName)` — **one call per store**, since a definition carries a single store-specific ID. Use `StoreSpecificIds` when product IDs differ across Apple/Google stores. For complex catalogs, use `CatalogProvider` / `RemoteCatalogProvider`; one provider serves every store: `foreach (var store in controller.StoreNames) catalogProvider.FetchProducts(defs => controller.FetchProductsWithNoRetries(defs, store), store);`

Products managed in Unity Cloud come from the Remote Catalog (`RemoteCatalogProvider.FetchRemoteCatalog()`). Fetching it needs a signed-in player: call `UnityServices.InitializeAsync()` and sign in first, even when only native stores are used, and don't show a store before then. Without sign-in the fetch fails with `Failed to fetch full remote catalog; paging stopped after cursor ...`; that error usually means the player isn't signed in.

| Method | Behavior |
|---|---|
| `GetProducts(storeName)` | Returns that store's **cached** product list (synchronous, stale if `FetchProducts` not called) |
| `FetchProducts(defs, storeName)` | Queries that **store** for fresh pricing/availability and updates its cache — the store name is required; call it once per store |
| `GetProductById(id, storeName)` | Returns a single cached product by ID from that store |

These are NOT interchangeable. Always call `FetchProducts` first before relying on `GetProducts`.

## Two-Step Purchase Flow

IAP v5 uses a mandatory two-step flow: **Pending → Confirm**.

1. `controller.PurchaseProduct(product)` — initiates the purchase in the store the product was fetched from (`product.StoreName`)
2. `OnPurchasePending(storeName, pendingOrder)` fires — you receive a `PendingOrder`
3. Validate the receipt, grant content to the player
4. `controller.ConfirmPurchase(pendingOrder)` — finalizes the transaction in `pendingOrder.StoreName`
5. `OnPurchaseConfirmed(storeName, order)` fires — receives `Order` base type; pattern-match `ConfirmedOrder` (success) vs `FailedOrder` (confirmation failed)

**You MUST call `ConfirmPurchase(pendingOrder)` after granting content.** Unconfirmed purchases are re-delivered on next app launch to prevent lost purchases.

**De-duplication:** `OnPurchasePending` may fire multiple times for the same purchase (e.g., app restart before confirmation). Always check if content was already granted.

**Consumables:** Confirmed consumable purchases are NOT returned by `FetchPurchases`. Track consumable grants yourself (e.g., in Cloud Save).

**Deferred purchases:** `OnPurchaseDeferred` fires for Ask-to-Buy (iOS) and Google Play deferred purchases. Do NOT grant content — wait for `OnPurchasePending` when approved.

## Restore Transactions

`controller.RestoreTransactions((storeName, success, error) => ...)` re-delivers non-consumable and subscription purchases; the callback runs once per store. Each restored purchase triggers `OnPurchasePending`.

Required on iOS for Apple App Store compliance — add a "Restore Purchases" button.

Apple non-renewable subscriptions **cannot be restored** via `RestoreTransactions`. Track these server-side.

## Receipt Validation

| Platform | Approach |
|---|---|
| **Google Play** | `CrossPlatformValidator` with `GooglePlayTangle.Data()` — local validation supported |
| **Apple (StoreKit 2)** | Local validation is a **no-op**. Use `order.Info.Apple?.jwsRepresentation` for server-side validation |

Generate tangle data via **Services > In-App Purchasing > Receipt Validation Obfuscator** in the Unity Editor.

## Entitlement Checking

Use when you don't have the `Order` and want to know the status of a specific product (replaces v4's `product.hasReceipt`). If you already have the `Order`, check its type instead: `PendingOrder` maps to `EntitledUntilConsumed` (consumables) or `EntitledButNotFinished` (non-consumables/subscriptions), `ConfirmedOrder` maps to `FullyEntitled`.

Call `controller.CheckEntitlement(product)` and handle `controller.OnCheckEntitlement += (storeName, entitlement) => ...`. Check `entitlement.Status == EntitlementStatus.FullyEntitled`.

`EntitlementStatus` values: `FullyEntitled`, `EntitledUntilConsumed`, `EntitledButNotFinished`, `NotEntitled`, `Unknown`.

## Fetch Existing Purchases

`controller.FetchPurchases()` retrieves all current purchases from every store (pass a store name for one). Useful at app startup.

| Method | Behavior |
|---|---|
| `GetPurchases(storeName)` | Returns that store's **cached** purchase list |
| `FetchPurchases(storeName?)` | Queries the **store(s)** for current purchases and **overwrites** each store's cached list |

`FetchPurchases()` replaces a store's entire cached list on each call. Only non-consumables and subscriptions are re-fetched — confirmed consumables are not returned (see Two-Step Purchase Flow above).

## Subscription Info

Subscription info is on `IPurchasedProductInfo`, accessed via `order.Info.PurchasedProductInfo` — **NOT on `CartItem`** (`CartItem` only has `Product` and `Quantity`).

`IsSubscribed()` returns `Result` enum (`True`/`False`/`Unsupported`), NOT `bool`. Use `== Result.True` for null-safe comparison.

## Required Event Subscriptions

**Always subscribe to BOTH success and failure events.** Not subscribing to failure events generates runtime warnings. On `PurchasingController` every event takes the store name first — one subscription covers every store.

| Call | Success Event | Failure Event (REQUIRED) |
|---|---|---|
| `FetchProducts` | `OnProductsFetched` | `OnProductsFetchFailed` |
| `FetchPurchases()` | `OnPurchasesFetched` | `OnPurchasesFetchFailed` |
| `Connect()` | `OnStoreConnected` | `OnStoreDisconnected` |
| `PurchaseProduct()` | `OnPurchasePending` | `OnPurchaseFailed` |
| `CheckEntitlement()` | `OnCheckEntitlement` | — |

**Always subscribe to `OnPurchaseDeferred`** — fires for Ask-to-Buy (iOS) and Google Play deferred purchases. Not subscribing silently drops deferred purchases.

Subscribe to events **BEFORE** calling `Connect()` — pending purchases from a previous session may fire immediately.

## Failure Description Property Names

These property names are NOT interchangeable — using the wrong one causes CS1061:

| Type | Field | Property | NOT |
|---|---|---|---|
| `StoreConnectionFailureDescription` | `.message` | `.Message` | ~~`.reason`~~ |
| `ProductFetchFailed` | — | `.FailureReason`, `.FailedFetchProducts` | ~~`.Message`~~ |
| `FailedOrder` | — | `.FailureReason`, `.Details` | — |
| `PurchasesFetchFailureDescription` | `.message`, `.failureReason` | `.Message`, `.FailureReason` | — |

## Validation

After writing code that uses this package:
1. Verify the project compiles without errors.
2. Confirm all API calls match the `PurchasingController` signatures in [purchasing-controller.md](references/purchasing-controller.md) — do NOT use v4 legacy patterns (`IStoreListener`, `UnityPurchasing.Initialize`, `ConfigurationBuilder`), and do NOT write new code against `StoreController` unless the user declined to upgrade a package without `PurchasingController`, or declined migrating a project already on `StoreController`.
3. Check the "Anti-Hallucination: Common v5 Mistakes" table in api-notes.md — do NOT use `OnStoreConnectionFailed` (use `OnStoreDisconnected`), do NOT pass callbacks to `FetchProducts`/`FetchPurchases` (use events), do NOT use `product.receipt` (use `order.Info.Receipt`).
4. Check that all required events are subscribed **before** calling `Connect()` (see Required Event Subscriptions table above).
5. Verify the two-step purchase flow: `OnPurchasePending` → grant content → `ConfirmPurchase(pendingOrder)`.
6. Confirm both success and failure events are subscribed for every async operation (`OnProductsFetched`/`OnProductsFetchFailed`, `OnStoreConnected`/`OnStoreDisconnected`, etc.).
7. If handling subscriptions, verify `IsSubscribed()` is compared with `== Result.True`, not cast to `bool`.
8. If updated files coexist with legacy versions in the same project, use a unique namespace (e.g., add `.Updated` suffix) to avoid CS0101/CS0111 compilation errors.
9. If the project subscribes to `OnAuthAccountChanged` (v5.4+): verify the handler re-fetches that store's products and purchases from scratch — Unity IAP clears both caches before raising this event. Do not read `GetProducts(storeName)` or `GetPurchases(storeName)` inside the handler.
10. Run the Validation list in [purchasing-controller.md](references/purchasing-controller.md) — store name first in every handler, store name on every getter and every `FetchProducts` call.
