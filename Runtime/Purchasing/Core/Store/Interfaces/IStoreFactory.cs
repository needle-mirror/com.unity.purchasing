#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Store factory interface used to create stores.
    /// </summary>
    interface IStoreFactory
    {
        /// <summary>
        /// Creates a store for the specified store.
        /// </summary>
        /// <param name="storeName"> The name of the store to create. </param>
        IStoreWrapper CreateStore(string storeName);

        /// <summary>
        /// Registers the function to use when creating a store.
        /// </summary>
        /// <param name="storeName"> The name of the store that will use this function. </param>
        /// <param name="function"> The function to be used to create the store. </param>
        void RegisterStore(string storeName, Func<IStoreWrapper> function);
    }
}
