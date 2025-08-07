#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// A public interface for a class that acts out the use case of initiating the Subscription Offer Code redemption API.
    /// </summary>
    interface IPresentCodeRedemptionSheetUseCase
    {
        /// <summary>
        /// Initiate Apple Subscription Offer Code redemption API, presentCodeRedemptionSheet
        /// </summary>
        void PresentCodeRedemptionSheet();
    }
}
