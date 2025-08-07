using System;
using UnityEngine.Networking;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// This is an internal API.
    /// We recommend that you do not use it as it will be removed in a future release.
    /// </summary>
    [Obsolete("Internal API, it will be removed soon.")]
    public interface IWebRequest
    {
        /// <summary>
        /// This is an internal API.
        /// We recommend that you do not use it as it will be removed in a future release.
        /// </summary>
        /// <param name="uri">uri</param>
        /// <returns>UnityWebRequest</returns>
        [Obsolete("Internal API, it will be removed soon.")]
        UnityWebRequest BuildWebRequest(string uri);
    }
}
