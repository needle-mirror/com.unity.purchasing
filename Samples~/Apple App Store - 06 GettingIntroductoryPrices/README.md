## README - In-App Purchasing Sample Scenes - Apple App Store - Getting Introductory Prices

This sample showcases how to use Apple extensions to get introductory subscription offer information.

## Instructions to test this sample:

1. Have in-app purchasing correctly configured with
   the [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@3.2/manual/UnityIAPAppleConfiguration.html).
2. Configure a subscription product.
3. Follow [Set an introductory offer for an auto-renewable subscription](https://help.apple.com/app-store-connect/#/deve1d49254f) to set up introductory pricing for the subscription.
4. Set your own product's id in the `InAppPurchasing game object > Getting Introductory Prices script > Subscription Product Id field`
   or change the `subscriptionProductId` field in the `GettingIntroductoryPrices.cs` script.
5. Build your project for `iOS`.
   1. If you are using a simulator with Xcode 12+, follow these [instructions](https://developer.apple.com/documentation/xcode/setting-up-storekit-testing-in-xcode)
      to set up StoreKit Testing.

## Getting Introductory Prices

From [Apple's documentation](https://developer.apple.com/documentation/storekit/original_api_for_in-app_purchase/subscriptions_and_offers/implementing_introductory_offers_in_your_app):
> Apps with auto-renewable subscriptions can offer a discounted introductory price, including a free trial, to eligible users. You can make introductory offers to customers who haven’t previously received an introductory offer for the given product, or for any products in the same subscription group.

`IAppleExtension.GetIntroductoryPriceDictionary` returns a dictionary of JSON encoded strings keyed by productIds.

For more information about Apple's introductory pricing for subscriptions, see [Implementing Introductory Offers in Your App
](https://developer.apple.com/documentation/storekit/original_api_for_in-app_purchase/subscriptions_and_offers/implementing_introductory_offers_in_your_app).
