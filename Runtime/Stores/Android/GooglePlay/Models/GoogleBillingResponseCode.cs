namespace UnityEngine.Purchasing.Models
{
    /// <summary>
    /// Values from Java Class BillingResponseCode
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient.BillingResponseCode">See more</a>
    /// </summary>
    enum GoogleBillingResponseCode
    {
        ServiceTimeout = -3,
        FeatureNotSupported = -2,
        ServiceDisconnected = -1,
        Ok = 0,
        UserCanceled = 1,
        ServiceUnavailable = 2,
        BillingUnavailable = 3,
        ItemUnavailable = 4,
        DeveloperError = 5,
        FatalError = 6,
        ItemAlreadyOwned = 7,
        ItemNotOwned = 8,
        NetworkError = 12
    }
}
