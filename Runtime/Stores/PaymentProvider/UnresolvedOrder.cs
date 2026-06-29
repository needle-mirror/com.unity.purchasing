#nullable enable
using System;
using UnityEngine.Purchasing.PaymentProviderService;

namespace UnityEngine.Purchasing.Stores
{
    internal record UnresolvedOrder
    {
        internal Guid orderId => orderData.id;
        internal OrderData orderData;
        internal int retryCount = 0;
        internal bool shouldTryCancel { get; private set; }  = true;

        internal UnresolvedOrder(OrderData orderData, int retryCount)
        {
            this.orderData = orderData;
            this.retryCount = retryCount;
        }

        internal void MarkShouldNotTryCancel()
        {
            shouldTryCancel = false;
        }
    }
}
