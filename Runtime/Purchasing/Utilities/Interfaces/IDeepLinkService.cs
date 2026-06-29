#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Global deep link dispatcher. Raise events whenever the application
    /// is opened or resumed via a deep link, including the cold-start launch link.
    /// </summary>
    public interface IDeepLinkService
    {
        /// <summary>
        /// Raised when a deep link activates the application. If the application
        /// was cold-started by a link, the cached launch link is delivered to
        /// the first handler that subscribes.
        /// </summary>
        event Action<string> OnDeepLinkActivated;

        /// <summary>
        /// Raised when a deep link activates the application but there is no
        /// authenticated Unity Services user to handle it. Subscribe to prompt
        /// the player to sign in.
        /// </summary>
        event Action OnAuthenticationFailed;
    }
}
