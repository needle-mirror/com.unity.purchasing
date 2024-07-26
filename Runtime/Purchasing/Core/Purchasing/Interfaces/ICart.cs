using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    public interface ICart
    {
        IReadOnlyList<CartItem> Items();
    }
}
