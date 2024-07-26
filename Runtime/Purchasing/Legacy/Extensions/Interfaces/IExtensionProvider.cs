using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface providing access to various store extensions.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public interface IExtensionProvider
    {
        /// <summary>
        /// Get an implementation of a store extension specified by the template parameter.
        /// </summary>
        /// <typeparam name="T"> Implementation of <c>IStoreExtension</c> </typeparam>
        /// <returns> The store extension requested. </returns>
        T GetExtension<T>() where T : IStoreExtension;
    }
}
