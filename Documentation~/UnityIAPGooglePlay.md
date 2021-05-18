Google Play
===========

Consumables
-----------

Unity IAP uses V3 of Google's Billing API, which features the concept of consumable products and explicit consumption API calls.

You should create all of your consumable products in the Google Publisher dashboard as 'Managed' products. Unity IAP will take care of consuming them when your application confirms a purchase.

Testing
-------

Before publishing your application you must test your in-app purchases on an Android device as an alpha or beta distribution.

Note that whilst your Alpha or Beta APK must be published to test your IAPs, this does not mean your App has to be publicly visible in the Google Play store.

In order to perform a complete end-to-end test of your in-app purchases, you must do so whilst signed into a device using a test account.

Please note the following:

* You must upload a signed, release version of your APK to Google Play that is published as an alpha or beta distribution.
* The version number of the APK that you upload must match the version number of the APK that you test with.
* After entering your purchase metadata into the Google Play publisher console, it may take up to 24 hours before you are able to purchase your in-app purchases using a test account.

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

public class GooglePlayInitializationDisconnectListener : IStoreListener
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
}
```
