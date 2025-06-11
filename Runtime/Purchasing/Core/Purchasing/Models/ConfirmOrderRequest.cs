using System;

namespace UnityEngine.Purchasing
{
    internal class ConfirmOrderRequest
    {
        internal PendingOrder OrderToConfirm { get; }
        internal Action<PendingOrder, Order> Action { get; }

        internal ConfirmOrderRequest(PendingOrder order, Action<PendingOrder, Order> purchaseSuccessAction)
        {
            OrderToConfirm = order;
            Action = purchaseSuccessAction;
        }
    }
}
