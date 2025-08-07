using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents the settings for Google IAP integration in Unity.
    /// </summary>
    [Serializable]
    public class GoogleIapSettings
    {
        /// <summary>
        /// The GPK (Google public key) associated to the unity project for IAP.
        /// </summary>
        public string publicKey;
    }
}
