namespace Stores.Android.GooglePlay.AAR.Models
{
    // From <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient.BillingProgram?hl=en"/>
    public enum BillingProgram
    {
        UNSPECIFIED_BILLING_PROGRAM = 0,
        EXTERNAL_CONTENT_LINK = 1,
        EXTERNAL_OFFER = 3,
        EXTERNAL_PAYMENTS = 4
    }
}
