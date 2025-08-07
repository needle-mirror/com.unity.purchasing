#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// A public interface for a class that acts out the use case of determining if the user can make payments.
    /// </summary>
    interface ICanMakePaymentsUseCase
    {
        /// <summary>
        /// Determine if the user can make payments; [SKPaymentQueue canMakePayments].
        /// </summary>
        bool CanMakePayments();
    }
}
