#nullable enable

using System;
using Purchasing.Extension;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class StoreWrapper : IStoreWrapper
    {
        InternalStore m_InternalStore;

        /// <summary>
        /// Gets the name of the store.
        /// </summary>
        /// <returns> The store's name </returns>
        public string name { get; }

        /// <summary>
        /// Gets the instance of the store.
        /// </summary>
        /// <returns> The store's instance </returns>
        public Store instance => m_InternalStore;

        /// <summary>
        /// Gets the connection state of the store
        /// </summary>
        /// <returns> The store's connectionState </returns>
        public ConnectionState GetStoreConnectionState()
        {
            return m_InternalStore.GetStoreConnectionState();
        }

        public StoreWrapper(string name, InternalStore instance)
        {
            this.name = name;
            m_InternalStore = instance;
        }
    }
}
