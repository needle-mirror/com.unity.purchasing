using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.GoogleBilling.Models
{
    /// <summary>
    /// The details used to report transactions made outside of Google Play Billing.
    ///
    /// This is C# representation of the Java Class BillingProgramReportingDetails
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingProgramReportingDetails">See more</a>
    /// </summary>
    public struct BillingProgramReportingDetails
    {
        /// <summary>
        /// Returns the billing program that the reporting details are associated with.
        /// </summary>
        public GoogleBillingResponseCode responseCode;

        /// <summary>
        /// Returns an external transaction token that can be used to report a transaction made outside of Google Play Billing.
        /// </summary>
        public string externalTransactionToken;
    }
}
