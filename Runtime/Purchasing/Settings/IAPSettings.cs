using System;
using Newtonsoft.Json;

namespace Common.Settings
{
    [Serializable]
    internal class IapSettings
    {
        /// <summary>
        /// The category of settings associated to Google.
        /// </summary>
        public GoogleIapSettings google;
    }
}
