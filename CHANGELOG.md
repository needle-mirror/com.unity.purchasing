# Changelog

## [3.0.1] - 2021-03-08
### Removed
- Pre-release disclaimer.

## [3.0.0] - 2021-03-05

## [3.0.0-pre.7] - 2021-03-03
### Added
GooglePlay - populate `Product.receipt` for `Action<Product>` parameter returned by `IGooglePlayStoreExtensions.SetDeferredPurchaseListener` callback

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
