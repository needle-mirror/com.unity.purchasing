using UnityEngine;

namespace UnityEngine.Purchasing.Models
{
    /// <summary>
    /// This is C# representation of the Java Class BillingResult
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingResult">See more</a>
    /// </summary>
    class GoogleBillingResult : IGoogleBillingResult
    {
        public GoogleBillingResponseCode responseCode { get; }
        public string debugMessage { get; }
        internal GoogleBillingResult(AndroidJavaObject billingResult)
        {
            if (billingResult != null)
            {
                responseCode = (GoogleBillingResponseCode)billingResult.Call<int>("getResponseCode");
                debugMessage = billingResult.Call<string>("getDebugMessage");
                // TODO: ULO-3790: getOnPurchasesUpdatedSubResponseCode
            }
        }

        // Synthetic result for code paths that need to short-circuit without
        // a real Java BillingResult (e.g. preflight failures before the JNI call).
        internal GoogleBillingResult(GoogleBillingResponseCode responseCode, string debugMessage)
        {
            this.responseCode = responseCode;
            this.debugMessage = debugMessage;
        }
    }
}
