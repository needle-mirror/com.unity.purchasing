## README - In-App Purchasing Sample Scenes - Apple App Store - Upgrading and Downgrading Subscriptions

This sample showcases how to use Unity IAP to upgrade and downgrade subscriptions. This allows
players to change their subscription, and pay a different amount of money for a different level of service.

You can offer users different subscription tiers, such as a base tier and a premium tier or monthly and yearly
subscriptions. A user that is already subscribed may be given the opportunity to pay a different amount of money to upgrade or
downgrade their subscription's tier to a different service level.

On Apple, the user purchases a subscription, and upgrades or downgrades by purchasing a second, or by visiting the [Manage Subscriptions](https://support.apple.com/en-us/HT204939) sheet.

## Instructions to test this sample:

1. Have in-app purchasing correctly configured with
   the [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@3.2/manual/UnityIAPAppleConfiguration.html).
2. Configure two subscriptions of different tiers in the Apple App Store, under the same subscription group. This can be as simple as having a "normal"
   subscription and a "VIP" subscription. Assign both of these products to a single [Subscription Group](https://help.apple.com/app-store-connect/#/dev75708c031).
3. Set your own product's id in
   the `InAppPurchasing game object > Upgrade Downgrade Subscription script > Normal Subscription Id field / Vip Subscription Id field`
   or change the `normalSubscriptionId` and `vipSubscriptionId` fields in the `UpgradeDowngradeSubscription.cs` script.
4. Build your project for `iOS`.

NOTE: Testing may be complicated and not convincing when using the Apple Sandbox app store. Only certain dialogs may appear, and there may be no "upgrade / downgrade" UI presented. Also the "Manage Subscriptions" Apple iOS feature may not work when testing with the Sandbox.

See
[Apple's documentation](https://help.apple.com/app-store-connect/#/dev75708c031)
on the topic for more information.
