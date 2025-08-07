#nullable enable

namespace UnityEngine.Purchasing
{
    class MultipleConsumableQuantityCartValidator : ICartValidator
    {
        public void Validate(ICart cart)
        {
            foreach (var cartItem in cart.Items())
            {
                ValidateItem(cartItem);
            }
        }

        private void ValidateItem(CartItem cartItem)
        {
            if (cartItem.Product.definition.type == ProductType.Consumable)
            {
                if (cartItem.Quantity <= 0)
                {
                    throw new InvalidCartItemException("Consumable product quantity should be greater than 0.");
                }
            }
            else if (cartItem.Quantity != 1)
            {
                throw new InvalidCartItemException(
                    "Subscription and non-consumable product quantity should be equal to 1.");
            }
        }
    }
}
