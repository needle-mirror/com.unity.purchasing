Handling purchase failures
====================

Purchases may fail for a number of reasons, including network failure, payment failure or device settings. You may wish to check the reason for a purchase failure and prompt the user to take action, though note that not all stores provide fine-grained failure information.

````
/// <summary>
/// Called when a purchase fails.
/// IStoreListener.OnPurchaseFailed is deprecated,
/// use IDetailedStoreListener.OnPurchaseFailed instead.
/// </summary>
public void OnPurchaseFailed (Product i, PurchaseFailureReason p)
{
    if (p == PurchaseFailureReason.PurchasingUnavailable) {
        // IAP may be disabled in device settings.
    }
}

/// <summary>
/// Called when a purchase fails.
/// </summary>
public void OnPurchaseFailed (Product i, PurchaseFailureDescription p)
{
    if (p.reason == PurchaseFailureReason.PurchasingUnavailable) {
        // IAP may be disabled in device settings.
    }
}
````
