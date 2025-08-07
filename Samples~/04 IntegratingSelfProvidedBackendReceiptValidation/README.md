README - In-App Purchasing Sample Scenes - Integrating Self-Provided Backend Receipt Validation

In this sample, you will learn how to integrate your own backend validation with Unity IAP by delaying calling `ConfirmPurchase`.

This sample uses a mock for the backend implementation. You can plug in your own backend by replacing
the `MockServerSideValidation` method in `IntegratingSelfProvidedBackendReceiptValidation.cs`.
For more information about how to do a web request in unity, see
the [documentation](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Networking.UnityWebRequest.Post.html).

This sample uses a fake store for its transactions, to use a real store like the App Store or the Google Play Store, you
would need to register your application and add in-app purchases.
Keep in mind that in this sample, product identifiers are kept in
the `IntegratingSelfProvidedBackendReceiptValidation.cs` file.

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
