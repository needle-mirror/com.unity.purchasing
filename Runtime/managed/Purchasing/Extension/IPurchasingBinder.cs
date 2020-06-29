using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Configures Unity Purchasing with one or more
    /// store implementations.
    /// </summary>
    public interface IPurchasingBinder
    {
        /// <summary>
        /// Informs Unity Purchasing that a store implementation exists,
        /// specifying its name.
        ///
        /// Modules can pass null IStore instances when running on platforms
        /// they do not support.
        /// </summary>
        void RegisterStore(string name, IStore a);

        /// <summary>
        /// Informs Unity Purchasing that a store extension is available.
        /// </summary>
        void RegisterExtension<T>(T instance) where T : IStoreExtension;

        /// <summary>
        /// Informs Unity Purchasing that extended Configuration is available.
        /// </summary>
        void RegisterConfiguration<T>(T instance) where T : IStoreConfiguration;

        /// <summary>
        /// Informs Unity Purchasing about a catalog provider which might replace or add products at runtime.
        /// </summary>
        void SetCatalogProvider(ICatalogProvider provider);

        /// <summary>
        /// Informs Unity Purchasing about a catalog provider function, which might replace or add products at runtime.
        /// This is an alternative to the SetCatalogProvider API for setting a catalog provider that does not implement
        /// the ICatalogProvider interface.
        /// </summary>
        void SetCatalogProviderFunction(Action<Action<HashSet<ProductDefinition>>> func);
    }
}
