## README - In-App Purchasing Sample Scenes - App Store - Family Sharing

This sample shows how to implement and use Apple's Family Sharing feature in your application.
The family sharing feature enables players to share non-consumable and subscription in-app purchases with members of
the same family group.

## Instructions to test this sample:

Unfortunately, Apple doesn't support family sharing on the Sandbox, Xcode testing, or TestFlight. The only way to test
the full feature is by using an app published on the app store.
However, it is possible to test the `AppleProductMetadata.isFamilyShareable` and
the `IAppleConfiguration.SetEntitlementsRevokedListener` API by using Xcode testing and refunding a purchase.
Even though it is hard to test this sample, it is still provided to showcase how to implement family sharing using Unity
IAP.

1. Have in-app purchasing correctly configured with
   the [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@3.2/manual/UnityIAPAppleConfiguration.html)
   .
2. Make sure you have a subscription with family sharing enabled.
3. Set your own product's id in
   the `InAppPurchasing game object > Family Sharing script > Family Shareable Subscription ProductId field`
   or change the `familyShareableSubscriptionProductId` the `FamilySharing.cs` script.
4. Publish your app to the Apple App Store.
5. To test, click `Buy Family Shared Subscription`. You should see your subscription status updated.
6. When you open the app with another phone that is sharing the same family, you should see that the
   account is subscribed. It may take several minutes before the purchase is updated by apple.
7. When you remove that account from the family, you should see a revoked notification and the subscription status
   updated.

See
Apple's [Family Sharing documentation](https://developer.apple.com/documentation/storekit/in-app_purchase/original_api_for_in-app_purchase/supporting_family_sharing_in_your_app)
for more information.
