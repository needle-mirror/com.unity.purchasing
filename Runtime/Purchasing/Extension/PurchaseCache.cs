#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing.Extension
{
    class PurchaseCache : IPurchaseCache
    {
        readonly ObservableCollection<Order> m_Orders = new();
        ReadOnlyObservableCollection<Order> m_OrdersReadOnly;

        internal PurchaseCache()
        {
            m_OrdersReadOnly = new ReadOnlyObservableCollection<Order>(m_Orders);
        }

        public void Add(Order order)
        {
            m_Orders.Add(order);
        }

        public void Remove(Order order)
        {
            m_Orders.Remove(order);
        }

        public void Clear()
        {
            m_Orders.Clear();
        }

        public ReadOnlyObservableCollection<Order> GetOrders()
        {
            return m_OrdersReadOnly;
        }
    }
}
