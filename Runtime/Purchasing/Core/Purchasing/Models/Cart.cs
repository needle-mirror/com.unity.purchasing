#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents a shopping cart that contains a collection of <see cref="CartItem"/> objects.
    /// </summary>
    public class Cart : ICart
    {
        private readonly HashSet<CartItem> m_CartItems = new();
        IReadOnlyList<CartItem>? m_CachedItemsList;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cart"/> class with a single <see cref="CartItem"/>.
        /// </summary>
        /// <param name="cartItem">A cart item.</param>
        public Cart(CartItem cartItem)
        {
            m_CartItems.Add(cartItem);
        }

        /// <summary>
        /// Gets the items in the cart as a read-only list.
        /// </summary>
        /// <returns>A read-only list of <see cref="CartItem"/> objects.</returns>
        public IReadOnlyList<CartItem> Items()
        {
            return m_CachedItemsList ??= m_CartItems.ToList();
        }

        /// <summary>
        /// Adds a new <see cref="CartItem"/> to the cart.
        /// </summary>
        /// <param name="item">The <see cref="CartItem"/> to add.</param>
        public void AddItem(CartItem item)
        {
            if (m_CartItems.Add(item))
            {
                m_CachedItemsList = null;
            }
        }

        /// <summary>
        /// Removes a <see cref="CartItem"/> from the cart.
        /// </summary>
        /// <param name="item">The <see cref="CartItem"/> to remove.</param>
        public void RemoveItem(CartItem item)
        {
            if (m_CartItems.Remove(item))
            {
                m_CachedItemsList = null;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="Cart"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="Cart"/>.</param>
        /// <returns>`true` if the specified object is equal to the current <see cref="Cart"/>; otherwise, `false`.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            return obj is Cart cart && m_CartItems.SetEquals(cart.m_CartItems);
        }

        /// <summary>
        /// Gets the hash code for the current <see cref="Cart"/>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="Cart"/>.</returns>
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
