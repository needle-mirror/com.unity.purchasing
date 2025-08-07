## README - In-App Purchasing Sample Scenes - Apple App Store - Fraud Detection

This sample showcases how to provide to the Apple App Store your user's identifiers to help prevent fraud using
Apple extensions.

## Instructions to test this sample:

1. Have In-App Purchasing correctly configured with
   the Apple App Store.
2. Configure a product.
3. Set your own product's id in the `InAppPurchasing game object > Fraud Detection script > Gold Product Id field`
   or change the `goldProductId` field in the `FraudDetection.cs` script.
4. Set a username in the InAppPurchasing game object > Fraud Detection script > User > Username field.
5. Build your project for `iOS`.
   1. If you are using a simulator with Xcode 12+, follow these [instructions](https://developer.apple.com/documentation/xcode/setting-up-storekit-testing-in-xcode)
      to set up StoreKit Testing.

## Apple App Store Fraud Detection

To help prevent fraud, it is useful to provide to Apple an in-app identifier of your user. This helps Apple
map iTunes Store accounts to their in-app account.

The username must not contain personally identifiable information such as emails in cleartext. To prevent
this, Apple recommends that you use either encryption or a one-way hash to generate an obfuscated identifier.

For more information see [Apple's documentation](https://developer.apple.com/documentation/appstoreserverapi/appaccounttoken/) on
the subject.

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
