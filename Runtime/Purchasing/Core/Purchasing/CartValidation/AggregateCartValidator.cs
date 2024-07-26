#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A cart validator that instead of having a specific implementation runs the validation implementations of the various cart validators passed to the constructor
    /// </summary>
    public class AggregateCartValidator : ICartValidator
    {
        private readonly List<ICartValidator> m_CartValidators;

        /// <summary>
        /// Create an AggregateCartValidator with a list of cart validators
        /// </summary>
        /// <param name="cartValidators"> The List of cart validators to aggregate. </param>
        public AggregateCartValidator(List<ICartValidator> cartValidators)
        {
            m_CartValidators = cartValidators;
        }

        /// <summary>
        /// Create an AggregateCartValidator with a series of cart validators as comma separated parameters.
        /// </summary>
        /// <param name="cartValidators"> The set of cart validators to aggregate, separated by commas. </param>
        public AggregateCartValidator(params ICartValidator[] cartValidators)
        {
            m_CartValidators = new List<ICartValidator>(cartValidators);
        }

        /// <summary>
        /// Runs the Validate functions of all of the aggregate validators and throws any exceptions caught by any of them.
        /// </summary>
        /// <param name="cart"> The car to validate. </param>
        public void Validate(ICart cart)
        {
            foreach (var cartValidator in m_CartValidators)
            {
                cartValidator.Validate(cart);
            }
        }
    }
}
