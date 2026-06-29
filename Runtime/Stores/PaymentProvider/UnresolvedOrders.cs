#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.PaymentProviderService;

namespace UnityEngine.Purchasing.Stores
{
    internal class UnresolvedOrders
    {
        readonly int m_RetryMax;
        List<UnresolvedOrder> m_UnresolvedOrders = new List<UnresolvedOrder>();

        internal UnresolvedOrders(int retryMax)
        {
            m_RetryMax = Math.Max(retryMax, 0);
        }

        internal bool HasOrders => GetValidOrders().Any();

        internal void RemoveOrder(Guid orderId)
        {
            m_UnresolvedOrders.RemoveAll(o => o.orderId.Equals(orderId));
        }

        internal void AddOrder(OrderData orderData)
        {
            RemoveOrder(orderData.id);
            m_UnresolvedOrders.Add(new UnresolvedOrder(orderData, 0));
        }

        internal List<UnresolvedOrder> GetValidOrders()
        {
            return m_UnresolvedOrders.Where(order => order.retryCount <= m_RetryMax).ToList();
        }

        internal bool CheckShouldFinalizeAndIncrementRetryCount(Guid orderId)
        {
            var order = GetFirstOrDefaultOrderForId(orderId);
            var shouldFinalize = order?.retryCount == m_RetryMax;

            if (order != null)
            {
                order.retryCount++;
            }

            return shouldFinalize;
        }

        internal void MarkOrderCannotBeCancelled(Guid orderId)
        {
            var order = GetFirstOrDefaultOrderForId(orderId);
            order?.MarkShouldNotTryCancel();
        }

        UnresolvedOrder? GetFirstOrDefaultOrderForId(Guid orderId)
        {
            return m_UnresolvedOrders.FirstOrDefault(o => o.orderId.Equals(orderId));
        }
    }
}
