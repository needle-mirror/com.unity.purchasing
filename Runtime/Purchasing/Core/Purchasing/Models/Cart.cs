#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    public class Cart : ICart
    {
        private readonly HashSet<CartItem> m_CartItems = new();
        IReadOnlyList<CartItem>? m_CachedItemsList;

        public Cart(CartItem cartItem)
        {
            m_CartItems.Add(cartItem);
        }

        public IReadOnlyList<CartItem> Items()
        {
            return m_CachedItemsList ??= m_CartItems.ToList();
        }

        public void AddItem(CartItem item)
        {
            if (m_CartItems.Add(item))
            {
                m_CachedItemsList = null;
            }
        }

        public void RemoveItem(CartItem item)
        {
            if (m_CartItems.Remove(item))
            {
                m_CachedItemsList = null;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            return obj is Cart cart && m_CartItems.SetEquals(cart.m_CartItems);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var item in m_CartItems)
                {
                    hash ^= item?.GetHashCode() ?? 0;
                }
                return hash;
            }
        }
    }
}
