#nullable enable
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing.Extension
{
    internal interface IReadOnlyPurchaseCache
    {
        public ReadOnlyObservableCollection<Order> GetOrders();
    }
}
