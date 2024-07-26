#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Model class describing the failure to fetch products from a Product Service object.
    /// </summary>
    public class ProductFetchFailed
    {

        /// <summary>
        /// The list of products that could not be fetched.
        /// </summary>
        public List<ProductDefinition> FailedFetchProducts { get; }

        /// <summary>
        /// The reason for which the products could not be fetched.
        /// </summary>
        public string FailureReason { get; }

        internal ProductFetchFailed(List<ProductDefinition> products, string reason)
        {
            FailedFetchProducts = products;
            FailureReason = reason;
        }
    }
}
