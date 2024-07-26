using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Public Interface for the confirmation of pending orders.
    /// </summary>
    public interface IConfirmOrderUseCase
    {
        /// <summary>
        /// Confirm a pending order, usually asynchronously. Success or failure is signalled via the actions passed.
        /// </summary>
        /// <param name="order">The pending order to be confirmed.</param>
        /// <param name="confirmationSuccessAction">The event called when the confirmation is successful.</param>
        /// <param name="confirmationFailedAction">The event called when the confirmation fails.</param>
        void ConfirmOrder(PendingOrder order, Action<PendingOrder, ConfirmedOrder> confirmationSuccessAction, Action<PendingOrder, FailedOrder> confirmationFailedAction);
    }
}
