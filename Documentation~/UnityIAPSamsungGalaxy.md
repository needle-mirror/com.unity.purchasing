# Samsung Galaxy apps

## Extended functionality

**Note**: The Samsung Galaxy store only apply to version 3.1.0 and earlier of the Unity In App Purchasing package and is now deprecated. Please use the [Unity Distribution Platform](https://docs.unity3d.com/2021.2/Documentation/Manual/udp.html) instead.

### Developer mode testing

Developer mode allows you to carry out IAP testing without incurring real-world monetary charges for products. To get started, create your configuration with an `ISamsungAppsConfiguration` instance, with its mode set to `SamsungAppsMode.AlwaysSucceed`:

````
var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

// Enable "developer mode" for purchases, not requiring real-world money

// SamsungAppsMode has: Production (developer mode "off"), AlwaysSucceed, AlwaysFail

builder.Configure<ISamsungAppsConfiguration>().SetMode(SamsungAppsMode.AlwaysSucceed);
````

### Restoring transactions

Users restore transactions to maintain access to content they’ve already purchased (for example, when they upgrade to a new phone, they don’t lose all of the items they purchased on the old phone). The Samsung Galaxy App Store does not require previous transactions to be restored. However, you could improve in-app usability by providing users with a button allowing them to restore their purchases, for instance if they have installed the app on a different device. 

During this process, the `ProcessPurchase` function of your `IStoreListener` is invoked for any items the user already owns. The following example illustrates such a call. This could be called from a **Restore Purchases** button:

````
/// <summary>

/// Your IStoreListener implementation of OnInitialized.

/// </summary>

public void OnInitialized(IStoreController controller, IExtensionProvider extensions)

{

    // The ProcessPurchase function is invoked for any items the user already owns

    extensions.GetExtension<ISamsungAppsExtensions>().RestoreTransactions(result => {

        if (result) {

            // This does not mean anything has been restored,

            // just that the restoration process succeeded.

        } else {

            // Restoration failed.

        }

    });

}
````

On Samsung Galaxy platforms, users may be required to input their Samsung Galaxy App Store password to retrieve previous transactions, if they haven’t already done so.

<!-- area:monetization -->

