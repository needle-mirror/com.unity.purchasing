## README - In-App Purchasing Sample Scenes - App Store - Present Code Redemption Sheet

This sample showcases how to use the Apple App Store extensions for users to redeem subscription offer codes. This allows developers to
re-engage with their users by the distribution of out-of-app subscription offers.

## Instructions to test this sample:

1. Have in-app purchasing correctly configured with
   the [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@3.2/manual/UnityIAPAppleConfiguration.html).
2. Configure a subscription product.
    1. Configure an [Offer Code on Apple App Store](https://help.apple.com/app-store-connect/#/dev6a098e4b1) for this subscription.
3. Set your own product's id in the `InAppPurchasing game object > Refreshing App Receipt script > Normal Subscription Id field`
   or change the `normalSubscriptionId` field in the `PresentCodeRedemptionSheet.cs` script.
4. Build your project for `iOS`.
   1. Testing on the Unity Editor will not display the offer code sheet.
   2. Testing on the Apple simulator will not display the offer code sheet, by Apple's StoreKit v1 design.
   3. Testing on the Apple SANDBOX can display the offer code sheet UI. The SANDBOX Apple App Store server does not support end-to-end fulfillment of the code, however.
   4. Testing on the PRODUCTION Apple App Store server allows end-to-end testing. See below for more information.

## Present Code Redemption Sheet

Using `PresentRedemptionSheet` on a device, including on the Sandbox, will prompt the user to enter an offer code.

See the documentation for
[the Unity IAP extension to present the code redemption sheet](http://docs.unity3d.com/Packages/com.unity.purchasing@4.0/api/UnityEngine.Purchasing.IAppleExtensions.html#UnityEngine_Purchasing_IAppleExtensions_PresentCodeRedemptionSheet). Also see the relevant [Apple API documentation](https://developer.apple.com/documentation/storekit/original_api_for_in-app_purchase/subscriptions_and_offers/implementing_offer_codes_in_your_app) for more context on this feature.
And see the [iOS & Mac App Stores document](https://docs.unity3d.com/Packages/com.unity.purchasing@4.0/manual/UnityIAPiOSMAS.html)
for setting up an Apple project with Unity IAP.

See [this tip for testing offer codes end-to-end](https://developer.apple.com/forums/thread/70426), using the PRODUCTION Apple App Store server.
