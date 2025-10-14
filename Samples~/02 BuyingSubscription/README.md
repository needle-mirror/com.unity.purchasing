README - In-App Purchasing Sample Scenes - Buying Subscription

This sample demonstrates how to handle subscription purchases using Unity's entitlement system.
It shows how to check a user's subscription status and update the UI accordingly.
The sample relies on entitlements provided by the store to manage subscription status.

## Instructions to test this sample:

1. Configure In-App Purchasing for your target store (e.g., Apple App Store or Google Play Store).
2. Set up a subscription product in your store's dashboard.
3. Set your subscription product's ID in the `BuyingSubscription` script's `subscriptionProductId` field.
4. Build and run your project on a supported platform.

## Subscription Handling

When a user purchases a subscription, the sample checks the entitlement status for the configured product. 
If the user is fully entitled, the UI updates to indicate an active subscription. 

Users can restore their subscription status after reinstalling the app, 
as the entitlement system retrieves current ownership from the store.

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
