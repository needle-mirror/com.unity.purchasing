## README - In-App Purchasing Sample Scenes - App Store - Promoting Products

This sample showcases how to use the Apple App Store extensions to Promote Products and intercept Promotional Purchases with
`IAppleExtensions.SetStorePromotionOrder`, `IAppleExtensions.SetStorePromotionVisibility`, `IAppleExtensions.ContinuePromotionalPurchases`,
and `IAppleConfiguration.SetApplePromotionalPurchaseInterceptorCallback`.

## Instructions to test this sample:

1. Have in-app purchasing correctly configured with
   the [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@3.2/manual/UnityIAPAppleConfiguration.html).
2. Configure a non-consumable product.
3. Set your own product's id in the `InAppPurchasing game object > Promoting Products script > No Ads Product Id field`
   or change the `noAdsProductId` field in the `PromotingProducts.cs` script.
4. Build your project for `iOS`.
   1. If you are using a simulator with Xcode 12+, follow these [instructions](https://developer.apple.com/documentation/xcode/setting-up-storekit-testing-in-xcode)
   to set up StoreKit Testing.
      
## Promoting Products

From [Apple's Documentation](https://developer.apple.com/app-store/promoting-in-app-purchases/):

> With iOS, users can browse in-app purchases directly on the App Store and start a purchase
> even before downloading your app. Promoted in-app purchases appear on your product page,
> can display in search results, and may be featured on the Today, Games, and Apps tabs.

For more information, see [Promoting In-App Purchases](https://developer.apple.com/documentation/storekit/original_api_for_in-app_purchase/promoting_in-app_purchases?language=objc),
and [Testing Promoted In-App Purchases](https://developer.apple.com/documentation/storekit/original_api_for_in-app_purchase/testing_promoted_in-app_purchases?language=objc)
from Apple's documentation.
