# Receipt Obfuscation

If the content that the user is purchasing already exists on the device, the application simply needs to make a decision about whether to unlock it.

Unity IAP provides tools to help you hide unpurchased content and to validate and parse receipts through Google Play and Apple stores.

## Obfuscating encryption keys

Receipt validation is performed using known encryption keys. For your application, this is an encrypted Google Play public key, and/or Apple's certificates.

If a user can replace these, they can defeat your receipt validation checks, so it is important to make it difficult for a user to easily find and modify these keys.

Unity IAP provides a tool that can help you obfuscate your encryption keys within your Application. This confuses or jumbles the keys so that it is much harder for a user to acces them.

* For Unity 2021 or older: In the Unity menu bar, go to __Services__ > __In-App Purchasing__ > __IAP Receipt Validation Obfuscator__.
![The Obfuscator window](images/IAPObfuscator.png)
* For more recent versions: In the Project Settings for In-App Purchasing, under Receipt Obfuscator. Note that if you have followed the steps in the [Google Public Key Guide](GooglePublicKey.md) entered your Google Play public key in the dashboard, this text field may already be populated with it.
![The Obfuscator setting](images/IAPObfuscatorServiceSettings.png)

This window encodes Apple's root certificate, [StoreKit Test certificate](https://developer.apple.com/documentation/Xcode/setting-up-storekit-testing-in-xcode) (which are bundled with Unity IAP) and your Google Play public key (copied by you from the application's [Google Play Developer Console's Services &amp; APIs](https://developer.android.com/google/play/licensing/setting-up.html) page) into different C# classes: __AppleTangle__, __AppleStoreKitTestTangle__, and __GooglePlayTangle__. These are added to your project for use in the next section.

Note that you do not have to provide a Google Play public key if you are only targeting Apple's stores, and vice versa.

## Validating receipts

Use the `CrossPlatformValidator` class for validation across both Google Play and Apple stores.

You must supply this class with either your Google Play public key or one of Apple's certificates, or both if you wish to validate across both platforms. Note that you cannot supply both Apple root and ["StoreKit Test"](https://developer.apple.com/documentation/Xcode/setting-up-storekit-testing-in-xcode)(*) certificates, and instead should pass only one, choosing that with a run-time or build-time switch.

The `CrossPlatformValidator` performs two checks:

* Receipt authenticity is checked via signature validation.
* The application bundle identifier on the receipt is compared to the one in your application. An **InvalidBundleId** exception is thrown if they do not match.

Note that the validator only validates receipts generated on Google Play and Apple platforms. Receipts generated on any other platform, including fakes generated in the Editor, throw an __IAPSecurityException__.

Be sure that your `CrossPlatformValidator` object has been created in time for processing your purchases. Note that during the initialization of Unity IAP, it is possible that pending purchases from previous sessions may be fetched from the store and processed. If you are using a persistent object of this type, create it before initializing Unity IAP.

If you try to validate a receipt for a platform that you haven't supplied a secret key for, a __MissingStoreSecretException__ is thrown.

````
public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e)
{
    bool validPurchase = true; // Presume valid for platforms with no R.V.

    // Unity IAP's validation logic is only included on these platforms.
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
    // Prepare the validator with the secrets we prepared in the Editor
    // obfuscation window.
    var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
        AppleTangle.Data(), Application.identifier);

    try {
        // On Google Play, result has a single product ID.
        // On Apple stores, receipts contain multiple products.
        var result = validator.Validate(e.purchasedProduct.receipt);
        // For informational purposes, we list the receipt(s)
        Debug.Log("Receipt is valid. Contents:");
        foreach (IPurchaseReceipt productReceipt in result) {
            Debug.Log(productReceipt.productID);
            Debug.Log(productReceipt.purchaseDate);
            Debug.Log(productReceipt.transactionID);
        }
    } catch (IAPSecurityException) {
        Debug.Log("Invalid receipt, not unlocking content");
        validPurchase = false;
    }
#endif

    if (validPurchase) {
        // Unlock the appropriate content here.
    }

    return PurchaseProcessingResult.Complete;
}

````

### Choose an Apple certificate: Apple Root or StoreKit Test

