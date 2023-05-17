# Extensions and Configuration

Consumables
-----------

Unity IAP uses V4 of Google's Billing API, which features the concept of consumable products and explicit consumption API calls.

When you create consumable products in the Google Publisher dashboard, set them to be 'Managed' products. Unity IAP will take care of consuming them after your application has confirmed a purchase.

Extended functionality
----------------------

### Listen for recoverable initialization interruptions

A game may not complete initializing Unity IAP, either successfully or unsuccessfully, in certain circumstances. This can be due to the user having no Google account added to their Android device when the game initializes Unity IAP.

For example: a user first installs the app with the Play Store. Then the user removes their Google account from the device. The user launches the game and Unity IAP does not finish initializing, preventing the user from purchasing or restoring any prior purchases. To fix this, the user can [add a Google account](https://support.google.com/android/answer/7664951) to their device and return to the game.

The `IGooglePlayConfiguration.SetServiceDisconnectAtInitializeListener(Action)` API can be used to listen for this scenario. When this Action is triggered, the game may choose to advise the user through a user interface dialog that a Google account is required for purchasing and restoring prior purchases.

Please refer to this usage sample:

```
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class GooglePlayInitializationDisconnectListener : IDetailedStoreListener
{
    public GooglePlayInitializationDisconnectListener()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.Configure<IGooglePlayConfiguration>().SetServiceDisconnectAtInitializeListener(() =>
        {
            Debug.Log("Unable to connect to the Google Play Billing service. " +
                "User may not have a Google account on their device.");
        });
        builder.AddProduct("100_gold_coins", ProductType.Consumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions) { }

    public void OnInitializeFailed(InitializationFailureReason error) { }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product i, PurchaseFailureReason p) { }

    public void OnPurchaseFailed(Product i, PurchaseFailureDescription p) { }
}
```

### Listen for failed query product details

Querying product details from the Google Play Store can fail due to certain circumstances. When this happens, we retry until successful.

For example: a user first installs the app with the Play Store. Then the user launches the app without having Internet access. The Google Play Store will be unavailable because it requires an Internet connection which will result in failing to query product details. Restoring the Internet connection will fix the problem and the app will resume correctly.

The `IGooglePlayConfiguration.SetQueryProductDetailsFailedListener(Action<int>)` API can be used to listen for this scenario. The action has a parameter which contains the retry count. When this Action is triggered, the app may choose to advise the user through a user interface dialog to verify their Internet connection.

Please refer to this usage sample:

```
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class QueryProductDetailsFailedListener : IDetailedStoreListener
{
    public QueryProductDetailsFailedListener()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.Configure<IGooglePlayConfiguration>().SetQueryProductDetailsFailedListener((int retryCount) =>
        {
            Debug.Log("Failed to query product details " + retryCount + " times.");
        });
        builder.AddProduct("100_gold_coins", ProductType.Consumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions) { }

    public void OnInitializeFailed(InitializationFailureReason error) { }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product i, PurchaseFailureReason p) { }

    public void OnPurchaseFailed(Product i, PurchaseFailureDescription p) { }
}
```
