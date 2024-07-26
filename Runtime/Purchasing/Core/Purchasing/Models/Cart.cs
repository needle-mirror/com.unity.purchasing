#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    public class Cart : ICart
    {
        private readonly HashSet<CartItem> m_CartItem = new HashSet<CartItem>();

        public Cart(CartItem cartItem)
        {
            m_CartItem.Add(cartItem);
        }

        public IReadOnlyList<CartItem> Items()
        {
            return m_CartItem.ToList();
        }
    }
}
