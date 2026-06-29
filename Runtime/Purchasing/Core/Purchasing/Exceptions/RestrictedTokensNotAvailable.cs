#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    [Serializable]
    public class RestrictedTokensNotAvailable : IapException
    {
        public RestrictedTokensNotAvailable() { }

        public RestrictedTokensNotAvailable(string message) : base(message) { }
    }
}
