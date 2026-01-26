namespace UnityEngine.Purchasing.GoogleBilling.Models
{
    /// <summary>
    /// Values from Java class LaunchExternalLinkParams.LaunchMode
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/LaunchExternalLinkParams.LaunchMode#LAUNCH_IN_EXTERNAL_BROWSER_OR_APP()"/>
    /// </summary>
    public enum LaunchMode
    {
        LAUNCH_MODE_UNSPECIFIED = 0,
        LAUNCH_IN_EXTERNAL_BROWSER_OR_APP = 1,
        CALLER_WILL_LAUNCH_LINK = 2
    }
}

