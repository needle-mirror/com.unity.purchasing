using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface for the confirmation of pending orders.
    /// </summary>
    interface IConfirmOrderUseCase
    {
        /// <summary>
        /// Confirm a pending order, usually asynchronously. Success or failure is signalled via the action passed.
        /// </summary>
        /// <param name="order">The pending order to be confirmed.</param>
        /// <param name="action">The event called when the confirmation is received.</param>
        void ConfirmOrder(PendingOrder order, Action<PendingOrder, Order> action);
    }
}
