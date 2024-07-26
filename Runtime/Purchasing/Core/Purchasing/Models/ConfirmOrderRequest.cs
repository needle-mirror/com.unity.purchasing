using System;

namespace UnityEngine.Purchasing
{
    internal class ConfirmOrderRequest
    {
        internal PendingOrder OrderToConfirm { get; }
        internal Action<PendingOrder, ConfirmedOrder> SuccessAction { get; }
        internal Action<PendingOrder, FailedOrder> FailureAction { get; }

        internal ConfirmOrderRequest(PendingOrder order, Action<PendingOrder, ConfirmedOrder> purchaseSuccessAction, Action<PendingOrder, FailedOrder> purchaseFailureAction)
        {
            OrderToConfirm = order;
            SuccessAction = purchaseSuccessAction;
            FailureAction = purchaseFailureAction;
        }
    }
}
