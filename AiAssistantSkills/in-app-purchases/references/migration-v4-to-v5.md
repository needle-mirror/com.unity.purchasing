# Unity IAP v4 to v5 Migration Guide

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Overview](#overview)
- [Migration Mapping Table](#migration-mapping-table)
- [Key Breaking Changes](#key-breaking-changes)
- [Migration Anti-Patterns](#migration-anti-patterns)
- [Minimal v5 Example](#minimal-v5-example)

## Trigger Phrases

- "Migrate from Unity IAP v4 to v5"
- "Upgrade Unity IAP to v5"
- "Update IAP from v4"
- "Migrate IStoreListener to StoreController" / "to PurchasingController"
- "Replace ConfigurationBuilder with v5 IAP"
- "Update UnityPurchasing.Initialize to v5"

## Overview

Unity IAP v5 replaces the listener-based pattern (`IStoreListener`) with an event-driven pattern. **Migrate IAP 4 code straight to `PurchasingController`** (v5.5+): every event takes the store name first (`(storeName, order) => ...`), getters take a store name, and purchases go to the store their product or order belongs to. This is a breaking change that requires updating all IAP code.

In the tables below, `m_Controller` is a `PurchasingController` created with `new PurchasingController(UnityIAPServices.GetDefaultStore())`, and `storeName` is the store being handled (the event's first parameter, or `UnityIAPServices.GetDefaultStore()`). If the installed package has no `PurchasingController` (5.0–5.4), recommend upgrading first; only if the user can't or won't, target `StoreController` instead (same member names, handlers without the store name, getters without it — see [api-notes.md](api-notes.md#storecontroller-existing-projects-and-fallback)).

## Migration Mapping Table

### Initialization

| v4 (Legacy) | v5 (Current) |
|---|---|
| `UnityPurchasing.Initialize(listener, builder)` | `new PurchasingController(UnityIAPServices.GetDefaultStore())` then `await m_Controller.Connect()` |
| `ConfigurationBuilder.Instance(StandardPurchasingModule.Instance())` | Use `CatalogProvider` for store-specific IDs/payouts, or pass `List<ProductDefinition>` directly to `FetchProducts` |
| `builder.AddProduct("id", ProductType.Consumable)` | `new ProductDefinition("id", ProductType.Consumable)` in a list |
| `IStoreListener.OnInitialized(controller, extensions)` | `m_Controller.OnStoreConnected` event — `storeName => ...` |
| `IStoreListener.OnInitializeFailed(error)` | `m_Controller.OnStoreDisconnected` event — `(storeName, failure) => ...` |

### Purchase Flow

| v4 (Legacy) | v5 (Current) |
|---|---|
| `controller.InitiatePurchase(product)` | `m_Controller.PurchaseProduct(product)` — goes to the store the product was fetched from; note: `Purchase()` no longer accepts a developer payload argument (removed in Google Billing v3) |
| `IStoreListener.ProcessPurchase(args)` for **new** purchases | `m_Controller.OnPurchasePending` — `(storeName, pendingOrder) => ...` — + `m_Controller.ConfirmPurchase(pendingOrder)` |
| `IStoreListener.ProcessPurchase(args)` for **restored** purchases | `m_Controller.OnPurchasesFetched` — use existing `ProcessPurchase` logic in both `OnPurchasePending` and `OnPurchasesFetched` |
| `IStoreListener.ProcessPurchase(args)` returning `PurchaseProcessingResult.Pending` | Just don't call `ConfirmPurchase` until ready — store the `PendingOrder` reference |
| `IStoreListener.OnPurchaseFailed(product, reason)` | `m_Controller.OnPurchaseFailed` event — `(storeName, failedOrder) => ...` (`FailedOrder` has `FailureReason` and `Details`) |
| `controller.ConfirmPendingPurchase(product)` | `m_Controller.ConfirmPurchase(pendingOrder)` — requires the `PendingOrder` object, not the `Product` |
| `product.hasReceipt` | **Preferred:** call `m_Controller.CheckEntitlement(product)` and handle `m_Controller.OnCheckEntitlement` — check `entitlement.Status == EntitlementStatus.FullyEntitled`. **Alternative:** track ownership with a `bool` flag updated in `OnPurchasePending` (new) and `OnPurchasesFetched` (restored). `CheckEntitlement` is the recommended v5 pattern for non-consumables and subscriptions. |

### Configuration / Products

| v4 (Legacy) | v5 (Current) |
|---|---|
| `ConfigurationBuilder` with `AddProduct()` | `CatalogProvider` with `AddProduct()`, or a `List<ProductDefinition>` passed to `m_Controller.FetchProducts(definitions, storeName)` |
| `builder.AddProduct("id", type, new IDs { { "store_id", store } })` | `catalogProvider.AddProduct("id", type, new StoreSpecificIds { { "store_id", store } })` — or `new ProductDefinition("id", "store_id", type)` for a single store ID |
| `controller.products.WithID("id")` | `m_Controller.GetProductById("id", storeName)` |
| `controller.products.all` | `m_Controller.GetProducts(storeName)` |

### Restore Transactions

| v4 (Legacy) | v5 (Current) |
|---|---|
| `extensions.GetExtension<IAppleExtensions>().RestoreTransactions(callback)` | `m_Controller.RestoreTransactions((storeName, success, error) => ...)` — called once per store |
| Manual restore call required | Confirmed purchases are **automatically restored** when you call `m_Controller.FetchPurchases()` or `m_Controller.CheckEntitlement()` — `RestoreTransactions` is only needed for explicit user-triggered restore buttons (required on iOS) |

### Receipt Validation

| v4 (Legacy) | v5 (Current) |
|---|---|
| `CrossPlatformValidator` with Apple + Google | `CrossPlatformValidator` still works, but under StoreKit 2 Apple validation returns an empty array (no-op). **Recommended:** Use the Google-only constructor: `CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier)`. The legacy 4-arg constructor still works. Single-bundle-ID constructors are `[Obsolete]` |
| `AppleTangle.Data()` for Apple validation | Under StoreKit 2, local Apple receipt validation is a no-op. Use `order.Info.Apple?.jwsRepresentation` for server-side Apple validation instead |
| Manual receipt parsing | Use `order.Info.Apple?.jwsRepresentation` for server-side validation |
| `product.receipt` | `order.Info.Receipt` — receipt is now accessed from the `Order`/`PendingOrder`, not the `Product` |

### Platform Extensions

| v4 (Legacy) | v5 (Current) |
|---|---|
| `extensions.GetExtension<IAppleExtensions>()` | `m_Controller.AppleStoreExtendedService` / `m_Controller.AppleStoreExtendedPurchaseService` |
| `extensions.GetExtension<IGooglePlayStoreExtensions>()` | `m_Controller.GooglePlayStoreExtendedService` / `m_Controller.GooglePlayStoreExtendedPurchaseService` |
| `builder.Configure<IAppleConfiguration>().SetApplePromotionalPurchaseInterceptorCallback(cb)` | `m_Controller.AppleStoreExtendedPurchaseService.OnPromotionalPurchaseIntercepted += cb` (event on `IAppleStoreExtendedPurchaseService`, null-check required; callback signature: `Action<Product>`) |
| `appleExtensions.ContinuePromotionalPurchases()` | `m_Controller.AppleStoreExtendedPurchaseService?.ContinuePromotionalPurchases()` |
| `appleExtensions.RegisterPurchaseDeferredListener(cb)` | `m_Controller.OnPurchaseDeferred += (storeName, deferredOrder) => ...` (event on the controller) |
| `appleExtensions.simulateAskToBuy` | `m_Controller.AppleStoreExtendedPurchaseService?.simulateAskToBuy` (null-check required) |
| `appleExtensions.PresentCodeRedemptionSheet()` | `m_Controller.AppleStoreExtendedPurchaseService?.PresentCodeRedemptionSheet()` |
| `appleExtensions.RestoreTransactions(cb)` | `m_Controller.RestoreTransactions((storeName, success, error) => ...)` |
| `appleExtensions.SetApplicationUsername(hashedString)` | `m_Controller.AppleStoreExtendedService?.SetAppAccountToken(Guid)` — must be called **after** `Connect()`; accepts a `Guid` (not a hash) identifying the user account in your system |
| `builder.Configure<IAppleConfiguration>().SetEntitlementsRevokedListener(cb)` | `m_Controller.AppleStoreExtendedPurchaseService.OnEntitlementRevoked += cb` (event on `IAppleStoreExtendedPurchaseService`, null-check required; callback signature: `Action<string>` — receives a single product ID, NOT `List<Product>`) |
| `appleExtensions.GetTransactionReceiptForProduct(product)` | `order.Info.Receipt` inside `OnPurchasePending` — per-transaction receipt is now on the order |
| `builder.Configure<IAppleConfiguration>().canMakePayments` | `m_Controller.AppleStoreExtendedService?.canMakePayments` — must be checked **after** `Connect()`, not before initialization |
| `appleExtensions.GetIntroductoryPriceDictionary()` | `m_Controller.AppleStoreExtendedProductService?.GetIntroductoryPriceDictionary()` — returns `Dictionary<string, string>` mapping product store-specific IDs to JSON with intro offer details. **NOT removed**, just moved to the product extension service. `AppleProductMetadata` does NOT expose `introductoryPrice`, `introductoryPriceLocale`, `introductoryNumberOfPeriods`, or `subscriptionPeriod` — those fields do not exist on that class. For introductory price data, use `SubscriptionInfo` methods (`GetIntroductoryPrice()`, `GetIntroductoryPricePeriod()`, `GetIntroductoryPricePeriodCycles()`). |
| `appleExtensions.GetProductDetails()` | `m_Controller.AppleStoreExtendedProductService?.GetProductDetails()` — returns `Dictionary<string, string>`. **NOT removed**, just moved to the product extension service. Basic metadata (title, description, price) is on `product.metadata` directly. |
| `appleExtensions.SetStorePromotionOrder(products)` | `m_Controller.AppleStoreExtendedProductService?.SetStorePromotionOrder(products)` — moved to `IAppleStoreExtendedProductService` (null-check required) |
| `appleExtensions.SetStorePromotionVisibility(product, visibility)` | `m_Controller.AppleStoreExtendedProductService?.SetStorePromotionVisibility(product, visibility)` — moved to `IAppleStoreExtendedProductService` (null-check required) |
| `appleExtensions.FetchStorePromotionOrder(success, failure)` | `m_Controller.AppleStoreExtendedProductService?.FetchStorePromotionOrder(successCb, errorCb)` — moved to `IAppleStoreExtendedProductService` (null-check required; callback signatures: `Action<List<Product>>`, `Action<string>`) |
| `appleExtensions.FetchStorePromotionVisibility(product, success, failure)` | `m_Controller.AppleStoreExtendedProductService?.FetchStorePromotionVisibility(product, successCb, errorCb)` — moved to `IAppleStoreExtendedProductService` (null-check required; callback signatures: `Action<string, AppleStorePromotionVisibility>`, `Action<string>`) |
| `googlePlayConfig.SetDeferredPurchaseListener(cb)` | `m_Controller.OnPurchaseDeferred += (storeName, deferredOrder) => ...` (event on the controller) |
| `googlePlayConfig.SetDeferredProrationUpgradeDowngradeSubscriptionListener(cb)` | `m_Controller.GooglePlayStoreExtendedPurchaseService.OnDeferredPaymentUntilRenewalDate += cb` (event on `IGooglePlayStoreExtendedPurchaseService`, null-check required; callback signature: `Action<DeferredPaymentUntilRenewalDateOrder>` — NOT `Action<Product>`. Access the product via `deferredOrder.SubscriptionOrdered`) |
| `googlePlayExtensions.UpgradeDowngradeSubscription(currentId, newId, mode)` | `m_Controller.GooglePlayStoreExtendedPurchaseService?.UpgradeDowngradeSubscription(currentOrder, newProduct, desiredReplacementMode)` — takes `Order` and `Product` objects instead of string IDs; third parameter is `GooglePlayReplacementMode` (not the deprecated `GooglePlayProrationMode`); call after `Connect()` |
| `googlePlayExtensions.IsPurchasedProductDeferred(product)` | `m_Controller.GooglePlayStoreExtendedPurchaseService?.IsOrderDeferred(order)` — renamed and takes `Order` instead of `Product`; marked `[Obsolete]`. Prefer tracking deferred state via `m_Controller.OnPurchaseDeferred` / `m_Controller.OnPurchasePending` events instead |
| `googlePlayExtensions.RestoreTransactions(cb)` | `m_Controller.RestoreTransactions((storeName, success, error) => ...)` — on the controller directly |
| `googlePlayConfig.SetObfuscatedAccountId(id)` | `m_Controller.GooglePlayStoreExtendedService?.SetObfuscatedAccountId(id)` — moved from config-time to **post-`Connect()`** |
| `googlePlayConfig.SetObfuscatedProfileId(id)` | `m_Controller.GooglePlayStoreExtendedService?.SetObfuscatedProfileId(id)` — moved from config-time to **post-`Connect()`** |

### Subscription Info

| v4 (Legacy) | v5 (Current) |
|---|---|
| `new SubscriptionManager(product, introJson).getSubscriptionInfo()` | `order.Info.PurchasedProductInfo.FirstOrDefault(p => p.productId == productId)?.subscriptionInfo` — accessed via `IPurchasedProductInfo` on the order's info, NOT on `CartItem`. `CartItem` only has `Product` and `Quantity`. |
| `subscriptionInfo.isSubscribed() == Result.True` | `purchasedProductInfo?.subscriptionInfo?.IsSubscribed() == Result.True` — method is now PascalCase but still returns `Result` enum (True/False/Unsupported), NOT `bool`. Use `== Result.True` for null-safe comparison (returns `false` if the chain is null). Do NOT use `?? false` — `Result?` and `bool` are incompatible types. |
| `product.receipt == null` to check ownership | **Preferred:** use `m_Controller.CheckEntitlement(product)` + `m_Controller.OnCheckEntitlement` — the recommended v5 ownership check. **Alternative:** track ownership with a `bool` flag updated in `OnPurchasePending` (new purchase) and `OnPurchasesFetched` (restored purchases). |
| `product.metadata.GetAppleProductMetadata()?.isFamilyShareable` | Still valid — `GetAppleProductMetadata()` extension method on `ProductMetadata` is unchanged |

### Codeless IAP

| v4 (Legacy) | v5 (Current) |
|---|---|
| `CodelessIAPStoreListener.Instance` | Codeless still works but uses v5 under the hood |
| `CodelessIAPStoreListener.initializationComplete` | `CodelessIAPStoreListener.IsInitialized()` |

## Key Breaking Changes

1. **`IStoreListener` is deprecated**: Replace the interface implementation with event subscriptions on `PurchasingController` — each handler takes the store name first. The interface still compiles but is `[Obsolete]`.
2. **Two-step purchase flow is mandatory**: v5 always uses pending → confirm. There is no equivalent of returning `PurchaseProcessingResult.Complete` from `ProcessPurchase`.
3. **`ConfigurationBuilder` is deprecated**: Use `CatalogProvider` for store-specific IDs and payouts, or pass `List<ProductDefinition>` directly to `FetchProducts`. The class still compiles but is `[Obsolete]`.
4. **Apple local receipt validation is a no-op under StoreKit 2**: `CrossPlatformValidator` itself is not deprecated, but Apple validation silently returns an empty array under StoreKit 2. **Recommended:** use the Google-only constructor `CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier)`. Single-bundle-ID constructors are `[Obsolete]`. For server-side Apple validation, use `order.Info.Apple?.jwsRepresentation`.
5. **Extensions are direct properties**: No more `GetExtension<T>()` pattern. Access via `m_Controller.AppleStoreExtendedService`, `m_Controller.AppleStoreExtendedPurchaseService`, `m_Controller.GooglePlayStoreExtendedService`, `m_Controller.GooglePlayStoreExtendedPurchaseService` (null-check required — only non-null on the matching platform).
6. **`product.receipt` and `product.hasReceipt` are gone**: Use `order.Info.Receipt` from `PendingOrder` for the transaction receipt. For ownership checking, use `m_Controller.CheckEntitlement(product)` + `OnCheckEntitlement` (recommended) or track ownership with `bool` flags updated in `OnPurchasePending` and `OnPurchasesFetched`.
7. **`Purchase()` no longer has a payload**: Developer payload support was removed in Google Billing v3.
8. **`ProcessPurchase` logic belongs in two places**: Put it in both `OnPurchasePending` (new purchases) and `OnPurchasesFetched` (restored/existing purchases).
9. **Restore is implicit**: Calling `FetchPurchases()` automatically re-delivers any unconfirmed purchases via `OnPurchasePending`. Explicit `RestoreTransactions` is still required for the iOS "Restore Purchases" button.
10. **`SubscriptionManager` is replaced**: No more `new SubscriptionManager(product, introJson).getSubscriptionInfo()`. In v5, subscription info is accessed via `order.Info.PurchasedProductInfo.FirstOrDefault(p => p.productId == id)?.subscriptionInfo` — it's on `IPurchasedProductInfo`, NOT on `CartItem`. `CartItem` only has `Product` and `Quantity`. Method names are now PascalCase: `IsSubscribed()` not `isSubscribed()`.
11. **Platform-specific config must happen post-`Connect()`**: `SetAppAccountToken`, `SetObfuscatedAccountId/ProfileId`, `canMakePayments` — all moved from pre-init `ConfigurationBuilder` to the corresponding extended service, only available after `await m_Controller.Connect()`.
12. **Apple promotional APIs moved to `IAppleStoreExtendedProductService`**: `SetStorePromotionOrder`, `SetStorePromotionVisibility`, `FetchStorePromotionOrder`, `FetchStorePromotionVisibility` are now on `m_Controller.AppleStoreExtendedProductService` (null-check required — only non-null on Apple platforms). No more `GetExtension<IAppleExtensions>()` pattern.
13. **`OnPurchaseConfirmed` receives `Order` base type**: You MUST pattern-match `ConfirmedOrder` (success) vs `FailedOrder` (confirmation failed). Do not assume confirmation always succeeds.
14. **Platform-specific events are on extended services, NOT the controller**: `OnPromotionalPurchaseIntercepted` and `OnEntitlementRevoked` are on `AppleStoreExtendedPurchaseService`. `OnDeferredPaymentUntilRenewalDate` is on `GooglePlayStoreExtendedPurchaseService`. Only `OnPurchaseDeferred` (Ask-to-Buy) is directly on `PurchasingController`. Always null-check the extended service before subscribing to events (use `if (m_Controller.AppleStoreExtendedPurchaseService != null)` pattern — `?.` does not work with `+=`).

## Migration Anti-Patterns

**Always subscribe to BOTH success and failure events.** Not subscribing to failure events (e.g., `OnProductsFetchFailed`, `OnPurchasesFetchFailed`, `OnStoreDisconnected`) generates runtime warnings and leaves failures unhandled.

**Always subscribe to `OnPurchaseDeferred`.** This event fires for Ask-to-Buy (iOS parental approval) and Google Play deferred purchases. Not subscribing means deferred purchases are silently ignored.

**Use `CheckEntitlement` for ownership checks, not manual bool flags.** When migrating `product.hasReceipt` or `product.receipt == null` ownership checks, the recommended v5 pattern is `m_Controller.CheckEntitlement(product)` + `m_Controller.OnCheckEntitlement`. This works cross-platform and handles edge cases (refunds, subscription expiration) automatically.

**`StoreConnectionFailureDescription` has `.message`, NOT `.reason`.** When migrating `OnInitializeFailed(error, message)` to `m_Controller.OnStoreDisconnected`, the failure description property is `failureDescription.message` (lowercase), not `reason`.

**`ProductFetchFailed` has `.FailureReason`, NOT `.Message`.** When handling `m_Controller.OnProductsFetchFailed`, access the reason via `failure.FailureReason`.

**Subscribe to events BEFORE calling `Connect()`.** Pending purchases from a previous session may fire immediately on reconnect. If your `OnPurchasePending` handler is not yet registered, those purchases will be missed.

**`OnPurchaseConfirmed` can receive `FailedOrder`.** The `OnPurchaseConfirmed` event fires with `Order` (base type). Always pattern-match: `ConfirmedOrder` means success, `FailedOrder` means confirmation failed. Do not assume confirmation always succeeds.

**Use `Awake()` for initialization, not `Start()`.** The official v5 samples use `Awake()` for controller setup and event subscription. This ensures IAP is initialized before other `Start()` methods that may depend on it.

## Minimal v5 Example

```csharp
PurchasingController m_Controller;

async void Awake()
{
    m_Controller = new PurchasingController(UnityIAPServices.GetDefaultStore());

    // Subscribe to ALL events BEFORE Connect — pending purchases may fire on reconnect
    m_Controller.OnPurchasePending += (storeName, order) =>
    {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        GrantContent(product);
        m_Controller.ConfirmPurchase(order);
    };
    m_Controller.OnPurchaseConfirmed += (storeName, order) =>
    {
        switch (order)
        {
            case ConfirmedOrder: Debug.Log("Purchase confirmed"); break;
            case FailedOrder failed: Debug.LogError($"Confirmation failed: {failed.FailureReason}"); break;
        }
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
        new ProductDefinition("com.mygame.coins100", ProductType.Consumable)
    };

    m_Controller.FetchProducts(products, storeName);
    // Pending/unfinished purchases are restored in OnProductsFetched, once the products are cached
}
```
