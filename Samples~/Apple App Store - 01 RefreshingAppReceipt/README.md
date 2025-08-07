## README - In-App Purchasing Sample Scenes - App Store - Refreshing App Receipts

This sample showcases how to use the Apple Store Extended Purchase Service to refresh app receipts. This allows
developers to manually check for new purchases without creating new transactions, in contrast to RestoreTransactions.

## Instructions to test this sample:

1. Have In-App Purchasing correctly configured with
   the Apple App Store.
2. Configure a non-consumable product.
3. Set your own product's id in the `InAppPurchasing game object > Refreshing App Receipt script > No Ads Product Id field`
   or change the `noAdsProductId` field in the `RefreshingAppReceipt.cs` script.
4. Build your project for `iOS`.
   1. If you are using a simulator with Xcode 12+, follow these [instructions](https://developer.apple.com/documentation/xcode/setting-up-storekit-testing-in-xcode)
   to set up StoreKit Testing.

## Refreshing App Receipts

Using `RefreshAppReceipt` will prompt the user to enter their Apple login password.

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
