#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The base Model encapsulating an order to purchase a cart.
    /// </summary>
    public abstract class Order
    {
        /// <summary>
        /// The product ordered. Read only.
        /// </summary>
        public ICart CartOrdered { get; internal set; }

        /// <summary>
        /// Additional information about the order
        /// </summary>
        public IOrderInfo Info { get; }

        string? m_StoreName;

        /// <summary>
        /// The name of the store this order belongs to, e.g. <c>GooglePlay.Name</c>.
        /// Orders produced by a store carry that store's name. For an order built with a public constructor, it's the
        /// store all the products in its cart come from, or null if they don't share one.
        /// </summary>
        public string? StoreName
        {
            get
            {
                if (m_StoreName != null)
                {
                    return m_StoreName;
                }
                // A cart from one store names it; an empty cart, mixed stores or a product with no store don't.
                var cartStores = StoreNamesOf(CartOrdered);
                return cartStores.Count == 1 ? cartStores[0] : null;
            }
            internal set => m_StoreName = value;
        }

        // The distinct stores of a cart's products, in cart order (null for a product with no store).
        internal static List<string?> StoreNamesOf(ICart? cart)
        {
            var names = new List<string?>();
            var items = cart?.Items();
            if (items == null)
            {
                return names;
            }
            foreach (var item in items)
            {
                var name = item?.Product?.StoreName;
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }
            return names;
        }

        // Tags the order, and any product in its cart without a store, with the store that produced it.
        // A product that already knows its store keeps it.
        internal void SetStoreNameIfMissing(string storeName)
        {
            m_StoreName ??= storeName;
            var items = CartOrdered?.Items();
            if (items == null)
            {
                return;
            }
            foreach (var item in items)
            {
                if (item?.Product != null)
                {
                    item.Product.StoreName ??= storeName;
                }
            }
        }

        /// <summary>
        /// Constructs the base order object.
        /// </summary>
        /// <param name="cart">The cart ordered.</param>
        /// <param name="info">Additional information concerning this order.</param>
        protected Order(ICart cart, IOrderInfo info)
        {
            CartOrdered = cart;
            Info = info;
        }
    }
}
