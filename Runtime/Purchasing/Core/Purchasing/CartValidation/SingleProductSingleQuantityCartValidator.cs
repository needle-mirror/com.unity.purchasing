#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Cart Validator that checks that the cart contains a single product with a single quantity.
    /// </summary>
    public class SingleProductSingleQuantityCartValidator : AggregateCartValidator
    {
        /// <summary>
        /// Create a SingleProductSingleQuantityCartValidator.
        /// </summary>
        public SingleProductSingleQuantityCartValidator()
            : base(new NonNullCartValidator(), new SingleProductCartValidator(), new SingleQuantityCartValidator()) { }
    }
}
