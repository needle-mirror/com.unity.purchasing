# Receipt Obfuscation

If the content that the user is purchasing already exists on the device, the application simply needs to make a decision about whether to unlock it.

Unity IAP provides tools to help you hide unpurchased content and to validate and parse receipts through Google Play.

## Obfuscating encryption keys

Receipt validation is performed using known encryption keys. For your application, this is an encrypted Google Play public key.

If a user can replace these, they can defeat your receipt validation checks, so it is important to make it difficult for a user to easily find and modify these keys.

Unity IAP provides a tool that can help you obfuscate your encryption keys within your Application. This confuses or jumbles the keys so that it is much harder for a user to acces them.

* For Unity 2021 or older: In the Unity menu bar, go to __Services__ > __In-App Purchasing__ > __IAP Receipt Validation Obfuscator__.
![The Obfuscator window](images/IAPObfuscator.png)
* For more recent versions: In the Project Settings for In-App Purchasing, under Receipt Obfuscator. Note that if you have followed the steps in the [Google Public Key Guide](GooglePublicKey.md) entered your Google Play public key in the dashboard, this text field may already be populated with it.
![The Obfuscator setting](images/IAPObfuscatorServiceSettings.png)

This window encodes your Google Play public key (copied by you from the application's [Google Play Developer Console's Services &amp; APIs](https://developer.android.com/google/play/licensing/setting-up.html) page) into a C# class: __GooglePlayTangle__. This is added to your project for use in the next section.

## Validating receipts

Use the `CrossPlatformValidator` class for validation across both Google Play.

You must supply this class with your Google Play public key.

The `CrossPlatformValidator` performs two checks:

* Receipt authenticity is checked via signature validation.
* The application bundle identifier on the receipt is compared to the one in your application. An **InvalidBundleId** exception is thrown if they do not match.

Note that the validator only validates receipts generated on Google Play platform. Receipts generated on any other platform, including fakes generated in the Editor, throw an __IAPSecurityException__.

Be sure that your `CrossPlatformValidator` object has been created in time for processing your purchases. Note that during the initialization of Unity IAP, it is possible that pending purchases from previous sessions may be fetched from the store and processed. If you are using a persistent object of this type, create it before initializing Unity IAP.

If you try to validate a receipt for a platform that you haven't supplied a secret key for, a __MissingStoreSecretException__ is thrown.

````
public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e)
{
    bool validPurchase = true; // Presume valid for platforms with no R.V.

    // Unity IAP's validation logic is only included on Android.
#if UNITY_ANDROID
    // Prepare the validator with the secrets we prepared in the Editor
    // obfuscation window.
    var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
        Application.identifier);

    try {
        // On Google Play, result has a single product ID.
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

### Deep validation

It is important you check not just that the receipt is valid, but also what information it contains. A common technique by users attempting to access content without purchase is to supply receipts from other products or applications. These receipts are genuine and do pass validation, so you should make decisions based on the product IDs parsed by the __CrossPlatformValidator__.

## Store-specific details

Different stores have different fields in their purchase receipts. To access store-specific fields, `IPurchaseReceipt` can be downcast to: `GooglePlayReceipt`.

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
}
````
