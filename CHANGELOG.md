# Changelog

## [4.7.0] - 2023-02-09
### Added
- Added `storeSpecificErrorCode` to `PurchaseFailureDescription.message` when available.

### Fixed
- Unity IAP will consider the call to `UnityPurchasing.initialize` completed before invoking the correct callback `IStoreListener.OnInitialized` or `IStoreListener.OnInitializeFailed`. This prevents these callbacks from being invoked more than once per initialization.
- GooglePlay - Fixed `No such proxy method` exception in our representation of `BillingClientStateListener.onBillingServiceDisconnected()` introduced by Unity IAP 4.6.0
- Apple - Fixed a `NullReferenceException` happening when the receipt isn't found.

### Changed
- Removed `com.unity.services.analytics` from the IAP SDK dependencies

## [4.6.0] - 2023-02-02
### Added
- Added a new restore transaction callback `RestoreTransactions(Action<bool, string> callback)` to obtain the error string when RestoreTransactions is not successful (`IAppleExtensions` and `IGooglePlayStoreExtensions`).
- Added a new initialize failed callback `IStoreListener.OnInitializeFailed(InitializationFailureReason, string)` to obtain the error string when OnInitializeFailed is invoked.
- Added a new setup failed callback `IStoreCallback.OnSetupFailed(InitializationFailureReason, string)` to obtain the error string when OnSetupFailed is invoked.
- Added a new FetchAdditionalProducts. The failCallback contains an error string. `IStoreController.FetchAdditionalProducts(HashSet<ProductDefinition>, Action, Action<InitializationFailureReason, string>)`
- Apple - `Product.appleOriginalTransactionId` : Returns the original transaction ID. This field is only available when the purchase was made in the active session.
- Apple - `Product.appleProductIsRestored` : Indicates whether the product has been restored.
- GooglePlay - `IGooglePlayConfiguration.SetFetchPurchasesExcludeDeferred(bool exclude)` has been added to revert to the previous behaviour. This is not recommended and should only be used if `Deferred` purchases are handled in your `IStoreListener.ProcessPurchase`.
- GooglePlay - `IGooglePlayStoreExtensions.GetPurchaseState(Product product)` has been added to obtain the `GooglePurchaseState` of a product.
- GooglePlay - Added missing values to `GoogleBillingResponseCode` in order to output it in `PurchaseFailureDescription`'s message when available.
- Codeless - Added to the [IAP Button](https://docs.unity3d.com/Packages/com.unity.purchasing@4.6/manual/IAPButton.html) the option to add a script for the On Transactions Restored: `void OnTransactionsRestored(bool success, string? error)`

### Changed
- Upgraded `com.unity.services.core` from 1.3.1 to 1.5.2
- Upgraded `com.unity.services.analytics` from 4.0.1 to 4.2.0
- The old OnInitializeFailed `OnInitializeFailed(InitializationFailureReason error)` was marked `Obsolete`
- The old OnSetupFailed `OnSetupFailed(InitializationFailureReason reason)` was marked `Obsolete`
- The old FetchAdditionalProducts `FetchAdditionalProducts(HashSet<ProductDefinition> additionalProducts, Action successCallback, Action<InitializationFailureReason> failCallback)` was marked `Obsolete`
- The old restore transaction callback `RestoreTransactions(Action<bool> callback)` was marked `Obsolete` (`IAppleExtensions` and `IGooglePlayStoreExtensions`).
- Apple - Transactions received from Apple that are invalid (where the product is not entitled) will no longer output the `Finishing transaction` log. This only affects transactions that never reached the `ProcessPurchase`.
- GooglePlay - The enum `GooglePurchaseState` now recognizes `4` as `Deferred`.

### Fixed
- Analytics - A ServicesInitializationException introduced in Analytics 4.3.0 is now handled properly.
- Analytics - Fixed an issue where transactions events were invalidated when there was no localization data for a product.
- GooglePlay - Fixed a `NullReferenceException` when querying sku details while the BillingClient is not ready.
- GooglePlay - Fixed [Application Not Responding (ANR)](https://developer.android.com/topic/performance/vitals/anr) when foregrounding the application while disconnected from the Google Play Store.
- GooglePlay - Limited the occurence of `PurchasingUnavailable` errors when retrieving products while in a disconnected state to once per connection.
- GooglePlay - `Deferred` purchases are, by default, no longer sent to `IStoreListener.ProcessPurchase` when fetching purchases. This avoids the possibility of granting products that were not paid for. These purchases will only be processed once they become `Purchased`. This can be reverted with `IGooglePlayConfiguration.SetFetchPurchasesExcludeDeferred(bool exclude)` to not exclude them, but `Deferred` purchases will need to be handled in `IStoreListener.ProcessPurchase`.
- Unity IAP will consider the call to `UnityPurchasing.initialize` completed before invoking the correct callback `IStoreListener.OnInitialized` or `IStoreListener.OnInitializeFailed`. This prevents these callbacks from being invoked more than once per initialization.

## [4.5.2] - 2022-10-28
### Fixed
- Removed unused exception variable causing a compiler warning CS0168.
- Telemetry - Calls to telemetry reporting were occasionally tripping a `NullReferenceException`, `IndexOutOfRangeException` or `KeyNotFoundException`,  for some users. These exceptions are now caught safely and logged. These failures are also mitigated by moving all Telemetry calls to the main thread. Issue noticed in IAP 4.4.1, but may be older.
- Apple - Optimized memory usage when processing transactions to prevent out of memory crash when processing transactions on launch.
- Batch Mode - Calls to `UnityPurchasingEditor.TargetAndroidStore` to select UDP will now successfully check UDP package installation and log an error instead of throwing a blocking popup when executed as part of a Batch Mode command.
- Analytics - Removed escape characters for receipt JSON which was causing parsing issues in the backend.
- GooglePlay - Fixed a bug causing a crash when retrying to finish a transaction while disconnected

## [4.5.1] - 2022-10-13
### Fixed
- GooglePlay - Fixed deferred purchases being processed when the app is foregrounded. Issue introduced in Unity IAP 4.5.0.
- GooglePlay - Fixed a `NullReferenceException` in `DequeueQueryProducts` happening when launching the app. Issue introduced in Unity IAP 4.2.0.
- Analytics - Fixed a `NullReferenceException` when reporting failed transactions of purchase unavailable products. Issue introduced in Unity IAP 4.2.0.
- Analytics - Legacy Analytics will no longer report events in custom UGS environments, which would cause misreported live sales figures. Issue introduced in Unity IAP 4.2.0.

## [4.5.0] - 2022-09-23
### Added
- Apple - Add support for [Family Sharing](https://developer.apple.com/app-store/subscriptions/#family-sharing).
  - API `IAppleConfiguration.SetEntitlementsRevokedListener(Action<List<Product>>` called when entitlement to a products are revoked. The `Action` will be called with the list of revoked products. See documentation "Store Guides" > "iOS & Mac App Stores" for a sample usage.
  - API - Product metadata is now available in `AppleProductMetadata` from `ProductMetadata.GetAppleProductMetadata()` via `IStoreController.products`.
  - API `AppleProductMetadata.isFamilyShareable` indicated if the product is family shareable.
  - `Apple App Store - 11 Family Sharing` sample that showcases how to use Unity IAP to manage family shared purchases.

### Fixed
- GooglePlay - Processing out-of-app purchases such as Promo codes no longer requires the app to be restarted. The
  purchase will be processed the next time the app is foregrounded. Technical limitation: In the case of promo codes, if
  the app is opened while the code is redeemed, you might receive an additional call
  to `IStoreListener.OnPurchaseFailed` with `PurchaseFailureReason.Unknown`. This can be safely ignored.
- GooglePlay - Fixed a `NullReferenceException` that would rarely occur when retrieving products due to a concurrency issue introduced in Unity IAP 4.2.0

## [4.4.1] - 2022-08-11
### Fixed
- GooglePlay - Fixed NullReferenceException and ArgumentException that would rarely occur due to a concurrency issue introduced in Unity IAP 4.2.0
- Amazon - Set android:export to true to support Android API level 31+

## [4.4.0] - 2022-07-11
### Added
- GooglePlay - Google Play Billing Library version 4.0.0.
  - The Multi-quantity feature is not yet supported by the IAP package and will come in a future update. **Do not enable `Multi-quantity` in the Google Play Console.**
  - Add support for
      the [IMMEDIATE_AND_CHARGE_FULL_PRICE](https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.ProrationMode#IMMEDIATE_AND_CHARGE_FULL_PRICE)
      proration mode. Use `GooglePlayProrationMode.ImmediateAndChargeFullPrice` for easy access.
  - The `"skuDetails"` in the receipt json is now an array of the old structure, not just one object. It will only have one element in most cases, so if this is being parsed in your app, treat it like an array and get the first element by default.

### Fixed
- GooglePlay - Fix `IGooglePlayConfiguration.SetDeferredPurchaseListener`
  and `IGooglePlayConfiguration.SetDeferredProrationUpgradeDowngradeSubscriptionListener` callbacks sometimes not being
  called from the main thread.
- GooglePlay - When configuring `IGooglePlayConfiguration.SetQueryProductDetailsFailedListener(Action<int> retryCount)`, the action will be invoked with retryCount starting at 1 instead of 0.
- GooglePlay - Added a validation when upgrading/downgrading a subscription that calls `IStoreListener.OnPurchaseFailed` with `PurchaseFailureReason.ProductUnavailable` when the old transaction id is empty or null. This can occur when attempting to upgrade/downgrade a subscription that the user doesn't own.

## [4.3.0] - 2022-06-16
### Added
- GooglePlay - API `IGooglePlayConfiguration.SetQueryProductDetailsFailedListener(Action<int>)` called when Unity IAP fails to query product details. The `Action` will be called on each query product details failure with the retry count. See documentation "Store Guides" > "Google Play" for a sample usage.

## [4.2.1] - 2022-06-14
### Fixed
- Downgrade `com.unity.services.core` from 1.4.1 to 1.3.1 due to a new bug found in 1.4.1

## [4.2.0] - 2022-06-13

### Added
- Feature to automatically initialize **Unity Gaming Services** through the catalog UI. Please see the [documentation](https://docs.unity3d.com/Packages/com.unity.purchasing@4.2/manual/UnityIAPInitializeUnityGamingServices.html) for more details.

### Changed
- The In-App Purchasing package now requires **Unity Gaming Services** to have been initialized before it can be used.
For the time being **IAP** will continue working as usual, but will log a warning if **Unity Gaming Services** has not been initialized.
In future releases of this package, initializing **Unity Gaming Services** will be mandatory. Please see the [documentation](https://docs.unity3d.com/Packages/com.unity.purchasing@4.2/manual/UnityIAPInitializeUnityGamingServices.html) for more details.

## [4.2.0-pre.2] - 2022-04-28

### Added
- Support for Unity Analytics TransactionFailed event.
- Sample showcasing how to initialize [Unity Gaming Services](https://unity.com/solutions/gaming-services) using the [Services Core API](https://docs.unity.com/ugs-overview/services-core-api.html)

### Changed
- The Analytics notice in the In-App Purchasing service window has been removed for Unity Editors 2022 and up.

## [4.2.0-pre.1] - 2022-04-07

### Added
- Support for the [new Unity Analytics](https://unity.com/products/unity-analytics) [transaction event](https://docs.unity.com/analytics/AnalyticsSDKAPI.html#Transaction).
- The package will now send telemetry diagnostic and metric events to help improve the long-term reliability and performance of the package.

### Changed
- The minimum Unity Editor version supported is 2020.3.
- The In-App Purchasing service window now links to the [new Unity Dashboard](https://dashboard.unity3d.com/) for Unity Editors 2022 and up.

### Fixed
- GooglePlay - Fixed OnInitializeFailed never called if GooglePlay BillingClient is not ready during initialization.
- GooglePlay - GoogleBilling is allowed to initialize correctly even if the user's Google account is logged out, so long as it is linked. The user will need to log in to their account to continue making purchases.
- Fixed a build error `DirectoryNotFoundException` that occurred when the build platform was iOS or tvOS and the build target was another platform.

## [4.1.5] - 2022-05-17

### Fixed
- GooglePlay - Fixed a null reference exception introduced in Unity IAP 4.1.4 that could happen when cancelling an in-app purchase.

## [4.1.4] - 2022-03-30

### Fixed
- GooglePlay - Fixed issue where if an app is backgrounded while a purchase is being processed,
an `OnPurchaseFailed` would be called with the purchase failure reason `UserCancelled`, even if the purchase was successful.

## [4.1.3] - 2022-01-11

### Fixed
- Removed deprecated UnityWebRequest calls, updating them to use safer ones. This avoids compiler warnings that may occur.
- Fixed a serious edge case where Apple StoreKit receipt parsing might fail, preventing validation. A portion of receipts on iOS could be affected and cause Unity IAP to freeze after the purchase completed, but before the SDK can finalize the purchase. The user will have to uninstall and reinstall your app in order to recover from this. Your customer service will have to refund the user's purchase or apply the purchase in some other way outside of Unity IAP. This bug was accidentally introduced in Unity IAP 4.1.0. To avoid encountering this problem with your app, we suggest you update to this version.

## [4.1.2] - 2021-11-15

### Fixed
- Various internal obsolete warnings have been removed, allowing the project to be compiled with errors as warnings enabled.

## [4.1.1] - 2021-10-28

### Changed
- A default store will be selected for each platform. For Android the default store will be Google. All other platforms already had default stores.

## [4.1.0] - 2021-09-20

### Added
- Apple - Add support for receipt validation with [StoreKit Test](https://developer.apple.com/documentation/Xcode/setting-up-storekit-testing-in-xcode). See the [Receipt Validation Obfuscator manual](https://docs.unity3d.com/Packages/com.unity.purchasing@4.0/manual/UnityIAPValidatingReceipts.html) for a usage recommendation. See also the [sample](https://docs.unity3d.com/Packages/com.unity.purchasing@4.0/manual/Overview.html#learn-more) "05 Local Receipt Validation" for an example.
- GooglePlay - Add support for controlling automatic fetching of purchases at initialization, with `IGooglePlayConfiguration.SetFetchPurchasesAtInitialize(bool)`. Use to help distinguish previously seen purchases from new purchases. Then to fetch previously seen purchases use `IGooglePlayExtensions.RestorePurchases(Action<bool>)`.

### Changed
- Menu items for this package were renamed from *Unity IAP -> In-App Purchasing* and have been moved from *Window > Unity IAP* to *Services > In-App Purchasing*.
- Choosing an Android app store target before building the Android Build Target is now required. A build error will be emitted otherwise. Set this with the Store Selector window (*Services > In-App Purchasing > Switch Store ...*) or the API (`UnityPurchasingEditor.TargetAndroidStore()`). The default Android app store is now AppStore.NotSpecified and is visible in the window as `<Select a targeted store>`. Previously the default app store was the Google Play Store for the Android Build Target. See the [Store Selector documentation](https://docs.unity3d.com/Packages/com.unity.purchasing@4.1/manual/StoreSelector.html) for more
- Apple - Workaround rare crash seen if `nil` `NSLocaleCurrencyCode` is received when extracting localized currency code from `[SKProduct priceLocale]` when fetching products. Substitutes [ISO Unknown Currency code "XXX"](https://en.wikipedia.org/wiki/ISO_4217#X_currencies) into `ProductMetadata.isoCurrencyCode`.
- Removed warning log `Already recorded transaction`.
- Codeless - The default setting for enabling Codeless Auto Initialization in new projects' catalogs is now true instead of false. (As seen in the Catalog Editor as "Automatically initialize UnityPurchasing (recommended)").

### Fixed
- Fixed warning, missing await for async call in ExponentialRetryPolicy.cs

### Removed
- Removed the original and complex Unity IAP sample known as "Example", or "IAP Demo". Please use the recently added [samples](https://docs.unity3d.com/Packages/com.unity.purchasing@4.0/manual/Overview.html#learn-more) for a granular introduction to In-App Purchasing features.

## [4.0.3] - 2021-08-18
### Added
- Added samples to the [Package Manager Details view](https://docs.unity3d.com/Manual/upm-ui-details.html):
  - Apple Sample - Restoring Transactions
  - Apple Sample - Handling Deferred Purchases
  - Apple Sample - Detecting Fraud
  - Apple Sample - Getting Introductory Prices
  - Apple Sample - Present Code Redemption Sheet
  - Apple Sample - Can Make Payments
  - Apple Sample - Retrieving Product Receipts
  - Apple Sample - Subscription Upgrade Downgrade
  - Apple Sample - Promoting Products
- Apple - Added support for fetching the current store promotion order of products on this device with `void IAppleExtensions.FetchStorePromotionOrder(Action<List<Product>> successCallback, Action errorCallback)`
- Apple - Added support for fetching the current store promotion visibility of a product on this device with `void FetchStorePromotionVisibilitySuccess(Product product, AppleStorePromotionVisibility visibility)`

## Fixed
- Apple - Fixed issue with unknown products being processed with `NonConsumable` type.

### Fixed
- GooglePlay - Fixed issue that led to purchases failing with a `ProductUnavailable` error when  fetching additional products multiple times in quick succession.
- GooglePlay - Fixed issue that led to purchases failing with a `ProductUnavailable` error when a game had been running for some time.
- GooglePlay - Fixed issue that led to initialization failing with a `NoProductsAvailable` error when the network is interrupted while initializing, requiring the user to restart the app. Now Unity IAP handle initialization with poor network
  connectivity by retrying periodically. This retry behavior is consistent with our Apple App Store's, and with the previous version of our Google Play Store's implementations.

### Changed
- Restructured [Manual documentation](https://docs.unity3d.com/Packages/com.unity.purchasing@4.0/manual/index.html) to improve readability.

## [4.0.0] - 2021-07-19
### Added
- Codeless Listener method to access the store configuration after initialization.
  - `CodelessIAPStoreListener.Instance.GetStoreConfiguration`
- Several samples to the [Package Manager Details view](https://docs.unity3d.com/Manual/upm-ui-details.html) for com.unity.purchasing:
  - Fetching additional products
  - Integrating self-provided backend receipt validation
  - Local receipt validation
  - Google Play Store - Upgrade and downgrade subscriptions
  - Google Play Store - Restoring Transactions
  - Google Play Store - Confirming subscription price change
  - Google Play Store - Handling Deferred Purchases
  - Google Play Store - Fraud detection
  - Apple App Store - Refreshing app receipts
- Google Play - `GooglePlayProrationMode` enum that represent Google's proration modes and added `IGooglePlayStoreExtensions.UpgradeDowngradeSubscription` using the enum.

### Fixed
- GooglePlay - Fixed [Application Not Responding (ANR)](https://developer.android.com/topic/performance/vitals/anr) error at `Product` initialization. The Google Play `SkuDetailsResponseListener.onSkuDetailsResponse` callback is now quickly handled.
- Amazon - Fixed `Product.metadata.localizedPrice` incorrectly being `0.00` for certain price formats.
- Apple, Mac App Store - Fixes Apple Silicon "arm64" support, missing from unitypurchasing bundle.

### Changed
- Reorganized and renamed APIs:
  - `CodelessIAPStoreListener.Instance.ExtensionProvider.GetExtension` to `CodelessIAPStoreListener.Instance.GetStoreExtensions` to match the new `GetStoreConfiguration` API, above
  - `IGooglePlayStoreExtensions.NotifyDeferredProrationUpgradeDowngradeSubscription` to `IGooglePlayConfiguration.NotifyDeferredProrationUpgradeDowngradeSubscription`
  - `IGooglePlayStoreExtensions.NotifyDeferredPurchase` to `IGooglePlayConfiguration.NotifyDeferredPurchase`
  - `IGooglePlayStoreExtensions.SetDeferredProrationUpgradeDowngradeSubscriptionListener` to `IGooglePlayConfiguration.SetDeferredProrationUpgradeDowngradeSubscriptionListener`
  - `IGooglePlayStoreExtensions.SetDeferredPurchaseListener` to `IGooglePlayConfiguration.SetDeferredPurchaseListener`
  - `IGooglePlayStoreExtensions.SetObfuscatedAccountId` to `IGooglePlayConfiguration.SetObfuscatedAccountId`
  - `IGooglePlayStoreExtensions.SetObfuscatedProfileId` to `IGooglePlayConfiguration.SetObfuscatedProfileId`
- Apple - Change the order of execution of the post-process build script, which adds the `StoreKitFramework` such that other post-process build scripts can run after it.
- Changed the __Target Android__ Menu app store selection feature to display a window under `Window > Unity IAP > Switch Store...`. To set the app store for the next build, first use __Build Settings__ to activate the Android build target.
- For the future Unity 2022
  - Moved Unity IAP menu items from `Window > Unity IAP > ...` to  `Services > In-App Purchasing > ...`
  - Updated and added new functionnality to the `Services > In-App Purchasing` window in the `Project Settings`. The `Current Targeted Store` selector and `Receipt Obfuscator` settings are now accessible from this window.

### Removed
- Samsung Galaxy - Removed Samsung Galaxy Store in-app purchasing support. Use the [Unity Distribution Portal](https://unity.com/products/unity-distribution-portal) for the continued support of the Samsung Galaxy Store.
    - All related classes and implementations have been removed including `AppStore.SamsungApps`.
- Removed the following obsolete API:
  - `CloudCatalogImpl`
  - `CloudCatalogUploader`
  - `CloudJSONProductCatalogExporter`
  - `EventDestType`
  - All `GooglePlayReceipt` constructors. Use `GooglePlayReceipt(string productID, string orderID, string packageName, string purchaseToken, DateTime purchaseTime, GooglePurchaseState purchaseState)` instead.
  - `IAndroidStoreSelection.androidStore`
  - `IDs.useCloudCatalog`
  - `IGooglePlayConfiguration.SetPublicKey`
  - `IGooglePlayConfiguration.UsePurchaseTokenForTransactionId`
  - `IGooglePlayConfiguration.aggressivelyRecoverLostPurchases`
  - `IGooglePlayStoreExtensionsMethod.FinishAdditionalTransaction`
  - `IGooglePlayStoreExtensionsMethod.GetProductJSONDictionary`
  - `IGooglePlayStoreExtensionsMethod.IsOwned`
  - `IGooglePlayStoreExtensionsMethod.SetLogLevel`
  - `IManagedStoreConfig`
  - `IManagedStoreExtensions`
  - `IStoreCallback.OnPurchasesRetrieved`. Use `IStoreCallback.OnAllPurchasesRetrieved` instead.
  - `Promo`
  - `StandardPurchasingModule.Instance(AndroidStore)`. Use `StandardPurchasingModule.Instance(AppStore)` instead.
  - `StandardPurchasingModule.androidStore`. Use `StandardPurchasingModule.appStore` instead.
  - `StandardPurchasingModule.useMockBillingSystem`. Use `IMicrosoftConfiguration` instead.
  - `StoreTestMode`
  - `UnityPurchasingEditor.TargetAndroidStore(AndroidStore)`. Use `TargetAndroidStore(AppStore)` instead.
  - `WinRT` class. Use `WindowsStore` instead.
  - `WindowsPhone8` class. Use `WindowsStore` instead.

## [3.2.3] - 2021-07-08
### Fixed
- GooglePlay - Fix `DuplicateTransaction` errors seen during purchase, after a purchase had previously been Acknowledged with Google.
- GooglePlay - Fix `DuplicateTransaction` errors seen after a user starts a purchase on a game with Unity IAP 1.x or 2.x, quits their game, upgrades their game to include a version of Unity IAP 3.x, and tries to finish consuming / completing that old purchase.

## [3.2.2] - 2021-06-02
### Added
- Sample to the [Package Manager Details view](https://docs.unity3d.com/Manual/upm-ui-details.html) for com.unity.purchasing:
  - Buying consumables

### Fixed
- WebGL - While WebGL is not supported with an included app store implementation, the WebGL Player will no longer crash when the `StandardPurchasingModule.Initialize` API is called if Project Settings > Player > WebGL > Publishing Settings > Enable Exceptions > "Explicitly Thrown Exceptions Only" or "None" are set.
- Amazon - Better support for Android R8 compiler. Added minification (Project Settings > Player > Publishing Settings > Minify) "keep" ProGuard rules.

## [3.2.1] - 2021-05-18
### Changed
- Manual and API documentation updated.

## [3.2.0] - 2021-05-17
### Added
- GooglePlay - Automatic resumption of initialization when a user's device initially does not have a Google account, and they correct that Android setting without killing the app, then they resume the app. NOTE this does not impact Unity IAP's behavior when a user removes their Google account after initialization.
- GooglePlay - API `IGooglePlayConfiguration.SetServiceDisconnectAtInitializeListener(Action)` called when Unity IAP fails to connect to the underlying Google Play Billing service. The `Action` may be called multiple times after `UnityPurchasing.Initialize` if a user does not have a Google account added to their Android device. Initialization of Unity IAP will remain paused until this is corrected. Inform the user they must add a Google account in order to be able to purchase. See documentation "Store Guides" > "Google Play" for a sample usage.
- GooglePlay - It is now possible to check if a purchased product is pending or not by calling IsPurchasedProductDeferred() from GooglePlayStoreExtensions.
- UDP - RegisterPurchaseDeferredListener in IUDPExtensions can be used to assign a callback for pending purchases.

### Fixed
- GooglePlay - Receipts for Pending purchases are now UnifiedReceipts and not raw Google receipts. Any parsers you have for these can extract the raw receipt json by parsing the "Payload" field.
- Editor - The Fake Store UI used in Play Mode in the Editor, as well as some unsupported platforms has been restored. A null reference exception when trying to make a purchase no longer occurs.
- UDP - Added a null check when comparing Store-Specific IDs

### Changed:
- Samsung Galaxy - Support is being deprecated when not using Unity Distribution Portal as a target. The feature will be removed soon. Please use the Unity Distribution Portal package with IAP for full Samsung Galaxy support.

## [3.1.0] - 2021-04-15
### Added
- GooglePlay - Google Play Billing Library version 3.0.3.
  - Fixes a broken purchase flow when user resumed their app through the Android Launcher after interrupting an ongoing purchase. Now `IStoreListener.OnPurchaseFailed(PurchaseFailureDescription.reason: PurchaseFailureReason.UserCancelled)` is called on resumption. E.g. first the user initiates a purchase, then sees the Google purchasing dialog, and sends their app to the background via the device's Home key. They tap the app's icon in the Launcher, see no dialog, and, finally, the app will now receive this callback.

### Changed
- `string StandardPurchasingModule.k_PackageVersion` is obsolete and will incorrectly report `"3.0.1"`. Please use the new `string StandardPurchasingModule.Version` to read the correct current version of this package.
- Reduced logging, and corrected the severity of several logs.

### Fixed
- tvOS - build errors due to undesirable call to `[[SKPaymentQueue defaultQueue] presentCodeRedemptionSheet]` which will now only be used for iOS 14.
- tvOS, macOS - Builds missing Xcode project In-App Purchasing capability and StoreKit.framework.
- Security - Tangle files causing compilation errors on platforms not supported by Security: non-GooglePlay and non-Apple.
- GooglePlay - Subscription upgrade/downgrade using proration mode [DEFERRED](https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.ProrationMode#DEFERRED) (via `IGooglePlayStoreExtensions.UpgradeDowngradeSubscription(string oldSku, string newSku, int desiredProrationMode)`) reported `OnPurchaseFailed` with `PurchaseFailureReason.Unknown`, when the deferred subscription upgrade/downgrade succeeded. This subscription change generates no immediate transaction and no receipt. Now a custom `Action<Product>` can be called when the change succeeds, and is set by the new `SetDeferredProrationUpgradeDowngradeSubscriptionListener` API:
  - Adds `IGooglePlayStoreExtensions.SetDeferredProrationUpgradeDowngradeSubscriptionListener(Action<Product> action)`. Sets listener for deferred subscription change events. Deferred subscription changes only take effect at the renewal cycle and no transaction is done immediately, therefore there is no receipt nor token. The `Action<Product>` is the deferred subscription change event. No payout is granted here. Instead, notify the user that the subscription change will take effect at the next renewal cycle.

## [3.0.2] - 2021-03-30

### Added
- Comprehensive manual and API documentation.

## [3.0.1] - 2021-03-08
### Removed
- Pre-release disclaimer.

## [3.0.0] - 2021-03-05

## [3.0.0-pre.7] - 2021-03-03
### Added
- GooglePlay - populate `Product.receipt` for `Action<Product>` parameter returned by `IGooglePlayStoreExtensions.SetDeferredPurchaseListener` callback

### Changed
- WinRT - This feature is now shipped as C# code under assembly definitions instead of .dll files.
- Security - This feature is now shipped as C# code under assembly definitions instead of .dll files.
- Receipt Validation Obfuscator - The Tangle File Obfuscate function is now Editor-only and no longer part of the Runtime Security module.

### Fixed
- Windows Standalone - launches FakeStore when detected by StandardPurchasingModule; disentangled from WinRT
- Security - restored Receipt Validation Obfuscator Editor functionality
- GooglePlay - fix regression, avoiding exception when using IGooglePlayConfiguration while running on a non-Google target

## [3.0.0-pre.6] - 2021-02-09
### Fixed
- WinRT - There was a bad path being pointed to by the .dll's meta file, preventing compilation to this target.

## [3.0.0-pre.5] - 2021-01-12
### Added
- Apple - Support for [auto-renewable subscription Offer Codes](https://developer.apple.com/documentation/storekit/in-app_purchase/subscriptions_and_offers/implementing_offer_codes_in_your_app) on iOS and iPadOS 14 and later via `IAppleExtensions.PresentOfferRedemptionSheet()`. E.g.

 ```csharp
public void ShowSubscriptionOfferRedemption(IExtensionProvider extensions)
{
    var appleExtensions = extensions.GetExtension<IAppleExtensions>();
    appleExtensions.PresentOfferRedemptionSheet();
}
```

### Fixed
 - Security and WinRT stub dlls and references to Analytics no longer break builds unsupported platforms like PS4, XboxOne, Switch and Lumin. These platforms are still unsupported but will no longer raise errors on build.

### Removed
- Support for Facebook in-app purchasing is no longer provided. All classes and implementations have been removed.

## [3.0.0-pre.4] - 2020-10-09
- Fix builds for UWP

## [3.0.0-pre.3] - 2020-10-09
- First integration into Unity 2021
- Includes changes listed in [CHANGELOG-ASSETSTORE.md](CHANGELOG-ASSETSTORE.md), starting from version 1, ending 2020-10-09
- **This is the first release of the Unified *Unity In App Purchasing*, combining the old package and its Asset Store Components.**

## [2.2.2] - 2021-01-19
- Fixed logs incorrectly formatted showing “purchases({0}): -id of product-”
- Renamed method IStoreCallback.OnPurchasesRetrieved to IStoreCallback.OnAllPurchasesRetrieved, deprecated old method name. This is to fix a problem when refreshing receipts.

## [2.2.1] - 2020-11-19
- Fixed exposure of function calls at runtime used by the Asset Store Package 2.2.0 and up.

## [2.2.0] - 2020-10-22
- Google Billing v3

## [2.1.2] - 2020-09-20
Fix migration tooling's obfuscator file destination path to target Scripts instead of Resources

## [2.1.1] - 2020-08-25
- Fix compilation compatibility with platforms that don't use Unity Analytics (ex: PS4)
- Fix compilation compatibility with "Scripting Runtime Version" option set to ".Net 3.5 Equivalent (Deprecated)" in Unity 2018.4

## [2.1.0] - 2020-06-29
- Source Code provided instead of precompiled dlls.
- Live vs Stub DLLs are now using asmdef files to differentiate their targeting via the Editor
- Fixed errors regarding failing to find assemblies when toggling In-App Purchasing in the Service Window or Purchasing Service Settings
- Fixed failure to find UI assemblies when updating the Editor version.
- Added menu to support eventual migration to In-App Purchasing version 3.

## [2.0.6] - 2019-02-18
- Remove embedded prebuilt assemblies.

## [2.0.5] - 2019-02-08
- Fixed Unsupported platform error

## [2.0.4] - 2019-01-20
- Added editor and playmode testing.

## [2.0.3] - 2018-06-14
- Fixed issue related to 2.0.2 that caused new projects to not compile in the editor.
- Engine dll is enabled for editor by default.
- Removed meta data that disabled engine dll for windows store.

## [2.0.2] - 2018-06-12
- Fixed issue where TypeLoadException occured while using "UnityEngine.Purchasing" because SimpleJson was not found. fogbugzId: 1035663.

## [2.0.1] - 2018-02-14
- Fixed issue where importing the asset store package would fail due to importer settings.

## [2.0.0] - 2018-02-07
- Fixed issue with IAP_PURCHASING flag not set on project load.
