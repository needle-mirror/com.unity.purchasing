#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// A public interface for a class that acts out the use case of simulating ask to buy.
    /// </summary>
    interface ISimulateAskToBuyUseCase
    {
        /// <summary>
        /// For testing purposes only.
        ///
        /// Get payment request for testing ask-to-buy.
        /// </summary>
        bool SimulateAskToBuy();

        /// <summary>
        /// For testing purposes only.
        ///
        /// Set payment request for testing ask-to-buy.
        /// </summary>
        void SetSimulateAskToBuy(bool value);
    }
}
