using System.Linq;

namespace UnityEngine.Purchasing
{
    class DistinctProductsCartValidator : ICartValidator
    {
        public void Validate(ICart cart)
        {
            var items = cart.Items();
            if (items.Distinct().Count() != items.Count())
            {
                throw new InvalidCartException("A cart cannot contain more than one copy of the same product. Use Quantity instead.");
            }
        }
    }
}
