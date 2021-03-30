Initialization
==============

You must provide an implementation of the ``IStoreListener`` interface which Unity IAP uses to inform your application of purchase-related events.

Call the ``UnityPurchasing.Initialize`` method to start the initialization process, supplying your listener implementation and configuration.

Note that initialization will not fail if the network is unavailable; Unity IAP will continue attempting to initialize in the background. Initialization will only fail if Unity IAP encounters an unrecoverable problem such as a misconfiguration or IAP being disabled in device settings.

Consequently Unity IAP may take an arbitrary period of time to initialize; indefinitely if the user is in airplane mode. You should design your store accordingly by preventing users from attempting to make purchases if initialization has not completed successfully.

````
using UnityEngine;
using UnityEngine.Purchasing;

public class MyIAPManager : IStoreListener {

    private IStoreController controller;
    private IExtensionProvider extensions;

    public MyIAPManager () {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct("100_gold_coins", ProductType.Consumable, new IDs
        {
            {"100_gold_coins_google", GooglePlay.Name},
            {"100_gold_coins_mac", MacAppStore.Name}
        });

        UnityPurchasing.Initialize (this, builder);
    }

    /// <summary>
    /// Called when Unity IAP is ready to make purchases.
    /// </summary>
    public void OnInitialized (IStoreController controller, IExtensionProvider extensions)
    {
        this.controller = controller;
        this.extensions = extensions;
    }

    /// <summary>
    /// Called when Unity IAP encounters an unrecoverable initialization error.
    ///
    /// Note that this will not be called if Internet is unavailable; Unity IAP
    /// will attempt initialization until it becomes available.
    /// </summary>
    public void OnInitializeFailed (InitializationFailureReason error)
    {
    }

    /// <summary>
    /// Called when a purchase completes.
    ///
    /// May be called at any time after OnInitialized().
    /// </summary>
    public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e)
    {
        return PurchaseProcessingResult.Complete;
    }

    /// <summary>
    /// Called when a purchase fails.
    /// </summary>
    public void OnPurchaseFailed (Product i, PurchaseFailureReason p)
    {
    }
}
````

