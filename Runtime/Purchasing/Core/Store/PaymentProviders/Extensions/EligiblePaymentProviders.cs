#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Result of <see cref="IPaymentProvidersExtendedService.GetEligiblePaymentProviders"/>.
    /// Bundles the priority-ordered eligible-providers list with a server-driven killswitch
    /// for the payment-options popup UI.
    /// </summary>
    public class EligiblePaymentProviders
    {
        /// <summary>
        /// Payment provider identifiers the calling player is eligible for, in priority order
        /// (highest first). Empty when no provider is eligible — treat as a normal
        /// "external payment unavailable" state, not an error.
        /// </summary>
        public IReadOnlyList<string> Providers { get; }

        /// <summary>
        /// When false, the backend has disabled the SDK's payment-options popup; the picker UI
        /// should be suppressed and purchases routed directly to the native store. True by default
        /// when the backend omits the field, so older backends and rollbacks keep the popup on.
        /// </summary>
        public bool PaymentOptionPopupEnabled { get; }

        public EligiblePaymentProviders(IReadOnlyList<string> providers, bool paymentOptionPopupEnabled = true)
        {
            Providers = providers ?? Array.Empty<string>();
            PaymentOptionPopupEnabled = paymentOptionPopupEnabled;
        }
    }
}
