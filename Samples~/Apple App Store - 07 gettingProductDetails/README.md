## README - In-App Purchasing Sample Scenes - Apple App Store - Getting Product Details

This sample showcases how to use Apple extensions to get additional product details.

## Instructions to test this sample:

1. Have in-app purchasing correctly configured with
   the [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@3.2/manual/UnityIAPAppleConfiguration.html).
2. Configure a non-consumable product.
3. Set your own product's id in the `InAppPurchasing game object > Getting Product Information script > No Ads Product Id field`
   or change the `noAdsProductId` field in the `GettingProductDetails.cs` script.
4. Build your project for `iOS`.
   1. If you are using a simulator with Xcode 12+, follow these [instructions](https://developer.apple.com/documentation/xcode/setting-up-storekit-testing-in-xcode)
      to set up StoreKit Testing.

## Getting Product Information

`IAppleExtension.GetProductDetails` returns a dictionary of JSON encoded strings keyed by productIds, made up of:
   `subscriptionNumberOfUnits`
   `subscriptionPeriodUnit`
   `localizedPrice`
   `isoCurrencyCode`
   `localizedPriceString`
   `localizedTitle`
   `localizedDescription`
   `introductoryPrice`
   `introductoryPriceLocale`
   `introductoryPriceNumberOfPeriods`
   `numberOfUnits`
   `unit`
