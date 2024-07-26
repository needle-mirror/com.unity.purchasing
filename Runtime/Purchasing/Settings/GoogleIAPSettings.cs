using System;
using Newtonsoft.Json;

namespace Common.Settings
{
    [Serializable]
    public class GoogleIapSettings
    {
        /// <summary>
        /// The GPK (Google public key) associated to the unity project for IAP.
        /// </summary>
        public string publicKey;
    }
}
