Extensions and Configuration
====================

Extended functionality
----------------------

### Reading the App Receipt

An [App Receipt](https://developer.apple.com/library/ios/releasenotes/General/ValidateAppStoreReceipt/Chapters/ValidateRemotely.html) is stored on the device's local storage and can be read as follows:

````
var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
string receipt = builder.Configure<IAppleConfiguration>().appReceipt;
````

### Checking for payment restrictions

In App Purchases may be restricted in a device's settings, which can be checked for as follows:

````
var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
bool canMakePayments = builder.Configure<IAppleConfiguration>().canMakePayments;
````

### Restoring transactions

On Apple platforms users must enter their password to retrieve previous transactions so your application must provide users with a button letting them do so. During this process the `ProcessPurchase` method of your `IStoreListener` will be invoked for any items the user already owns.

````
/// <summary>
/// Your IStoreListener implementation of OnInitialized.
/// </summary>
public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
{
    extensions.GetExtension<IAppleExtensions> ().RestoreTransactions ((result, error) => {
        if (result) {
            // This does not mean anything was restored,
            // merely that the restoration process succeeded.
        } else {
            // Restoration failed. `error` contains the failure reason.
        }
    });
}
````

### Refreshing the App Receipt

Apple provides a mechanism to fetch a new App Receipt from their servers, typically used when no receipt is currently cached in local storage; [SKReceiptRefreshRequest](https://developer.apple.com/library/ios/documentation/StoreKit/Reference/SKReceiptRefreshRequest_ClassRef/index.html#/apple_ref/occ/cl/SKReceiptRefreshRequest).

Note that this will prompt the user for their password.

Unity IAP makes this method available as follows:

````
/// <summary>
/// Your IStoreListener implementation of OnInitialized.
/// </summary>
public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
{
    extensions.GetExtension<IAppleExtensions> ().RefreshAppReceipt (receipt => {
        // This handler is invoked if the request is successful.
        // Receipt will be the latest app receipt.
        Console.WriteLine(receipt);
    },
    () => {
        // This handler will be invoked if the request fails,
        // such as if the network is unavailable or the user
        // enters the wrong password.
    });
}
````

### Ask to Buy

iOS 8 introduced a new parental control feature called [Ask to Buy](https://developer.apple.com/library/ios/technotes/tn2259/_index.html#/apple_ref/doc/uid/DTS40009578-CH1-UPDATE_YOUR_APP_FOR_ASK_TO_BUY).

Ask to Buy purchases defer for parent approval. When this occurs, Unity IAP sends your app a notification as follows:

````
/// This is called when Unity IAP has finished initialising.
public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
{
    extensions.GetExtension<IAppleExtensions>().RegisterPurchaseDeferredListener(product => {
        Console.WriteLine(product.definition.id);
    });
}
````

#### Enable "Ask-to-Buy" simulation in the Sandbox App Store

The sample class below demonstrates how to access `IAppleExtensions` in order to enable Ask-to-Buy simulation in the Sandbox App Store:

```
using UnityEngine;
using UnityEngine.Purchasing;

public class AppleSimulateAskToBuy : MonoBehaviour {
    public void SetSimulateAskToBuy(bool shouldSimulateAskToBuy) {
        if (Application.platform == RuntimePlatform.IPhonePlayer) {
            IAppleExtensions extensions = IAPButton.IAPButtonStoreManager.Instance.ExtensionProvider.GetExtension<IAppleExtensions>();
            extensions.simulateAskToBuy = shouldSimulateAskToBuy;
        }
    }
}
```

When the purchase is approved or rejected, your store's normal `ProcessPurchase` or `OnPurchaseFailed` listener methods are invoked.

## Intercepting Apple promotional purchases
Apple allows you to promote [in-game purchases](https://developer.apple.com/app-store/promoting-in-app-purchases/#:~:text=inside%20your%20app.-,Overview,approved%20and%20ready%20to%20promote.&text=When%20a%20user%20doesn't,to%20download%20the%20app%20first.) through your app’s product page. Unlike conventional in-app purchases, Apple promotional purchases initiate directly from the App Store on iOS and tvOS. The App Store then launches your app to complete the transaction, or prompts the user to download the app if it isn’t installed.

The `IAppleConfiguration` `SetApplePromotionalPurchaseInterceptor` callback method intercepts Apple promotional purchases. Use this callback to present parental gates, send analytics events, or perform other functions before sending the purchase to Apple. The callback uses the `Product` that the user attempted to purchase. You must call `IAppleExtensions.ContinuePromotionalPurchases()` to continue with the promotional purchase. This will initiate any queued-up payments.

If you do not set the callback, promotional purchases go through immediately and call `ProcessPurchase` with the result.

**Note**: Calling these APIs on other platforms has no effect.

```
private IAppleExtensions m_AppleExtensions;

public void Awake() {
    var module = StandardPurchasingModule.Instance();
    var builder = ConfigurationBuilder.Instance(module);
        // On iOS and tvOS we can intercept promotional purchases that come directly from
        // the App Store.
        // On other platforms this will have no effect; OnPromotionalPurchase will never be
        // called.
    builder.Configure<IAppleConfiguration>().
     SetApplePromotionalPurchaseInterceptorCallback(OnPromotionalPurchase);
    Debug.Log("Setting Apple promotional purchase interceptor callback");
}

public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
    m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();
    foreach (var item in controller.products.all) {
        if (item.availableToPurchase) {
            // Set all these products to be visible in the user's App Store
            m_AppleExtensions.SetStorePromotionVisibility(item, AppleStorePromotionVisibility.Show);
        }
    }
}

private void OnPromotionalPurchase(Product item) {
    Debug.Log("Attempted promotional purchase: " + item.definition.id);
    // Promotional purchase has been detected.
    // Handle this event by, e.g. presenting a parental gate.
    // Here, for demonstration purposes only, we will wait five seconds before continuing
    // the purchase.
    StartCoroutine(ContinuePromotionalPurchases());
}

private IEnumerator ContinuePromotionalPurchases() {
    Debug.Log("Continuing promotional purchases in 5 seconds");
    yield return new WaitForSeconds(5);
    Debug.Log("Continuing promotional purchases now");
    m_AppleExtensions.ContinuePromotionalPurchases (); // iOS and tvOS only
}

```