(*)  Unity IAP supports receipt validation of purchases made with the StoreKit Test store simulation.

Apple's Xcode 12 offers the ["StoreKit Test"](https://developer.apple.com/documentation/Xcode/setting-up-storekit-testing-in-xcode) suite of features for developers to more conveniently test IAP, without the need to use an Apple App Store Connect Sandbox configuration.

Use the `AppleStoreKitTestTangle` class in place of the usual `AppleTangle` class, when constructing the `CrossPlatformValidator` for receipt validation. Note that both tangle classes are generated by the **Receipt Validation Obfuscator**.

````
public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e)
{
    bool validPurchase = true;

#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
    // Choose one Apple certificate. NOTE AppleStoreKitTestTangle requires
    // the active Xcode Scheme set to use a StoreKit Configuration file.
    // Here we use a symbol, defined either in code or Project Settings >
    // Player > Scripting Define Symbols, to choose which Apple IAP system
    // we intend to test with in Xcode, next.

#if !DEBUG_STOREKIT_TEST
    var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
        AppleTangle.Data(), Application.identifier);
#else
    var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
        AppleStoreKitTestTangle.Data(), Application.identifier);
#endif

    try {
        validator.Validate(e.purchasedProduct.receipt);
    } catch (IAPSecurityException) {
        validPurchase = false;
    }
#endif

    if (validPurchase) { }

    return PurchaseProcessingResult.Complete;
}

````


### Deep validation

It is important you check not just that the receipt is valid, but also what information it contains. A common technique by users attempting to access content without purchase is to supply receipts from other products or applications. These receipts are genuine and do pass validation, so you should make decisions based on the product IDs parsed by the __CrossPlatformValidator__.

## Store-specific details

Different stores have different fields in their purchase receipts. To access store-specific fields, `IPurchaseReceipt` can be downcast to two different subtypes: `GooglePlayReceipt` and `AppleInAppPurchaseReceipt`.

````
var result = validator.Validate(e.purchasedProduct.receipt);
Debug.Log("Receipt is valid. Contents:");
foreach (IPurchaseReceipt productReceipt in result) {
    Debug.Log(productReceipt.productID);
    Debug.Log(productReceipt.purchaseDate);
    Debug.Log(productReceipt.transactionID);

    GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
    if (null != google) {
        // This is Google's Order ID.
        // Note that it is null when testing in the sandbox
        // because Google's sandbox does not provide Order IDs.
        Debug.Log(google.transactionID);
        Debug.Log(google.purchaseState);
        Debug.Log(google.purchaseToken);
    }

    AppleInAppPurchaseReceipt apple = productReceipt as AppleInAppPurchaseReceipt;
    if (null != apple) {
        Debug.Log(apple.originalTransactionIdentifier);
        Debug.Log(apple.subscriptionExpirationDate);
        Debug.Log(apple.cancellationDate);
        Debug.Log(apple.quantity);
    }
}
````

## Parsing raw Apple receipts

Use the `AppleValidator` class to extract detailed information about an Apple receipt. Note that this class only works with iOS App receipts from version 7.0 onwards, not Apple's deprecated transaction receipts.

````
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
// Get a reference to IAppleConfiguration during IAP initialization.
var appleConfig = builder.Configure<IAppleConfiguration>();
var receiptData = System.Convert.FromBase64String(appleConfig.appReceipt);
AppleReceipt receipt = new AppleValidator(AppleTangle.Data()).Validate(receiptData);

Debug.Log(receipt.bundleID);
Debug.Log(receipt.receiptCreationDate);
foreach (AppleInAppPurchaseReceipt productReceipt in receipt.inAppPurchaseReceipts) {
    Debug.Log(productReceipt.transactionIdentifier);
    Debug.Log(productReceipt.productIdentifier);
}
#endif
````

The `AppleReceipt` type models Apple's ASN1 receipt format. See [Apple's documentation](https://developer.apple.com/library/ios/releasenotes/General/ValidateAppStoreReceipt/Chapters/ReceiptFields.html#/apple_ref/doc/uid/TP40010573-CH106-SW1) for an explanation of its fields.
