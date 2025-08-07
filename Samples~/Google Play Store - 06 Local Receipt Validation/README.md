## README - In-App Purchasing Sample Scenes - Local Validation for Google Play Store

This sample showcases how to use the cross-platform validator to do local receipt validation with Google Play Store receipts.

## Local validation

**Important:** While Unity IAP provides a local validation method, local validation is more vulnerable to fraud.
Validating sensitive transactions server-side where possible is considered best practice. For more information, please
see [Google Play’s](https://developer.android.com/google/play/billing/security) documentation on fraud prevention.

If the content that the user is purchasing already exists on the device, the application simply needs to make a decision
about whether to unlock it.

Unity IAP provides tools to help you hide content and to validate and parse receipts through Google Play.

For more information, see the [documentation](https://docs.unity3d.com/Manual/UnityIAPValidatingReceipts.html) on the
topic.

## Instructions to test this sample:

1. Have In-App Purchasing correctly configured with
   the Google Play Store.
2. Set your own product's id in
   the `InAppPurchasing game object > Local Receipt Validation script > Gold Product Id field`
   or change the `GoldProductId` value in the `LocalReceiptValidation.cs` script.
3. This sample uses the `GooglePlayTangle` class. To generate these classes in your project, do the
   following:
    1. Get your license key from the [Google Play Developer Console](https://play.google.com/apps/publish/).
        1. Select your app from the list.
        2. Go to "Monetization setup" under "Monetize".
        3. Copy the key from the "Licensing" section.
    2. Open the obfuscation window from `Services > In-App Purchasing > Receipt Validation Obfuscator`.
    3. Paste your Google Play key.
    4. Obfuscate the key. (Creates `GooglePlayTangle` class in your project, along with `AppleTangle`, and `AppleStoreKitTestTangle`.)
    5. (Optional) To ensure correct revenue data, enter your key in the Analytics dashboard.
4. Add the sample scene to the build settings in the `File > Build Settings` window

###### *Make sure the `GooglePlayStore` is selected. You can change the currently selected store under `Services > In-App Purchasing > Configure` and changing the `Current Targeted Store` field.

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
