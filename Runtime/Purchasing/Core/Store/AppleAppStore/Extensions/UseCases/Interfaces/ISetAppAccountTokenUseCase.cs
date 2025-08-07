#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// A public interface for a class that acts out the use case of setting the Apple-specific obfuscation token
    /// (appAccountToken) to uniquely identify a user for transaction tracking.
    /// </summary>
    interface ISetAppAccountTokenUseCase
    {
        /// <summary>
        /// Sets the Apple-specific obfuscation token (appAccountToken) to uniquely identify a user for transaction tracking.
        /// </summary>
        /// <param name="appAccountToken">The obfuscated account token as a Guid</param>
        void SetAppAccountToken(Guid appAccountToken);
    }
}
