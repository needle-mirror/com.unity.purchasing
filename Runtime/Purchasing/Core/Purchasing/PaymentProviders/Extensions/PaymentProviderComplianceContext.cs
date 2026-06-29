#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Information passed to the developer-supplied compliance callback registered via
    /// <see cref="IPaymentProvidersExtendedPurchaseService.SetComplianceCheck"/>.
    /// The callback runs before Unity creates a Payment Provider order, so no platform-specific
    /// redirect URL is available yet.
    /// </summary>
    public class PaymentProviderComplianceContext
    {
        /// <summary>
        /// The cart the developer asked to purchase.
        /// </summary>
        public ICart Cart { get; }

        internal PaymentProviderComplianceContext(ICart cart)
        {
            Cart = cart;
        }
    }
}
