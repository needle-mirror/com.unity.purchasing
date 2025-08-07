## README - In-App Purchasing Sample Scenes - Apple App Store - Retrieve JWS Representation

This sample showcases how to retrieve the JWS Representation from the Apple App Store returned order.

## Instructions to test this sample:

1. If testing with Sandbox, have In-App Purchasing correctly configured with
   the Apple App Store.
2. Configure a consumable product.
3. Set your own product's id in the `InAppPurchasing game object > Retrieving JWS Documentation script > Gold Product Id field`
   or change the `goldProductId` field in the `RetrievingJwsDocumentation.cs` script.
4. Build your project for `iOS`.
    1. If you are using a simulator with Xcode 12+, follow these [instructions](https://developer.apple.com/documentation/xcode/setting-up-storekit-testing-in-xcode)
       to set up StoreKit Testing.

### Getting Order JWS Representation

For additional information, see the JWS Representation section of the [Apple App Store Documentation](https://developer.apple.com/documentation/storekit/verificationresult/jwsrepresentation-21vgo).

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
