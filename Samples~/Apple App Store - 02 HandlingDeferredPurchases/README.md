## README - In-App Purchasing Sample Scenes - Apple App Store - Handling Deferred Purchases

This sample showcases how to handle deferred purchases
and test using `AppleStoreExtendedPurchaseService.simulateAskToBuy`.

## Instructions to test this sample:

1. If testing with Sandbox, have in-app purchasing correctly configured with
   the Apple App Store.
2. Configure a non-consumable product.
3. Set your own productID in the `InAppPurchasing game object > Refreshing App Receipt script > No Ads Product Id field`
   or change the `goldProductId` field in the `HandlingDeferredPurchases.cs` script.
4. Build your project for `iOS`.
   1. If you are using a simulator with Xcode 12+, follow these [instructions](https://developer.apple.com/documentation/xcode/setting-up-storekit-testing-in-xcode)
      to set up StoreKit Testing. (We recommend using Storekit Testing if possible. Testing in
      Sandbox will not allow you to approve or reject Ask to Buy transactions.)
   2. In order to test deferred purchases using Storekit Testing, select Editor > Enable Ask to Buy.
      To approve and decline transactions, navigate to Debug > StoreKit > Manage Transactions.
      Right click on pending transactions to approve or decline.

## Apple Deferred Purchases and Ask to Buy

To learn more about Ask to Buy, please see [Request and make purchases with Ask to Buy](https://support.apple.com/en-us/HT201089) from Apple.

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
