#nullable enable

using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Store wrapper containing the store instance and name.
    /// </summary>
    public interface IStoreWrapper
    {
        /// <summary>
        /// Gets the instance of the store.
        /// </summary>
        /// <value> The store's instance </value>
        Store instance { get; }

        /// <summary>
        /// Gets the name of the store.
        /// </summary>
        /// <value> The store's name </value>
        string name { get; }

        /// <summary>
        /// Gets the connection state of the store
        /// </summary>
        /// <returns> The store's connectionState </returns>
        ConnectionState GetStoreConnectionState();
    }
}
