#nullable enable

namespace UnityEngine.Purchasing.Extension
{
    internal interface IPurchaseCache : IReadOnlyPurchaseCache
    {
        public void Add(Order order);
        public void Remove(Order order);
        public void Clear();
    }
}
