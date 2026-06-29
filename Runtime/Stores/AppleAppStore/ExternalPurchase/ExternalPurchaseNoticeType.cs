#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Notice types for Apple's ExternalPurchaseCustomLink API.
    /// Determines how the external purchase link will be opened after showing the notice.
    /// </summary>
    public enum ExternalPurchaseNoticeType
    {
        /// <summary>
        /// Opens the external purchase link in Safari (external browser).
        /// </summary>
        Browser,

        /// <summary>
        /// Opens the external purchase link in a web view within the app.
        /// </summary>
        WithinApp
    }
}
