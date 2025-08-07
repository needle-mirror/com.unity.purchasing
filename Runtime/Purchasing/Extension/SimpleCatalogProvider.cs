using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A simple implementation of <see cref="IBaseCatalogProvider"/> that uses a delegate to fetch product definitions.
    /// This provider wraps a function delegate to provide product catalog functionality in a straightforward manner.
    /// </summary>
    // todo: make this public and remove it from purchasingfactory
    public class SimpleCatalogProvider : IBaseCatalogProvider
    {
        readonly Action<Action<List<ProductDefinition>>> m_Func;

        /// <summary>
        /// Initializes a new instance of the SimpleCatalogProvider with a function delegate for fetching products.
        /// </summary>
        /// <param name="func">
        /// A function delegate that takes a callback as parameter and is responsible for fetching product definitions.
        /// The delegate should invoke the provided callback with the retrieved list of product definitions.
        /// Can be null, in which case FetchProducts will not perform any operation.
        /// </param>
        public SimpleCatalogProvider(Action<Action<List<ProductDefinition>>> func)
        {
            m_Func = func;
        }

        /// <summary>
        /// Fetches product definitions by invoking the configured function delegate.
        /// </summary>
        /// <param name="callback">
        /// A callback function that will be invoked with the list of product definitions.
        /// The callback receives a <see cref="List{ProductDefinition}"/> containing the fetched products.
        /// If the internal function delegate is null, the callback will not be invoked.
        /// </param>
        public void FetchProducts(Action<List<ProductDefinition>> callback)
        {
            m_Func?.Invoke(callback);
        }
    }
}
