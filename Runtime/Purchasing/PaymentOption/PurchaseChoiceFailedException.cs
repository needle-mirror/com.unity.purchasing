#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Thrown on the awaitable returned by <see cref="IPaymentOptionProvider.ShowPurchaseOption(string)"/>
    /// (and its overloads) when the picker's chosen route fails — e.g. the webshop URL
    /// fetch errors, or the store's <c>PurchaseProduct</c> call throws synchronously.
    /// Carries the <see cref="PurchaseOption"/> the user picked so callers can correlate
    /// the failure to the specific button.
    /// </summary>
    public sealed class PurchaseChoiceFailedException : Exception
    {
        /// <summary>The option the user picked before the failure.</summary>
        public PurchaseOption Choice { get; }

        /// <summary>
        /// Constructs the exception with the user's pick and the underlying failure.
        /// </summary>
        /// <param name="choice">The option the user picked.</param>
        /// <param name="innerException">The underlying failure thrown by the chosen route.</param>
        public PurchaseChoiceFailedException(PurchaseOption choice, Exception innerException)
            : base($"Purchase failed for {choice.StoreName} / {choice.CatalogListingId}: {innerException.Message}", innerException)
        {
            Choice = choice;
        }
    }
}
